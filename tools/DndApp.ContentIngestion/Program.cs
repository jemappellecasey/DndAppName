using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
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

if (args.Any(a => string.Equals(a, "--validate-catalog", StringComparison.OrdinalIgnoreCase)))
{
    await ValidateCatalogCoverageAsync(repoRoot);
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

    var existingItems = db.ItemDefinitions.ToList();
    if (existingItems.Count > 0)
    {
        db.ItemDefinitions.RemoveRange(existingItems);
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
        join block in db.SourceBlocks on section.Id equals block.SourceSectionId into blockJoin
        from block in blockJoin.DefaultIfEmpty()
        select new
        {
            book.SourceCode,
            book.VersionTag,
            section.Id,
            section.Title,
            section.SectionOrder,
            section.StartLine,
            section.EndLine,
            Preview = block != null ? block.RawText : string.Empty
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
        var moduleType = InferModuleType(row.Title, row.Preview);
        var displayName = ResolveDisplayName(row.Title, row.Preview, moduleType);
        var abilityBonuses = InferAbilityBonuses(moduleType, row.Title, row.SourceCode);
        var spellClasses = moduleType == "spell" ? InferSpellClasses(row.Preview) : Array.Empty<string>();
        var skillChoices = InferSkillChoices(moduleType, displayName);
        var skillChoiceCount = InferSkillChoiceCount(moduleType, displayName);
        var expertiseChoiceCount = InferExpertiseChoiceCount(moduleType, displayName);
        var fixedSkillProficiencies = InferFixedSkillProficiencies(moduleType, displayName);

        var slug = $"{Slugify(displayName)}-{row.SectionOrder}";
        var moduleId = Guid.NewGuid().ToString("N");
        var module = new RuleModuleEntity
        {
            Id = moduleId,
            ContentSourceId = contentSourceId,
            ModuleType = moduleType,
            Slug = slug,
            DisplayName = displayName,
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
                endLine = row.EndLine,
                abilityBonuses,
                spellClasses,
                fixedSkillProficiencies,
                skillChoices,
                skillChoiceCount,
                expertiseChoiceCount
            })
        };
        db.RuleVariants.Add(variant);
        variantCount++;

        if (moduleType == "item")
        {
            db.ItemDefinitions.Add(new ItemDefinitionEntity
            {
                Id = Guid.NewGuid().ToString("N"),
                RuleModuleId = moduleId,
                ItemType = InferItemType(displayName),
                Rarity = InferItemRarity(displayName, row.Preview),
                RequiresAttunement = row.Preview.Contains("attunement", StringComparison.OrdinalIgnoreCase),
                GoldValue = InferGoldValue(displayName, row.Preview),
                Weight = InferWeight(displayName, row.Preview),
                IsWeapon = InferIsWeapon(displayName, row.Preview),
                DamageDice = InferDamageDice(displayName, row.Preview),
                WeaponAbility = InferWeaponAbility(displayName, row.Preview),
                AttackBonus = InferAttackBonus(row.Preview),
                DamageBonus = InferDamageBonus(row.Preview),
                ChargesModelJson = "{}"
            });
        }
    }

    await db.SaveChangesAsync();

    Console.WriteLine("Normalization complete.");
    Console.WriteLine($"Rule modules created: {moduleCount}");
    Console.WriteLine($"Rule variants created: {variantCount}");
    Console.WriteLine($"SQLite file: {sqlitePath}");
}

