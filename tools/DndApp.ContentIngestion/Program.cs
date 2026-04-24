using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using DndApp.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var repoRoot = ResolveRepoRoot(args);
var outputRoot = Path.Combine(repoRoot, "data");
var now = DateTimeOffset.UtcNow;
const int Rules2014EditionYear = 2014;
const int Rules2024EditionYear = 2024;

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

    var payloads = await LoadIngestionPayloadsForImportAsync(repoRoot);

    var importedSections = 0;
    var importedBlocks = 0;
    var reviewCount = 0;

    foreach (var payload in payloads.OrderBy(x => x.SourceCode, StringComparer.OrdinalIgnoreCase))
    {
        var json = JsonSerializer.Serialize(payload);

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
        new ContentSourceEntity { Id = "WIKIDOT2014", RuleSystemId = "rules-2014", Code = "2014WIKIDOT", Name = "Wikidot Archive 2014" },
        new ContentSourceEntity { Id = "WIKIDOT2024", RuleSystemId = "rules-2024", Code = "2024WIKIDOT", Name = "Wikidot Archive 2024" },
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

    var existingCharacterInventoryItems = db.CharacterInventoryItems.ToList();
    if (existingCharacterInventoryItems.Count > 0)
    {
        db.CharacterInventoryItems.RemoveRange(existingCharacterInventoryItems);
    }

    var existingItems = db.ItemDefinitions.ToList();
    if (existingItems.Count > 0)
    {
        db.ItemDefinitions.RemoveRange(existingItems);
    }

    var existingRaces2014 = db.Races2014.ToList();
    if (existingRaces2014.Count > 0)
    {
        db.Races2014.RemoveRange(existingRaces2014);
    }

    var existingSpecies2024 = db.Species2024.ToList();
    if (existingSpecies2024.Count > 0)
    {
        db.Species2024.RemoveRange(existingSpecies2024);
    }

    var existingBackgrounds2014 = db.Backgrounds2014.ToList();
    if (existingBackgrounds2014.Count > 0)
    {
        db.Backgrounds2014.RemoveRange(existingBackgrounds2014);
    }

    var existingBackgrounds2024 = db.Backgrounds2024.ToList();
    if (existingBackgrounds2024.Count > 0)
    {
        db.Backgrounds2024.RemoveRange(existingBackgrounds2024);
    }

    var existingFeats2014 = db.Feats2014.ToList();
    if (existingFeats2014.Count > 0)
    {
        db.Feats2014.RemoveRange(existingFeats2014);
    }

    var existingFeats2024 = db.Feats2024.ToList();
    if (existingFeats2024.Count > 0)
    {
        db.Feats2024.RemoveRange(existingFeats2024);
    }

    var existingSpells = db.Spells.ToList();
    if (existingSpells.Count > 0)
    {
        db.Spells.RemoveRange(existingSpells);
    }

    var existingClasses2014 = db.Classes2014.ToList();
    if (existingClasses2014.Count > 0)
    {
        db.Classes2014.RemoveRange(existingClasses2014);
    }

    var existingClasses2024 = db.Classes2024.ToList();
    if (existingClasses2024.Count > 0)
    {
        db.Classes2024.RemoveRange(existingClasses2024);
    }

    var existingSubclasses2014 = db.Subclasses2014.ToList();
    if (existingSubclasses2014.Count > 0)
    {
        db.Subclasses2014.RemoveRange(existingSubclasses2014);
    }

    var existingSubclasses2024 = db.Subclasses2024.ToList();
    if (existingSubclasses2024.Count > 0)
    {
        db.Subclasses2024.RemoveRange(existingSubclasses2024);
    }

    var existingItems2014 = db.Items2014.ToList();
    if (existingItems2014.Count > 0)
    {
        db.Items2014.RemoveRange(existingItems2014);
    }

    var existingItems2024 = db.Items2024.ToList();
    if (existingItems2024.Count > 0)
    {
        db.Items2024.RemoveRange(existingItems2024);
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
            Preview = block != null ? block.RawText : string.Empty,
            ParseConfidence = block != null ? block.ParseConfidence : (decimal?)null
        })
        .OrderBy(x => x.SourceCode)
        .ThenBy(x => x.SectionOrder)
        .ToListAsync();

    var moduleCount = 0;
    var variantCount = 0;
    var race2014Count = 0;
    var species2024Count = 0;
    var background2014Count = 0;
    var background2024Count = 0;
    var feat2014Count = 0;
    var feat2024Count = 0;
    var spell2014Count = 0;
    var spell2024Count = 0;
    var class2014Count = 0;
    var class2024Count = 0;
    var subclass2014Count = 0;
    var subclass2024Count = 0;
    var item2014Count = 0;
    var item2024Count = 0;
    var classIdsByName2014 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    var classIdsByName2024 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    var pendingSubclassRows = new List<PendingSubclassRow>();

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

        var moduleDescription = BuildCatalogDescription(row.Preview);
        var inferredLanguages = InferLanguages(row.Preview);
        var inferredTraits = InferTraits(row.Preview);
        var inferredBackgroundTools = InferBackgroundTools(row.Preview);
        var inferredBackgroundEquipment = InferBackgroundEquipment(row.Preview);
        var inferredFeatCategory = InferFeatCategory(row.Preview);
        var inferredSpellLevel = moduleType == "spell" ? InferSpellLevel(row.Title, row.Preview) : 0;
        var inferredSpellSchool = moduleType == "spell" ? InferSpellSchool(row.Preview) : string.Empty;
        var inferredCastingTime = moduleType == "spell" ? InferCastingTime(row.Preview) : string.Empty;
        var inferredRange = moduleType == "spell" ? InferRangeText(row.Preview) : string.Empty;
        var inferredDuration = moduleType == "spell" ? InferDurationText(row.Preview) : string.Empty;
        var inferredRitual = moduleType == "spell" && Regex.IsMatch(row.Preview ?? string.Empty, @"\britual\b", RegexOptions.IgnoreCase);
        var inferredConcentration = moduleType == "spell" && Regex.IsMatch(row.Preview ?? string.Empty, @"\bconcentration\b", RegexOptions.IgnoreCase);
        var inferredSpellConfidence = moduleType == "spell" ? InferSpellIngestionConfidence(row.Preview, row.ParseConfidence) : 0m;
        var payloadJson = variant.PayloadJson;

        if (moduleType is "race" or "subrace" && string.Equals(ruleSystemId, "rules-2014", StringComparison.Ordinal))
        {
            db.Races2014.Add(new Race2014Entity
            {
                Id = moduleId,
                ContentSourceId = contentSourceId,
                LegacyRuleModuleId = moduleId,
                Slug = slug,
                Name = displayName,
                IsSubrace = string.Equals(moduleType, "subrace", StringComparison.OrdinalIgnoreCase),
                ParentRaceSlug = string.Empty,
                Description = moduleDescription,
                AbilityBonusesJson = JsonSerializer.Serialize(abilityBonuses),
                LanguagesJson = JsonSerializer.Serialize(inferredLanguages),
                TraitsJson = JsonSerializer.Serialize(inferredTraits),
                EditionPayloadJson = payloadJson
            });
            race2014Count++;
        }
        else if ((moduleType == "species" || moduleType == "race") && string.Equals(ruleSystemId, "rules-2024", StringComparison.Ordinal))
        {
            db.Species2024.Add(new Species2024Entity
            {
                Id = moduleId,
                ContentSourceId = contentSourceId,
                LegacyRuleModuleId = moduleId,
                Slug = slug,
                Name = displayName,
                Description = moduleDescription,
                AbilityBonusesJson = JsonSerializer.Serialize(abilityBonuses),
                LanguagesJson = JsonSerializer.Serialize(inferredLanguages),
                TraitsJson = JsonSerializer.Serialize(inferredTraits),
                EditionPayloadJson = payloadJson
            });
            species2024Count++;
        }
        else if ((moduleType == "background" || moduleType == "origin") && string.Equals(ruleSystemId, "rules-2014", StringComparison.Ordinal))
        {
            db.Backgrounds2014.Add(new Background2014Entity
            {
                Id = moduleId,
                ContentSourceId = contentSourceId,
                LegacyRuleModuleId = moduleId,
                Slug = slug,
                Name = displayName,
                Description = moduleDescription,
                SkillProficienciesJson = JsonSerializer.Serialize(fixedSkillProficiencies),
                ToolProficienciesJson = JsonSerializer.Serialize(inferredBackgroundTools),
                LanguageChoicesJson = JsonSerializer.Serialize(inferredLanguages),
                EquipmentJson = JsonSerializer.Serialize(inferredBackgroundEquipment),
                EditionPayloadJson = payloadJson
            });
            background2014Count++;
        }
        else if ((moduleType == "background" || moduleType == "origin") && string.Equals(ruleSystemId, "rules-2024", StringComparison.Ordinal))
        {
            db.Backgrounds2024.Add(new Background2024Entity
            {
                Id = moduleId,
                ContentSourceId = contentSourceId,
                LegacyRuleModuleId = moduleId,
                Slug = slug,
                Name = displayName,
                Description = moduleDescription,
                SkillProficienciesJson = JsonSerializer.Serialize(fixedSkillProficiencies),
                ToolProficienciesJson = JsonSerializer.Serialize(inferredBackgroundTools),
                LanguageChoicesJson = JsonSerializer.Serialize(inferredLanguages),
                GrantedFeatSlug = string.Empty,
                EditionPayloadJson = payloadJson
            });
            background2024Count++;
        }
        else if (moduleType == "feat" && string.Equals(ruleSystemId, "rules-2014", StringComparison.Ordinal))
        {
            db.Feats2014.Add(new Feat2014Entity
            {
                Id = moduleId,
                ContentSourceId = contentSourceId,
                LegacyRuleModuleId = moduleId,
                Slug = slug,
                Name = displayName,
                Description = moduleDescription,
                PrerequisitesJson = "{}",
                EditionPayloadJson = payloadJson
            });
            feat2014Count++;
        }
        else if (moduleType == "feat" && string.Equals(ruleSystemId, "rules-2024", StringComparison.Ordinal))
        {
            db.Feats2024.Add(new Feat2024Entity
            {
                Id = moduleId,
                ContentSourceId = contentSourceId,
                LegacyRuleModuleId = moduleId,
                Slug = slug,
                Name = displayName,
                Description = moduleDescription,
                Category = inferredFeatCategory,
                PrerequisitesJson = "{}",
                EditionPayloadJson = payloadJson
            });
            feat2024Count++;
        }
        else if (moduleType == "spell")
        {
            var editionYear = string.Equals(ruleSystemId, "rules-2014", StringComparison.Ordinal)
                ? Rules2014EditionYear
                : Rules2024EditionYear;
            var statBlockJson = JsonSerializer.Serialize(new
            {
                inferredSpellLevel,
                inferredSpellSchool,
                inferredCastingTime,
                inferredRange,
                inferredDuration,
                inferredRitual,
                inferredConcentration,
                spellClasses
            });
            db.Spells.Add(new SpellEntity
            {
                Id = moduleId,
                ContentSourceId = contentSourceId,
                LegacyRuleModuleId = moduleId,
                SourceSectionId = row.Id,
                Slug = slug,
                Name = displayName,
                EditionYear = editionYear,
                Level = inferredSpellLevel,
                School = inferredSpellSchool,
                CastingTime = inferredCastingTime,
                RangeText = inferredRange,
                Duration = inferredDuration,
                Ritual = inferredRitual,
                Concentration = inferredConcentration,
                Description = moduleDescription,
                IngestionConfidence = inferredSpellConfidence,
                StatBlockJson = statBlockJson,
                EditionPayloadJson = payloadJson
            });
            if (editionYear == Rules2014EditionYear)
            {
                spell2014Count++;
            }
            else
            {
                spell2024Count++;
            }
        }
        else if (moduleType == "class")
        {
            var classPayloadJson = JsonSerializer.Serialize(new
            {
                sourceSectionId = row.Id,
                hitDie = InferClassHitDie(displayName, row.Preview),
                primaryAbilities = InferClassPrimaryAbilities(displayName),
                savingThrowAbilities = InferClassSavingThrowAbilities(displayName)
            });
            if (string.Equals(ruleSystemId, "rules-2014", StringComparison.Ordinal))
            {
                db.Classes2014.Add(new Class2014Entity
                {
                    Id = moduleId,
                    ContentSourceId = contentSourceId,
                    LegacyRuleModuleId = moduleId,
                    Slug = slug,
                    Name = displayName,
                    Description = moduleDescription,
                    HitDie = InferClassHitDie(displayName, row.Preview),
                    PrimaryAbilityJson = JsonSerializer.Serialize(InferClassPrimaryAbilities(displayName)),
                    SavingThrowAbilitiesJson = JsonSerializer.Serialize(InferClassSavingThrowAbilities(displayName)),
                    SkillProficienciesJson = JsonSerializer.Serialize(new { fixedSkills = Array.Empty<string>(), choices = skillChoices, choose = skillChoiceCount }),
                    EditionPayloadJson = classPayloadJson
                });
                classIdsByName2014[displayName] = moduleId;
                class2014Count++;
            }
            else
            {
                db.Classes2024.Add(new Class2024Entity
                {
                    Id = moduleId,
                    ContentSourceId = contentSourceId,
                    LegacyRuleModuleId = moduleId,
                    Slug = slug,
                    Name = displayName,
                    Description = moduleDescription,
                    HitDie = InferClassHitDie(displayName, row.Preview),
                    PrimaryAbilityJson = JsonSerializer.Serialize(InferClassPrimaryAbilities(displayName)),
                    SavingThrowAbilitiesJson = JsonSerializer.Serialize(InferClassSavingThrowAbilities(displayName)),
                    SkillProficienciesJson = JsonSerializer.Serialize(new { fixedSkills = Array.Empty<string>(), choices = skillChoices, choose = skillChoiceCount }),
                    EditionPayloadJson = classPayloadJson
                });
                classIdsByName2024[displayName] = moduleId;
                class2024Count++;
            }
        }
        else if (moduleType == "subclass")
        {
            pendingSubclassRows.Add(new PendingSubclassRow(
                ModuleId: moduleId,
                ContentSourceId: contentSourceId,
                RuleSystemId: ruleSystemId,
                Slug: slug,
                DisplayName: displayName,
                Description: moduleDescription,
                Preview: row.Preview,
                PayloadJson: payloadJson));
        }

        if (moduleType == "item")
        {
            var itemDefinitionId = Guid.NewGuid().ToString("N");
            db.ItemDefinitions.Add(new ItemDefinitionEntity
            {
                Id = itemDefinitionId,
                RuleModuleId = moduleId,
                ItemType = InferItemType(displayName),
                Rarity = InferItemRarity(displayName, row.Preview),
                RequiresAttunement = (row.Preview ?? string.Empty).Contains("attunement", StringComparison.OrdinalIgnoreCase),
                GoldValue = InferGoldValue(displayName, row.Preview),
                Weight = InferWeight(displayName, row.Preview),
                IsWeapon = InferIsWeapon(displayName, row.Preview),
                DamageDice = InferDamageDice(displayName, row.Preview),
                WeaponAbility = InferWeaponAbility(displayName, row.Preview),
                AttackBonus = InferAttackBonus(row.Preview),
                DamageBonus = InferDamageBonus(row.Preview),
                ChargesModelJson = "{}"
            });

            if (string.Equals(ruleSystemId, "rules-2014", StringComparison.Ordinal))
            {
                db.Items2014.Add(new Item2014Entity
                {
                    Id = itemDefinitionId,
                    ContentSourceId = contentSourceId,
                    LegacyRuleModuleId = moduleId,
                    LegacyItemDefinitionId = itemDefinitionId,
                    Slug = slug,
                    Name = displayName,
                    ItemType = InferItemType(displayName),
                    Rarity = InferItemRarity(displayName, row.Preview),
                    RequiresAttunement = (row.Preview ?? string.Empty).Contains("attunement", StringComparison.OrdinalIgnoreCase),
                    GoldValue = InferGoldValue(displayName, row.Preview),
                    Weight = InferWeight(displayName, row.Preview),
                    IsWeapon = InferIsWeapon(displayName, row.Preview),
                    DamageDice = InferDamageDice(displayName, row.Preview),
                    WeaponAbility = InferWeaponAbility(displayName, row.Preview),
                    AttackBonus = InferAttackBonus(row.Preview),
                    DamageBonus = InferDamageBonus(row.Preview),
                    Description = moduleDescription,
                    EditionPayloadJson = payloadJson
                });
                item2014Count++;
            }
            else
            {
                db.Items2024.Add(new Item2024Entity
                {
                    Id = itemDefinitionId,
                    ContentSourceId = contentSourceId,
                    LegacyRuleModuleId = moduleId,
                    LegacyItemDefinitionId = itemDefinitionId,
                    Slug = slug,
                    Name = displayName,
                    ItemType = InferItemType(displayName),
                    Rarity = InferItemRarity(displayName, row.Preview),
                    RequiresAttunement = (row.Preview ?? string.Empty).Contains("attunement", StringComparison.OrdinalIgnoreCase),
                    GoldValue = InferGoldValue(displayName, row.Preview),
                    Weight = InferWeight(displayName, row.Preview),
                    IsWeapon = InferIsWeapon(displayName, row.Preview),
                    DamageDice = InferDamageDice(displayName, row.Preview),
                    WeaponAbility = InferWeaponAbility(displayName, row.Preview),
                    AttackBonus = InferAttackBonus(row.Preview),
                    DamageBonus = InferDamageBonus(row.Preview),
                    Description = moduleDescription,
                    EditionPayloadJson = payloadJson
                });
                item2024Count++;
            }
        }
    }

    foreach (var subclass in pendingSubclassRows)
    {
        var classLookup = string.Equals(subclass.RuleSystemId, "rules-2014", StringComparison.Ordinal)
            ? classIdsByName2014
            : classIdsByName2024;
        var parentClassName = InferSubclassParentClassName(subclass.DisplayName, subclass.Preview);
        var parentClassId = !string.IsNullOrWhiteSpace(parentClassName) && classLookup.TryGetValue(parentClassName, out var resolvedClassId)
            ? resolvedClassId
            : classLookup.Values.FirstOrDefault() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(parentClassId))
        {
            continue;
        }

        var subclassFeatureStartLevel = InferSubclassFeatureStartLevel(parentClassName, subclass.DisplayName, subclass.RuleSystemId);
        var subclassPayloadJson = JsonSerializer.Serialize(new
        {
            parentClassName,
            source = "heuristic",
            subclass.PayloadJson
        });
        if (string.Equals(subclass.RuleSystemId, "rules-2014", StringComparison.Ordinal))
        {
            db.Subclasses2014.Add(new Subclass2014Entity
            {
                Id = subclass.ModuleId,
                ContentSourceId = subclass.ContentSourceId,
                LegacyRuleModuleId = subclass.ModuleId,
                ParentClassId = parentClassId,
                Slug = subclass.Slug,
                Name = subclass.DisplayName,
                SubclassFeatureStartLevel = subclassFeatureStartLevel,
                Description = subclass.Description,
                EditionPayloadJson = subclassPayloadJson
            });
            subclass2014Count++;
        }
        else
        {
            db.Subclasses2024.Add(new Subclass2024Entity
            {
                Id = subclass.ModuleId,
                ContentSourceId = subclass.ContentSourceId,
                LegacyRuleModuleId = subclass.ModuleId,
                ParentClassId = parentClassId,
                Slug = subclass.Slug,
                Name = subclass.DisplayName,
                SubclassFeatureStartLevel = subclassFeatureStartLevel,
                Description = subclass.Description,
                EditionPayloadJson = subclassPayloadJson
            });
            subclass2024Count++;
        }
    }

    await db.SaveChangesAsync();

    Console.WriteLine("Normalization complete.");
    Console.WriteLine($"Rule modules created: {moduleCount}");
    Console.WriteLine($"Rule variants created: {variantCount}");
    Console.WriteLine($"Catalog split rows: race2014={race2014Count}, species2024={species2024Count}, background2014={background2014Count}, background2024={background2024Count}");
    Console.WriteLine($"Catalog split rows: feat2014={feat2014Count}, feat2024={feat2024Count}, spell2014={spell2014Count}, spell2024={spell2024Count}, class2014={class2014Count}, class2024={class2024Count}, subclass2014={subclass2014Count}, subclass2024={subclass2024Count}, item2014={item2014Count}, item2024={item2024Count}");
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
        var subclassCount = moduleRows.Count(x => string.Equals(x, "subclass", StringComparison.OrdinalIgnoreCase));
        var speciesCount = moduleRows.Count(x =>
            string.Equals(x, "race", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x, "species", StringComparison.OrdinalIgnoreCase));
        var backgroundCount = moduleRows.Count(x =>
            string.Equals(x, "background", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x, "origin", StringComparison.OrdinalIgnoreCase));
        var featCount = moduleRows.Count(x => string.Equals(x, "feat", StringComparison.OrdinalIgnoreCase));
        var spellCount = moduleRows.Count(x => string.Equals(x, "spell", StringComparison.OrdinalIgnoreCase));

        var itemCount = await (
            from item in db.ItemDefinitions
            join module in db.RuleModules on item.RuleModuleId equals module.Id
            join source in db.ContentSources on module.ContentSourceId equals source.Id
            where source.RuleSystemId == ruleSystemId
            select item.Id)
            .CountAsync();

        var splitRaceCount = ruleSystemId == "rules-2014"
            ? await db.Races2014.CountAsync()
            : await db.Species2024.CountAsync();
        var splitBackgroundCount = ruleSystemId == "rules-2014"
            ? await db.Backgrounds2014.CountAsync()
            : await db.Backgrounds2024.CountAsync();
        var splitFeatCount = ruleSystemId == "rules-2014"
            ? await db.Feats2014.CountAsync()
            : await db.Feats2024.CountAsync();
        var splitSpellCount = ruleSystemId == "rules-2014"
            ? await db.Spells.CountAsync(x => x.EditionYear == Rules2014EditionYear)
            : await db.Spells.CountAsync(x => x.EditionYear == Rules2024EditionYear);
        var splitClassCount = ruleSystemId == "rules-2014"
            ? await db.Classes2014.CountAsync()
            : await db.Classes2024.CountAsync();
        var splitSubclassCount = ruleSystemId == "rules-2014"
            ? await db.Subclasses2014.CountAsync()
            : await db.Subclasses2024.CountAsync();
        var splitItemCount = ruleSystemId == "rules-2014"
            ? await db.Items2014.CountAsync()
            : await db.Items2024.CountAsync();

        Console.WriteLine($"{label}: classes={classCount}, subclasses={subclassCount}, species={speciesCount}, backgrounds={backgroundCount}, spells={spellCount}, items={itemCount}");
        Console.WriteLine($"{label}: split tables classes={splitClassCount}, subclasses={splitSubclassCount}, races/species={splitRaceCount}, backgrounds={splitBackgroundCount}, feats={splitFeatCount}, spells={splitSpellCount}, items={splitItemCount}");

        if (classCount == 0) errors.Add($"{label}: missing class catalog entries.");
        if (subclassCount == 0) errors.Add($"{label}: missing subclass catalog entries.");
        if (speciesCount == 0) errors.Add($"{label}: missing race/species catalog entries.");
        if (backgroundCount == 0) errors.Add($"{label}: missing background/origin catalog entries.");
        if (spellCount == 0) errors.Add($"{label}: missing spell catalog entries.");
        if (itemCount == 0) errors.Add($"{label}: missing item catalog entries.");
        if (classCount > 0 && splitClassCount == 0) errors.Add($"{label}: missing split class table entries.");
        if (subclassCount > 0 && splitSubclassCount == 0) errors.Add($"{label}: missing split subclass table entries.");
        if (speciesCount > 0 && splitRaceCount == 0) errors.Add($"{label}: missing split race/species table entries.");
        if (backgroundCount > 0 && splitBackgroundCount == 0) errors.Add($"{label}: missing split background table entries.");
        if (featCount > 0 && splitFeatCount == 0) errors.Add($"{label}: missing split feat table entries.");
        if (spellCount > 0 && splitSpellCount == 0) errors.Add($"{label}: missing split spell table entries.");
        if (itemCount > 0 && splitItemCount == 0) errors.Add($"{label}: missing split item table entries.");

        if (ruleSystemId == "rules-2014")
        {
            var orphanCount = await (
                from subclass in db.Subclasses2014
                join parent in db.Classes2014 on subclass.ParentClassId equals parent.Id into parentJoin
                from parent in parentJoin.DefaultIfEmpty()
                where subclass.ParentClassId == string.Empty || parent == null
                select subclass.Id)
                .CountAsync();
            if (orphanCount > 0) errors.Add($"{label}: subclass rows missing valid parent class links.");
        }
        else
        {
            var orphanCount = await (
                from subclass in db.Subclasses2024
                join parent in db.Classes2024 on subclass.ParentClassId equals parent.Id into parentJoin
                from parent in parentJoin.DefaultIfEmpty()
                where subclass.ParentClassId == string.Empty || parent == null
                select subclass.Id)
                .CountAsync();
            if (orphanCount > 0) errors.Add($"{label}: subclass rows missing valid parent class links.");
        }
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
    var previewText = preview ?? string.Empty;

    if (Regex.IsMatch(title, @"\bspells?\b", RegexOptions.IgnoreCase) ||
        Regex.IsMatch(previewText, @"\bCasting Time[:\s]|Range[:\s]|Duration[:\s]|Components?[:\s]\b", RegexOptions.IgnoreCase) ||
        Regex.IsMatch(previewText, @"\b(cantrip|[1-9](?:st|nd|rd|th)?[- ]level)\b", RegexOptions.IgnoreCase))
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

