using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DndApp.Api.Data;
using Microsoft.EntityFrameworkCore;

var repoRoot = ResolveRepoRoot(args);
var outputRoot = Path.Combine(repoRoot, "data");
var now = DateTimeOffset.UtcNow;

if (args.Any(a => string.Equals(a, "--import-db", StringComparison.OrdinalIgnoreCase)))
{
    await ImportSectionsToDatabaseAsync(repoRoot, now);
    return;
}

if (args.Any(a => string.Equals(a, "--normalize-db", StringComparison.OrdinalIgnoreCase)))
{
    await NormalizeCoreEntitiesAsync(repoRoot);
    return;
}

var sources = new[]
{
    new SourceSpec("phb2014", "DnDPHB2014.md"),
    new SourceSpec("phb2024", "DnDPHB2024.md"),
    new SourceSpec("dmg2014", "DnDDMG2014.md"),
    new SourceSpec("dmg2024", "DNDDMG2024.md"),
};

var summaries = new List<IngestionSummary>();

foreach (var source in sources)
{
    var inputPath = Path.Combine(repoRoot, source.FileName);
    if (!File.Exists(inputPath))
    {
        Console.WriteLine($"Skipping {source.Code}: missing file {source.FileName}");
        continue;
    }

    var lines = File.ReadAllLines(inputPath);
    var sections = ExtractSections(lines);
    var records = sections
        .Select((section, index) => BuildSectionRecord(section, index + 1))
        .ToList();

    var payload = new IngestionPayload(
        source.Code,
        source.FileName,
        now,
        records.Count,
        records);

    var sourceOutputDir = Path.Combine(outputRoot, "ingested", source.Code, "v1");
    Directory.CreateDirectory(sourceOutputDir);
    var sectionsPath = Path.Combine(sourceOutputDir, "sections.json");

    WriteJson(sectionsPath, payload);

    var lowConfidence = records.Where(r => r.Confidence < 0.75m).ToList();
    if (lowConfidence.Count > 0)
    {
        var reviewDir = Path.Combine(outputRoot, "review", "pending");
        Directory.CreateDirectory(reviewDir);
        var reviewPath = Path.Combine(reviewDir, $"{source.Code}-low-confidence.json");
        WriteJson(reviewPath, new LowConfidencePayload(source.Code, now, lowConfidence.Count, lowConfidence));
    }

    summaries.Add(new IngestionSummary(
        source.Code,
        records.Count,
        lowConfidence.Count,
        sectionsPath));
}

Console.WriteLine("Ingestion complete.");
foreach (var summary in summaries)
{
    Console.WriteLine(
        $"{summary.SourceCode}: sections={summary.SectionCount}, lowConfidence={summary.LowConfidenceCount}, output={summary.OutputPath}");
}

return;