static async Task ValidateCatalogCoverageAsync(string repoRoot)
{
    var sqlitePath = Path.Combine(repoRoot, "src", "DndApp.Api", "dndapp-dev.sqlite");
    var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite($"Data Source={sqlitePath}")
        .Options;

    await using var db = new AppDbContext(dbOptions);
    await db.Database.MigrateAsync();

    var validations = new[]
    {
        ("rules-2014", "Rules2014"),
        ("rules-2024", "Rules2024"),
    };
    var errors = new List<string>();

    foreach (var (ruleSystemId, label) in validations)
    {
        var moduleRows = await (
            from module in db.RuleModules
            join source in db.ContentSources on module.ContentSourceId equals source.Id
            where source.RuleSystemId == ruleSystemId
            select module.ModuleType)
            .ToArrayAsync();

        var classCount = moduleRows.Count(x => string.Equals(x, "class", StringComparison.OrdinalIgnoreCase));
        var speciesCount = moduleRows.Count(x =>
            string.Equals(x, "race", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x, "species", StringComparison.OrdinalIgnoreCase));
        var backgroundCount = moduleRows.Count(x =>
            string.Equals(x, "background", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x, "origin", StringComparison.OrdinalIgnoreCase));
        var spellCount = moduleRows.Count(x => string.Equals(x, "spell", StringComparison.OrdinalIgnoreCase));

        var itemCount = await (
            from item in db.ItemDefinitions
            join module in db.RuleModules on item.RuleModuleId equals module.Id
            join source in db.ContentSources on module.ContentSourceId equals source.Id
            where source.RuleSystemId == ruleSystemId
            select item.Id)
            .CountAsync();

        Console.WriteLine($"{label}: classes={classCount}, species={speciesCount}, backgrounds={backgroundCount}, spells={spellCount}, items={itemCount}");

        if (classCount == 0) errors.Add($"{label}: missing class catalog entries.");
        if (speciesCount == 0) errors.Add($"{label}: missing race/species catalog entries.");
        if (backgroundCount == 0) errors.Add($"{label}: missing background/origin catalog entries.");
        if (spellCount == 0) errors.Add($"{label}: missing spell catalog entries.");
        if (itemCount == 0) errors.Add($"{label}: missing item catalog entries.");
    }

    if (errors.Count > 0)
    {
        throw new InvalidOperationException("Catalog validation failed: " + string.Join(" ", errors));
    }

    Console.WriteLine("Catalog validation passed.");
}

static string ComputeSha256(string value)
{
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
    return Convert.ToHexString(bytes);
}

static string InferModuleType(string title, string? preview)
{
    var text = $"{title} {preview}".Trim();

    if (Regex.IsMatch(text, @"\bspell\b", RegexOptions.IgnoreCase))
    {
        return "spell";
    }
    if (Regex.IsMatch(text, @"\bsubclass\b|subclasses|archetype|college|domain|circle|oath|patron", RegexOptions.IgnoreCase))
    {
        return "subclass";
    }
    if (Regex.IsMatch(text, @"\bcharacter class\b|barbarian|bard|cleric|druid|fighter|monk|paladin|ranger|rogue|sorcerer|warlock|wizard|artificer", RegexOptions.IgnoreCase))
    {
        return "class";
    }
    if (Regex.IsMatch(text, @"\brace\b|species|elf|dwarf|halfling|human|dragonborn|gnome|orc|tiefling|aasimar|goliath", RegexOptions.IgnoreCase))
    {
        return title.Contains("species", StringComparison.OrdinalIgnoreCase) ? "species" : "race";
    }
    if (Regex.IsMatch(text, @"\bbackground\b|origin", RegexOptions.IgnoreCase))
    {
        return title.Contains("origin", StringComparison.OrdinalIgnoreCase) ? "origin" : "background";
    }
    if (Regex.IsMatch(text, @"\bfeat\b", RegexOptions.IgnoreCase))
    {
        return "feat";
    }
    if (Regex.IsMatch(text, @"\barmor\b|\bweapon\b|\bshield\b|wondrous|magic item|potion|ring|rod|staff|wand|adventuring gear|equipment|gear", RegexOptions.IgnoreCase))
    {
        return "item";
    }

    if (title.StartsWith("Page ", StringComparison.OrdinalIgnoreCase) && text.Contains("DMG", StringComparison.OrdinalIgnoreCase))
    {
        return "item";
    }

    return "section";
}

