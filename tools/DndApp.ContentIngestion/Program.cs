using System.Text.Json;
using System.Text.RegularExpressions;

var repoRoot = ResolveRepoRoot(args);
var outputRoot = Path.Combine(repoRoot, "data");
var now = DateTimeOffset.UtcNow;

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

static string ResolveRepoRoot(string[] args)
{
    if (args.Length > 0 && Directory.Exists(args[0]))
    {
        return Path.GetFullPath(args[0]);
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