static async Task ImportSectionsToDatabaseAsync(string repoRoot, DateTimeOffset startedAtUtc)
{
    var sqlitePath = Path.Combine(repoRoot, "src", "DndApp.Api", "dndapp-dev.sqlite");
    var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite($"Data Source={sqlitePath}")
        .Options;

    await using var db = new AppDbContext(dbOptions);
    await db.Database.MigrateAsync();

    var run = new IngestionRunEntity
    {
        Id = Guid.NewGuid().ToString("N"),
        SourceCode = "all",
        VersionTag = "v1",
        StartedAtUtc = startedAtUtc,
        Status = "running",
        Checksum = string.Empty
    };
    db.IngestionRuns.Add(run);
    await db.SaveChangesAsync();

    var sectionsFiles = Directory
        .EnumerateFiles(Path.Combine(repoRoot, "data", "ingested"), "sections.json", SearchOption.AllDirectories)
        .OrderBy(x => x)
        .ToArray();

    var importedSections = 0;
    var importedBlocks = 0;
    var reviewCount = 0;

    foreach (var sectionsFile in sectionsFiles)
    {
        var json = await File.ReadAllTextAsync(sectionsFile);
        var payload = JsonSerializer.Deserialize<IngestionPayload>(json);
        if (payload is null)
        {
            continue;
        }

        var versionTag = "v1";
        var book = db.SourceBooks.SingleOrDefault(x => x.SourceCode == payload.SourceCode && x.VersionTag == versionTag);
        if (book is null)
        {
            book = new SourceBookEntity
            {
                Id = Guid.NewGuid().ToString("N"),
                SourceCode = payload.SourceCode,
                FileName = payload.SourceFile,
                VersionTag = versionTag
            };
            db.SourceBooks.Add(book);
        }

        var existingChapters = db.SourceChapters.Where(x => x.SourceBookId == book.Id).ToList();
        if (existingChapters.Count > 0)
        {
            db.SourceChapters.RemoveRange(existingChapters);
            await db.SaveChangesAsync();
        }

        var chapter = new SourceChapterEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            SourceBookId = book.Id,
            Title = "Imported Sections",
            ChapterOrder = 1
        };
        db.SourceChapters.Add(chapter);
        await db.SaveChangesAsync();

        foreach (var section in payload.Sections.OrderBy(x => x.SectionIndex))
        {
            var sectionEntity = new SourceSectionEntity
            {
                Id = Guid.NewGuid().ToString("N"),
                SourceChapterId = chapter.Id,
                Title = section.Title,
                SectionOrder = section.SectionIndex,
                StartLine = section.StartLine,
                EndLine = section.EndLine
            };
            db.SourceSections.Add(sectionEntity);

            var block = new SourceBlockEntity
            {
                Id = Guid.NewGuid().ToString("N"),
                SourceSectionId = sectionEntity.Id,
                BlockType = "preview",
                RawText = section.Preview,
                ParseConfidence = section.Confidence
            };
            db.SourceBlocks.Add(block);

            importedSections++;
            importedBlocks++;

            if (section.Confidence < 0.75m)
            {
                db.ReviewQueue.Add(new ReviewQueueEntity
                {
                    Id = Guid.NewGuid().ToString("N"),
                    IngestionRunId = run.Id,
                    QueueType = "low-confidence-section",
                    ReferenceId = sectionEntity.Id,
                    Confidence = section.Confidence,
                    Notes = $"Section '{section.Title}' from {payload.SourceCode}",
                    Status = "pending"
                });
                reviewCount++;
            }
        }

        await db.SaveChangesAsync();
        run.Checksum += $"{payload.SourceCode}:{ComputeSha256(json)};";
    }

    run.CompletedAtUtc = DateTimeOffset.UtcNow;
    run.Status = "completed";
    run.Checksum = ComputeSha256(run.Checksum);

    db.ImportReports.Add(new ImportReportEntity
    {
        Id = Guid.NewGuid().ToString("N"),
        IngestionRunId = run.Id,
        CreatedAtUtc = DateTimeOffset.UtcNow,
        ReportJson = JsonSerializer.Serialize(new
        {
            importedSections,
            importedBlocks,
            reviewCount
        })
    });

    await db.SaveChangesAsync();

    Console.WriteLine("Database import complete.");
    Console.WriteLine($"Sections imported: {importedSections}");
    Console.WriteLine($"Blocks imported: {importedBlocks}");
    Console.WriteLine($"Review queue entries: {reviewCount}");
    Console.WriteLine($"SQLite file: {sqlitePath}");
}