static string ResolveDisplayName(string title, string? preview, string moduleType)
{
    var cleanedTitle = Regex.Replace(title ?? string.Empty, @"\s+", " ").Trim();
    if (!cleanedTitle.StartsWith("Page ", StringComparison.OrdinalIgnoreCase))
    {
        return cleanedTitle;
    }

    var text = Regex.Replace(preview ?? string.Empty, @"\s+", " ").Trim();
    if (string.IsNullOrWhiteSpace(text))
    {
        return cleanedTitle;
    }

    string? fromKeywords = moduleType switch
    {
        "class" => new[]
        {
            "Artificer", "Barbarian", "Bard", "Cleric", "Druid", "Fighter",
            "Monk", "Paladin", "Ranger", "Rogue", "Sorcerer", "Warlock", "Wizard"
        }.FirstOrDefault(x => text.Contains(x, StringComparison.OrdinalIgnoreCase)),
        "race" or "species" => new[]
        {
            "Aasimar", "Dragonborn", "Dwarf", "Elf", "Gnome", "Goliath",
            "Halfling", "Human", "Orc", "Tiefling", "Half-Elf", "Half-Orc"
        }.FirstOrDefault(x => text.Contains(x, StringComparison.OrdinalIgnoreCase)),
        "background" or "origin" => new[]
        {
            "Acolyte", "Artisan", "Charlatan", "Criminal", "Entertainer", "Folk Hero",
            "Guild Artisan", "Hermit", "Noble", "Sage", "Sailor", "Soldier", "Urchin"
        }.FirstOrDefault(x => text.Contains(x, StringComparison.OrdinalIgnoreCase)),
        _ => null
    };
    if (!string.IsNullOrWhiteSpace(fromKeywords))
    {
        return fromKeywords;
    }

    if (moduleType == "spell")
    {
        var spellMatch = Regex.Match(text, @"([A-Z][A-Za-z' -]{2,40})");
        if (spellMatch.Success)
        {
            return spellMatch.Groups[1].Value.Trim();
        }
    }

    if (moduleType == "item")
    {
        var itemMatch = Regex.Match(text, @"([A-Z][A-Za-z' -]{2,60})");
        if (itemMatch.Success)
        {
            return itemMatch.Groups[1].Value.Trim();
        }
    }

    var sentence = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
    if (!string.IsNullOrWhiteSpace(sentence))
    {
        return sentence.Length > 80 ? sentence[..80].Trim() : sentence;
    }

    return cleanedTitle;
}

static IReadOnlyDictionary<string, int> InferAbilityBonuses(string moduleType, string title, string sourceCode)
{
    if (!string.Equals(moduleType, "race", StringComparison.OrdinalIgnoreCase))
    {
        return new Dictionary<string, int>();
    }

    if (!sourceCode.EndsWith("2014", StringComparison.OrdinalIgnoreCase))
    {
        return new Dictionary<string, int>();
    }

    var normalized = title.ToLowerInvariant();
    if (normalized.Contains("dwarf"))
    {
        return new Dictionary<string, int> { ["Constitution"] = 2 };
    }
    if (normalized.Contains("elf"))
    {
        return new Dictionary<string, int> { ["Dexterity"] = 2 };
    }
    if (normalized.Contains("halfling"))
    {
        return new Dictionary<string, int> { ["Dexterity"] = 2 };
    }
    if (normalized.Contains("human"))
    {
        return new Dictionary<string, int>
        {
            ["Strength"] = 1,
            ["Dexterity"] = 1,
            ["Constitution"] = 1,
            ["Intelligence"] = 1,
            ["Wisdom"] = 1,
            ["Charisma"] = 1
        };
    }
    if (normalized.Contains("dragonborn"))
    {
        return new Dictionary<string, int> { ["Strength"] = 2, ["Charisma"] = 1 };
    }
    if (normalized.Contains("gnome"))
    {
        return new Dictionary<string, int> { ["Intelligence"] = 2 };
    }
    if (normalized.Contains("half-elf"))
    {
        return new Dictionary<string, int> { ["Charisma"] = 2 };
    }
    if (normalized.Contains("half-orc"))
    {
        return new Dictionary<string, int> { ["Strength"] = 2, ["Constitution"] = 1 };
    }
    if (normalized.Contains("tiefling"))
    {
        return new Dictionary<string, int> { ["Charisma"] = 2, ["Intelligence"] = 1 };
    }

    return new Dictionary<string, int>();
}

static IReadOnlyList<string> InferSpellClasses(string? preview)
{
    if (string.IsNullOrWhiteSpace(preview))
    {
        return Array.Empty<string>();
    }

    var classes = new[]
    {
        "Artificer", "Barbarian", "Bard", "Cleric", "Druid",
        "Fighter", "Monk", "Paladin", "Ranger", "Rogue",
        "Sorcerer", "Warlock", "Wizard"
    };

    return classes
        .Where(c => preview.Contains(c, StringComparison.OrdinalIgnoreCase))
        .ToArray();
}