static string BuildCatalogDescription(string? preview)
{
    if (string.IsNullOrWhiteSpace(preview))
    {
        return string.Empty;
    }

    var compact = Regex.Replace(preview, @"\s+", " ").Trim();
    return compact;
}

static IReadOnlyList<string> InferLanguages(string? preview)
{
    if (string.IsNullOrWhiteSpace(preview))
    {
        return Array.Empty<string>();
    }

    var known = new[] { "Common", "Elvish", "Dwarvish", "Orc", "Gnomish", "Halfling", "Draconic", "Infernal", "Celestial", "Sylvan" };
    return known
        .Where(x => preview.Contains(x, StringComparison.OrdinalIgnoreCase))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

static IReadOnlyList<string> InferTraits(string? preview)
{
    if (string.IsNullOrWhiteSpace(preview))
    {
        return Array.Empty<string>();
    }

    var known = new[] { "Darkvision", "Fey Ancestry", "Lucky", "Brave", "Relentless Endurance", "Keen Senses", "Trance" };
    return known
        .Where(x => preview.Contains(x, StringComparison.OrdinalIgnoreCase))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

static IReadOnlyList<string> InferBackgroundTools(string? preview)
{
    if (string.IsNullOrWhiteSpace(preview))
    {
        return Array.Empty<string>();
    }

    var known = new[]
    {
        "Thieves' Tools", "Disguise Kit", "Forgery Kit", "Herbalism Kit", "Navigator's Tools", "Vehicles (Land)", "Vehicles (Water)"
    };
    return known
        .Where(x => preview.Contains(x, StringComparison.OrdinalIgnoreCase))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

static IReadOnlyList<string> InferBackgroundEquipment(string? preview)
{
    if (string.IsNullOrWhiteSpace(preview))
    {
        return Array.Empty<string>();
    }

    var known = new[] { "Backpack", "Bedroll", "Rope", "Rations", "Waterskin", "Torch", "Tinderbox" };
    return known
        .Where(x => preview.Contains(x, StringComparison.OrdinalIgnoreCase))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

static string InferFeatCategory(string? preview)
{
    if (string.IsNullOrWhiteSpace(preview))
    {
        return string.Empty;
    }

    if (preview.Contains("Origin Feat", StringComparison.OrdinalIgnoreCase))
    {
        return "Origin";
    }
    if (preview.Contains("Epic Boon", StringComparison.OrdinalIgnoreCase))
    {
        return "Epic Boon";
    }

    return string.Empty;
}

static int InferSpellLevel(string title, string? preview)
{
    var text = $"{title} {preview}";
    var match = Regex.Match(text, @"\b(cantrip|[1-9](?:st|nd|rd|th)?[- ]level)\b", RegexOptions.IgnoreCase);
    if (!match.Success)
    {
        return 0;
    }

    var token = match.Groups[1].Value.Trim().ToLowerInvariant();
    if (token == "cantrip")
    {
        return 0;
    }

    var digitMatch = Regex.Match(token, @"[1-9]");
    return digitMatch.Success && int.TryParse(digitMatch.Value, out var level) ? level : 0;
}

static string InferSpellSchool(string? preview)
{
    if (string.IsNullOrWhiteSpace(preview))
    {
        return string.Empty;
    }

    foreach (var school in new[] { "Abjuration", "Conjuration", "Divination", "Enchantment", "Evocation", "Illusion", "Necromancy", "Transmutation" })
    {
        if (preview.Contains(school, StringComparison.OrdinalIgnoreCase))
        {
            return school;
        }
    }

    return string.Empty;
}

static string InferCastingTime(string? preview)
{
    if (string.IsNullOrWhiteSpace(preview))
    {
        return string.Empty;
    }

    var match = Regex.Match(preview, @"Casting Time[:\s]+([^.;\n]+)", RegexOptions.IgnoreCase);
    return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
}

static string InferRangeText(string? preview)
{
    if (string.IsNullOrWhiteSpace(preview))
    {
        return string.Empty;
    }

    var match = Regex.Match(preview, @"Range[:\s]+([^.;\n]+)", RegexOptions.IgnoreCase);
    return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
}

static string InferDurationText(string? preview)
{
    if (string.IsNullOrWhiteSpace(preview))
    {
        return string.Empty;
    }

    var match = Regex.Match(preview, @"Duration[:\s]+([^.;\n]+)", RegexOptions.IgnoreCase);
    return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
}

static decimal InferSpellIngestionConfidence(string? preview, decimal? parseConfidence)
{
    var score = parseConfidence ?? 0.55m;
    if (string.IsNullOrWhiteSpace(preview))
    {
        return Math.Clamp(score - 0.15m, 0.05m, 0.99m);
    }

    if (Regex.IsMatch(preview, @"\bCasting Time[:\s]", RegexOptions.IgnoreCase))
    {
        score += 0.10m;
    }
    if (Regex.IsMatch(preview, @"\bRange[:\s]", RegexOptions.IgnoreCase))
    {
        score += 0.10m;
    }
    if (Regex.IsMatch(preview, @"\bDuration[:\s]", RegexOptions.IgnoreCase))
    {
        score += 0.10m;
    }
    if (Regex.IsMatch(preview, @"\b(Abjuration|Conjuration|Divination|Enchantment|Evocation|Illusion|Necromancy|Transmutation)\b", RegexOptions.IgnoreCase))
    {
        score += 0.05m;
    }

    return Math.Clamp(score, 0.05m, 0.99m);
}

static string InferClassHitDie(string displayName, string? preview)
{
    var text = $"{displayName} {preview}";
    var explicitMatch = Regex.Match(text, @"\bd\s*(6|8|10|12)\b", RegexOptions.IgnoreCase);
    if (explicitMatch.Success)
    {
        return $"d{explicitMatch.Groups[1].Value}";
    }

    return displayName.ToLowerInvariant() switch
    {
        var name when name.Contains("wizard") || name.Contains("sorcerer") => "d6",
        var name when name.Contains("artificer") || name.Contains("bard") || name.Contains("cleric") ||
                         name.Contains("druid") || name.Contains("monk") || name.Contains("rogue") ||
                         name.Contains("warlock") => "d8",
        var name when name.Contains("fighter") || name.Contains("paladin") || name.Contains("ranger") => "d10",
        var name when name.Contains("barbarian") => "d12",
        _ => "d8"
    };
}

static IReadOnlyList<string> InferClassPrimaryAbilities(string displayName)
{
    var lowered = displayName.ToLowerInvariant();
    return lowered switch
    {
        var name when name.Contains("barbarian") => new[] { "Strength" },
        var name when name.Contains("bard") => new[] { "Charisma" },
        var name when name.Contains("cleric") => new[] { "Wisdom" },
        var name when name.Contains("druid") => new[] { "Wisdom" },
        var name when name.Contains("fighter") => new[] { "Strength", "Dexterity" },
        var name when name.Contains("monk") => new[] { "Dexterity", "Wisdom" },
        var name when name.Contains("paladin") => new[] { "Strength", "Charisma" },
        var name when name.Contains("ranger") => new[] { "Dexterity", "Wisdom" },
        var name when name.Contains("rogue") => new[] { "Dexterity" },
        var name when name.Contains("sorcerer") => new[] { "Charisma" },
        var name when name.Contains("warlock") => new[] { "Charisma" },
        var name when name.Contains("wizard") => new[] { "Intelligence" },
        var name when name.Contains("artificer") => new[] { "Intelligence" },
        _ => Array.Empty<string>()
    };
}

static IReadOnlyList<string> InferClassSavingThrowAbilities(string displayName)
{
    var lowered = displayName.ToLowerInvariant();
    return lowered switch
    {
        var name when name.Contains("barbarian") => new[] { "Strength", "Constitution" },
        var name when name.Contains("bard") => new[] { "Dexterity", "Charisma" },
        var name when name.Contains("cleric") => new[] { "Wisdom", "Charisma" },
        var name when name.Contains("druid") => new[] { "Intelligence", "Wisdom" },
        var name when name.Contains("fighter") => new[] { "Strength", "Constitution" },
        var name when name.Contains("monk") => new[] { "Strength", "Dexterity" },
        var name when name.Contains("paladin") => new[] { "Wisdom", "Charisma" },
        var name when name.Contains("ranger") => new[] { "Strength", "Dexterity" },
        var name when name.Contains("rogue") => new[] { "Dexterity", "Intelligence" },
        var name when name.Contains("sorcerer") => new[] { "Constitution", "Charisma" },
        var name when name.Contains("warlock") => new[] { "Wisdom", "Charisma" },
        var name when name.Contains("wizard") => new[] { "Intelligence", "Wisdom" },
        var name when name.Contains("artificer") => new[] { "Constitution", "Intelligence" },
        _ => Array.Empty<string>()
    };
}

static string InferSubclassParentClassName(string displayName, string? preview)
{
    var text = $"{displayName} {preview}".ToLowerInvariant();
    var mapping = new (string Pattern, string ClassName)[]
    {
        ("wizard", "Wizard"),
        ("school of", "Wizard"),
        ("arcane tradition", "Wizard"),
        ("bard", "Bard"),
        ("college", "Bard"),
        ("cleric", "Cleric"),
        ("domain", "Cleric"),
        ("druid", "Druid"),
        ("circle", "Druid"),
        ("fighter", "Fighter"),
        ("martial archetype", "Fighter"),
        ("rogue", "Rogue"),
        ("roguish archetype", "Rogue"),
        ("sorcerer", "Sorcerer"),
        ("sorcerous origin", "Sorcerer"),
        ("warlock", "Warlock"),
        ("otherworldly patron", "Warlock"),
        ("monk", "Monk"),
        ("monastic tradition", "Monk"),
        ("way of", "Monk"),
        ("paladin", "Paladin"),
        ("oath", "Paladin"),
        ("ranger", "Ranger"),
        ("ranger archetype", "Ranger"),
        ("barbarian", "Barbarian"),
        ("path of", "Barbarian"),
        ("artificer", "Artificer"),
        ("alchemist", "Artificer"),
        ("armorer", "Artificer"),
        ("artillerist", "Artificer"),
        ("battle smith", "Artificer")
    };

    foreach (var (pattern, className) in mapping)
    {
        if (text.Contains(pattern, StringComparison.Ordinal))
        {
            return className;
        }
    }

    return string.Empty;
}

static int InferSubclassFeatureStartLevel(string? parentClassName, string subclassDisplayName, string ruleSystemId)
{
    if (string.Equals(ruleSystemId, "rules-2014", StringComparison.Ordinal) &&
        string.Equals(parentClassName, "Wizard", StringComparison.OrdinalIgnoreCase))
    {
        return 2;
    }
    if (subclassDisplayName.Contains("Wizard", StringComparison.OrdinalIgnoreCase))
    {
        return 2;
    }

    return 3;
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
        "2014wikidot" => "WIKIDOT2014",
        "2024wikidot" => "WIKIDOT2024",
        _ => throw new InvalidOperationException($"Unknown source code '{sourceCode}'.")
    };
}

static async Task<IReadOnlyList<IngestionPayload>> LoadIngestionPayloadsForImportAsync(string repoRoot)
{
    var payloads = new List<IngestionPayload>();
    var sectionsFiles = Directory
        .EnumerateFiles(Path.Combine(repoRoot, "data", "ingested"), "sections.json", SearchOption.AllDirectories)
        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
        .ToArray();
    foreach (var sectionsFile in sectionsFiles)
    {
        var json = await File.ReadAllTextAsync(sectionsFile);
        var payload = JsonSerializer.Deserialize<IngestionPayload>(json);
        if (payload is not null)
        {
            payloads.Add(payload);
        }
    }

    var wikidotSpecs = new[]
    {
        ("2014wikidot", Path.Combine(repoRoot, "data", "ingested", "2014wikidot", "dnd5ewikidot.json")),
        ("2024wikidot", Path.Combine(repoRoot, "data", "ingested", "2024wikidot", "dnd2024wikidot.json")),
    };

    foreach (var (sourceCode, path) in wikidotSpecs)
    {
        if (!File.Exists(path))
        {
            continue;
        }

        var json = await File.ReadAllTextAsync(path);
        var snapshot = JsonSerializer.Deserialize<WikidotSnapshot>(json);
        if (snapshot is null || snapshot.Pages.Count == 0)
        {
            continue;
        }

        payloads.Add(BuildWikidotPayload(sourceCode, Path.GetFileName(path), snapshot));
    }

    return payloads;
}

static IngestionPayload BuildWikidotPayload(string sourceCode, string sourceFile, WikidotSnapshot snapshot)
{
    var dedupe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var sections = new List<SectionRecord>();
    foreach (var page in snapshot.Pages)
    {
        var rawText = page.Text?.Trim() ?? string.Empty;
        if (rawText.Length == 0)
        {
            continue;
        }

        var dedupeKey = !string.IsNullOrWhiteSpace(page.ContentHash)
            ? page.ContentHash.Trim()
            : page.Url.Trim();
        if (!dedupe.Add(dedupeKey))
        {
            continue;
        }

        var normalizedText = rawText.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalizedText.Split('\n', StringSplitOptions.None);
        var title = BuildWikidotTitle(page.Url);
        var lineCount = Math.Max(1, lines.Length);
        var confidence = normalizedText.Length >= 200 ? 0.85m : 0.7m;
        var preview = normalizedText.Length > 220 ? normalizedText[..220] : normalizedText;
        sections.Add(new SectionRecord(
            SectionIndex: sections.Count + 1,
            Title: title,
            StartLine: 1,
            EndLine: lineCount,
            LineCount: lineCount,
            Confidence: confidence,
            Preview: preview));
    }

    return new IngestionPayload(
        SourceCode: sourceCode,
        SourceFile: sourceFile,
        GeneratedAtUtc: DateTimeOffset.UtcNow,
        SectionCount: sections.Count,
        Sections: sections);
}

static string BuildWikidotTitle(string url)
{
    if (string.IsNullOrWhiteSpace(url))
    {
        return "Wikidot page";
    }

    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
    {
        return url.Trim();
    }

    var segment = uri.AbsolutePath.Trim('/');
    if (string.IsNullOrWhiteSpace(segment))
    {
        return uri.Host;
    }

    var normalized = segment.Replace('_', ' ').Replace('-', ' ').Replace(':', ' ').Trim();
    return Regex.Replace(normalized, @"\s+", " ");
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

sealed record PendingSubclassRow(
    string ModuleId,
    string ContentSourceId,
    string RuleSystemId,
    string Slug,
    string DisplayName,
    string Description,
    string? Preview,
    string PayloadJson);

sealed record WikidotSnapshot(
    [property: JsonPropertyName("pages_fetched")] int PagesFetched,
    [property: JsonPropertyName("pages")] IReadOnlyList<WikidotPage> Pages);

sealed record WikidotPage(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("incoming_link_count")] int IncomingLinkCount,
    [property: JsonPropertyName("content_hash")] string ContentHash,
    [property: JsonPropertyName("text")] string Text);