static async Task NormalizeCoreEntitiesAsync(string repoRoot)
{
    var sqlitePath = Path.Combine(repoRoot, "src", "DndApp.Api", "dndapp-dev.sqlite");
    var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite($"Data Source={sqlitePath}")
        .Options;

    await using var db = new AppDbContext(dbOptions);
    await db.Database.MigrateAsync();

    var systems = new[]
    {
        new RuleSystemEntity { Id = "rules-2014", Name = "2014" },
        new RuleSystemEntity { Id = "rules-2024", Name = "2024" },
    };
    foreach (var system in systems)
    {
        if (!db.RuleSystems.Any(x => x.Id == system.Id))
        {
            db.RuleSystems.Add(system);
        }
    }

    var sources = new[]
    {
        new ContentSourceEntity { Id = "PHB2014", RuleSystemId = "rules-2014", Code = "PHB2014", Name = "Player's Handbook 2014" },
        new ContentSourceEntity { Id = "PHB2024", RuleSystemId = "rules-2024", Code = "PHB2024", Name = "Player's Handbook 2024" },
        new ContentSourceEntity { Id = "DMG2014", RuleSystemId = "rules-2014", Code = "DMG2014", Name = "Dungeon Master's Guide 2014" },
        new ContentSourceEntity { Id = "DMG2024", RuleSystemId = "rules-2024", Code = "DMG2024", Name = "Dungeon Master's Guide 2024" },
    };
    foreach (var source in sources)
    {
        if (!db.ContentSources.Any(x => x.Id == source.Id))
        {
            db.ContentSources.Add(source);
        }
    }
    await db.SaveChangesAsync();

    var existingVariants = db.RuleVariants.ToList();
    if (existingVariants.Count > 0)
    {
        db.RuleVariants.RemoveRange(existingVariants);
    }

    var existingModules = db.RuleModules.ToList();
    if (existingModules.Count > 0)
    {
        db.RuleModules.RemoveRange(existingModules);
    }
    await db.SaveChangesAsync();

    var sectionRows = await (
        from book in db.SourceBooks
        join chapter in db.SourceChapters on book.Id equals chapter.SourceBookId
        join section in db.SourceSections on chapter.Id equals section.SourceChapterId
        select new
        {
            book.SourceCode,
            book.VersionTag,
            section.Id,
            section.Title,
            section.SectionOrder,
            section.StartLine,
            section.EndLine,
        })
        .OrderBy(x => x.SourceCode)
        .ThenBy(x => x.SectionOrder)
        .ToListAsync();

    var moduleCount = 0;
    var variantCount = 0;

    foreach (var row in sectionRows)
    {
        var contentSourceId = ResolveContentSourceId(row.SourceCode);
        var ruleSystemId = contentSourceId.EndsWith("2014", StringComparison.OrdinalIgnoreCase)
            ? "rules-2014"
            : "rules-2024";

        var slug = $"{Slugify(row.Title)}-{row.SectionOrder}";
        var moduleId = Guid.NewGuid().ToString("N");
        var module = new RuleModuleEntity
        {
            Id = moduleId,
            ContentSourceId = contentSourceId,
            ModuleType = "section",
            Slug = slug,
            DisplayName = row.Title,
            VersionTag = row.VersionTag
        };
        db.RuleModules.Add(module);
        moduleCount++;

        var variant = new RuleVariantEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            RuleModuleId = moduleId,
            RuleSystemId = ruleSystemId,
            CompatibilityTagsJson = JsonSerializer.Serialize(new[] { ruleSystemId == "rules-2014" ? "compatible-2014" : "compatible-2024" }),
            PayloadJson = JsonSerializer.Serialize(new
            {
                sourceSectionId = row.Id,
                startLine = row.StartLine,
                endLine = row.EndLine
            })
        };
        db.RuleVariants.Add(variant);
        variantCount++;
    }

    await db.SaveChangesAsync();

    Console.WriteLine("Normalization complete.");
    Console.WriteLine($"Rule modules created: {moduleCount}");
    Console.WriteLine($"Rule variants created: {variantCount}");
    Console.WriteLine($"SQLite file: {sqlitePath}");
}

static string ComputeSha256(string value)
{
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
    return Convert.ToHexString(bytes);
}

static string Slugify(string value)
{
    var lower = value.Trim().ToLowerInvariant();
    var slug = Regex.Replace(lower, @"[^a-z0-9]+", "-");
    return slug.Trim('-');
}

static string ResolveContentSourceId(string sourceCode)
{
    return sourceCode.ToLowerInvariant() switch
    {
        "phb2014" => "PHB2014",
        "phb2024" => "PHB2024",
        "dmg2014" => "DMG2014",
        "dmg2024" => "DMG2024",
        _ => throw new InvalidOperationException($"Unknown source code '{sourceCode}'.")
    };
}