static string InferItemType(string title)
{
    var normalized = title.ToLowerInvariant();
    if (normalized.Contains("armor") || normalized.Contains("shield"))
    {
        return "Armor";
    }
    if (normalized.Contains("weapon"))
    {
        return "Weapon";
    }
    if (normalized.Contains("potion"))
    {
        return "Potion";
    }
    if (normalized.Contains("ring"))
    {
        return "Ring";
    }
    if (normalized.Contains("wand") || normalized.Contains("rod") || normalized.Contains("staff"))
    {
        return "Focus";
    }
    return "Wondrous Item";
}

static string InferItemRarity(string title, string? preview)
{
    var text = $"{title} {preview}".ToLowerInvariant();
    foreach (var rarity in new[] { "common", "uncommon", "rare", "very rare", "legendary", "artifact" })
    {
        if (text.Contains(rarity, StringComparison.Ordinal))
        {
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(rarity);
        }
    }

    return "Unknown";
}

static decimal InferGoldValue(string title, string? preview)
{
    var text = $"{title} {preview}";
    var gpMatch = Regex.Match(text, @"(\d+(?:\.\d+)?)\s*gp", RegexOptions.IgnoreCase);
    if (gpMatch.Success && decimal.TryParse(gpMatch.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var gp))
    {
        return gp;
    }
    var spMatch = Regex.Match(text, @"(\d+(?:\.\d+)?)\s*sp", RegexOptions.IgnoreCase);
    if (spMatch.Success && decimal.TryParse(spMatch.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var sp))
    {
        return Math.Round(sp / 10m, 2);
    }
    return 0m;
}

static decimal InferWeight(string title, string? preview)
{
    var text = $"{title} {preview}";
    var match = Regex.Match(text, @"(\d+(?:\.\d+)?)\s*(?:lb|lbs|pounds?)", RegexOptions.IgnoreCase);
    if (match.Success && decimal.TryParse(match.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var weight))
    {
        return weight;
    }
    return 0m;
}

static bool InferIsWeapon(string title, string? preview)
{
    return Regex.IsMatch($"{title} {preview}", @"weapon|sword|axe|bow|dagger|mace|spear|crossbow|hammer|flail|halberd|pike|rapier|scimitar|whip|club", RegexOptions.IgnoreCase);
}

static string InferDamageDice(string title, string? preview)
{
    var text = $"{title} {preview}";
    var dice = Regex.Match(text, @"\b\d+d\d+\b", RegexOptions.IgnoreCase);
    if (dice.Success)
    {
        return dice.Value.ToLowerInvariant();
    }
    return InferIsWeapon(title, preview) ? "1d6" : string.Empty;
}

static string InferWeaponAbility(string title, string? preview)
{
    var text = $"{title} {preview}";
    if (Regex.IsMatch(text, @"finesse|ranged|bow|crossbow|dart|dagger", RegexOptions.IgnoreCase))
    {
        return "Dexterity";
    }
    return InferIsWeapon(title, preview) ? "Strength" : string.Empty;
}

static int InferAttackBonus(string? preview)
{
    if (string.IsNullOrWhiteSpace(preview))
    {
        return 0;
    }
    var match = Regex.Match(preview, @"\+(\d+)\s*to hit", RegexOptions.IgnoreCase);
    return match.Success && int.TryParse(match.Groups[1].Value, out var value) ? value : 0;
}

static int InferDamageBonus(string? preview)
{
    if (string.IsNullOrWhiteSpace(preview))
    {
        return 0;
    }
    var match = Regex.Match(preview, @"\d+d\d+\s*\+\s*(\d+)", RegexOptions.IgnoreCase);
    return match.Success && int.TryParse(match.Groups[1].Value, out var value) ? value : 0;
}