static string ResolveRepoRoot(string[] args)
{
    var firstArgDirectory = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
    if (firstArgDirectory is not null && Directory.Exists(firstArgDirectory))
    {
        return Path.GetFullPath(firstArgDirectory);
    }

    var markerFiles = new[]
    {
        "DnDPHB2014.md",
        "DnDPHB2024.md",
        "DnDDMG2014.md",
        "DNDDMG2024.md",
    };

    var current = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (current is not null)
    {
        var found = markerFiles.All(f => File.Exists(Path.Combine(current.FullName, f)));
        if (found)
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    throw new InvalidOperationException("Could not locate repository root containing source markdown files.");
}

static List<RawSection> ExtractSections(IReadOnlyList<string> lines)
{
    var sections = new List<RawSection>();
    var headingRegex = new Regex(@"^\s{0,3}#{1,6}\s+(.+?)\s*$", RegexOptions.Compiled);

    string currentTitle = "Document Root";
    var startLine = 1;
    var buffer = new List<string>();

    for (var i = 0; i < lines.Count; i++)
    {
        var line = lines[i];
        var headingMatch = headingRegex.Match(line);
        if (headingMatch.Success)
        {
            if (buffer.Count > 0)
            {
                sections.Add(new RawSection(currentTitle, startLine, i, buffer.ToArray()));
            }

            currentTitle = headingMatch.Groups[1].Value.Trim();
            startLine = i + 1;
            buffer.Clear();
            continue;
        }

        buffer.Add(line);
    }

    if (buffer.Count > 0)
    {
        sections.Add(new RawSection(currentTitle, startLine, lines.Count, buffer.ToArray()));
    }

    return sections;
}

static SectionRecord BuildSectionRecord(RawSection raw, int sectionIndex)
{
    var confidence = EstimateConfidence(raw);
    var preview = string.Join(" ", raw.ContentLines.Take(3)).Trim();
    if (preview.Length > 220)
    {
        preview = preview[..220];
    }

    return new SectionRecord(
        sectionIndex,
        raw.Title,
        raw.StartLine,
        raw.EndLine,
        raw.ContentLines.Length,
        confidence,
        preview);
}

static decimal EstimateConfidence(RawSection section)
{
    var lineCount = Math.Max(section.ContentLines.Length, 1);
    var joined = string.Join('\n', section.ContentLines);
    var textLength = Math.Max(joined.Length, 1);

    var controlChars = joined.Count(c => char.IsControl(c) && c is not '\n' and not '\r' and not '\t');
    var unknownGlyphs = joined.Count(c => c == '�');
    var weirdRatio = (decimal)(controlChars + unknownGlyphs) / textLength;
    var sparsePenalty = lineCount < 2 ? 0.08m : 0m;
    var noisyPenalty = weirdRatio > 0.01m ? Math.Min(0.25m, weirdRatio * 4m) : 0m;

    var score = 0.92m - sparsePenalty - noisyPenalty;
    return Math.Clamp(score, 0.25m, 0.99m);
}

static void WriteJson<T>(string path, T value)
{
    var options = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    File.WriteAllText(path, JsonSerializer.Serialize(value, options));
}

sealed record SourceSpec(string Code, string FileName);
sealed record RawSection(string Title, int StartLine, int EndLine, string[] ContentLines);

sealed record SectionRecord(
    int SectionIndex,
    string Title,
    int StartLine,
    int EndLine,
    int LineCount,
    decimal Confidence,
    string Preview);

sealed record IngestionPayload(
    string SourceCode,
    string SourceFile,
    DateTimeOffset GeneratedAtUtc,
    int SectionCount,
    IReadOnlyList<SectionRecord> Sections);

sealed record LowConfidencePayload(
    string SourceCode,
    DateTimeOffset GeneratedAtUtc,
    int LowConfidenceCount,
    IReadOnlyList<SectionRecord> Sections);

sealed record IngestionSummary(
    string SourceCode,
    int SectionCount,
    int LowConfidenceCount,
    string OutputPath);