static IReadOnlyList<string> InferSkillChoices(string moduleType, string displayName)
{
    if (!string.Equals(moduleType, "class", StringComparison.OrdinalIgnoreCase))
    {
        return Array.Empty<string>();
    }

    return displayName.ToLowerInvariant() switch
    {
        var name when name.Contains("barbarian") => new[] { "Animal Handling", "Athletics", "Intimidation", "Nature", "Perception", "Survival" },
        var name when name.Contains("bard") => new[] { "Acrobatics", "Animal Handling", "Arcana", "Athletics", "Deception", "History", "Insight", "Intimidation", "Investigation", "Medicine", "Nature", "Perception", "Performance", "Persuasion", "Religion", "Sleight of Hand", "Stealth", "Survival" },
        var name when name.Contains("cleric") => new[] { "History", "Insight", "Medicine", "Persuasion", "Religion" },
        var name when name.Contains("druid") => new[] { "Arcana", "Animal Handling", "Insight", "Medicine", "Nature", "Perception", "Religion", "Survival" },
        var name when name.Contains("fighter") => new[] { "Acrobatics", "Animal Handling", "Athletics", "History", "Insight", "Intimidation", "Perception", "Survival" },
        var name when name.Contains("monk") => new[] { "Acrobatics", "Athletics", "History", "Insight", "Religion", "Stealth" },
        var name when name.Contains("paladin") => new[] { "Athletics", "Insight", "Intimidation", "Medicine", "Persuasion", "Religion" },
        var name when name.Contains("ranger") => new[] { "Animal Handling", "Athletics", "Insight", "Investigation", "Nature", "Perception", "Stealth", "Survival" },
        var name when name.Contains("rogue") => new[] { "Acrobatics", "Athletics", "Deception", "Insight", "Intimidation", "Investigation", "Perception", "Performance", "Persuasion", "Sleight of Hand", "Stealth" },
        var name when name.Contains("sorcerer") => new[] { "Arcana", "Deception", "Insight", "Intimidation", "Persuasion", "Religion" },
        var name when name.Contains("warlock") => new[] { "Arcana", "Deception", "History", "Intimidation", "Investigation", "Nature", "Religion" },
        var name when name.Contains("wizard") => new[] { "Arcana", "History", "Insight", "Investigation", "Medicine", "Religion" },
        _ => Array.Empty<string>()
    };
}

static int InferSkillChoiceCount(string moduleType, string displayName)
{
    if (!string.Equals(moduleType, "class", StringComparison.OrdinalIgnoreCase))
    {
        return 0;
    }

    return displayName.ToLowerInvariant() switch
    {
        var name when name.Contains("rogue") => 4,
        var name when name.Contains("bard") => 3,
        var name when name.Contains("ranger") => 3,
        var name when name.Contains("fighter") => 2,
        var name when name.Contains("barbarian") => 2,
        var name when name.Contains("cleric") => 2,
        var name when name.Contains("druid") => 2,
        var name when name.Contains("monk") => 2,
        var name when name.Contains("paladin") => 2,
        var name when name.Contains("sorcerer") => 2,
        var name when name.Contains("warlock") => 2,
        var name when name.Contains("wizard") => 2,
        _ => 0
    };
}

static int InferExpertiseChoiceCount(string moduleType, string displayName)
{
    if (!string.Equals(moduleType, "class", StringComparison.OrdinalIgnoreCase))
    {
        return 0;
    }

    return displayName.ToLowerInvariant() switch
    {
        var name when name.Contains("rogue") => 2,
        var name when name.Contains("bard") => 2,
        _ => 0
    };
}

static IReadOnlyList<string> InferFixedSkillProficiencies(string moduleType, string displayName)
{
    if (!string.Equals(moduleType, "background", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(moduleType, "origin", StringComparison.OrdinalIgnoreCase))
    {
        return Array.Empty<string>();
    }

    return displayName.ToLowerInvariant() switch
    {
        var name when name.Contains("acolyte") => new[] { "Insight", "Religion" },
        var name when name.Contains("criminal") => new[] { "Deception", "Stealth" },
        var name when name.Contains("entertainer") => new[] { "Acrobatics", "Performance" },
        var name when name.Contains("folk hero") => new[] { "Animal Handling", "Survival" },
        var name when name.Contains("sage") => new[] { "Arcana", "History" },
        var name when name.Contains("soldier") => new[] { "Athletics", "Intimidation" },
        var name when name.Contains("urchin") => new[] { "Sleight of Hand", "Stealth" },
        _ => Array.Empty<string>()
    };
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
