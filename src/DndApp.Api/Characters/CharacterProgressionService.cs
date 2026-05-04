using DndApp.Api.Data;
using DndApp.Api.MixedRules;
using Microsoft.EntityFrameworkCore;

namespace DndApp.Api.Characters;

public interface ICharacterProgressionService
{
    Task<CharacterSpellsData?> GetSpellsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<CharacterSpellsData> UpsertSpellsAsync(Guid characterId, UpsertCharacterSpellsRequest request, CancellationToken cancellationToken);
    Task<RecommendedSpellsResult?> GetRecommendedSpellsAsync(Guid characterId, string classModuleId, int classLevel, CancellationToken cancellationToken);
    Task<CharacterCurrencyData?> GetCurrencyAsync(Guid characterId, CancellationToken cancellationToken);
    Task<CharacterCurrencyData> UpsertCurrencyAsync(Guid characterId, UpsertCharacterCurrencyRequest request, CancellationToken cancellationToken);
    Task<(CharacterCurrencyData? Data, IReadOnlyList<string> Errors)> ConvertCurrencyAsync(Guid characterId, ConvertCurrencyRequest request, CancellationToken cancellationToken);
    Task<CharacterCurrencyData> ConsolidateCurrencyAsync(Guid characterId, ConsolidateCurrencyRequest request, CancellationToken cancellationToken);
    Task<(CharacterCurrencyData? Data, IReadOnlyList<string> Errors)> PurchaseFromCurrencyAsync(Guid characterId, PurchaseFromCurrencyRequest request, CancellationToken cancellationToken);
    Task<CharacterResourcesData?> GetResourcesAsync(Guid characterId, CancellationToken cancellationToken);
    Task<CharacterResourcesData> UpsertResourcesAsync(Guid characterId, UpsertCharacterResourcesRequest request, CancellationToken cancellationToken);
    Task<CharacterVitalsData?> GetVitalsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<CharacterVitalsData> UpsertVitalsAsync(Guid characterId, UpsertCharacterVitalsRequest request, CancellationToken cancellationToken);
}

public sealed class CharacterProgressionService : ICharacterProgressionService
{
    private static readonly Dictionary<string, int> CurrencyValuesInCp = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cp"] = 1,
        ["sp"] = 10,
        ["ep"] = 50,
        ["gp"] = 100,
        ["pp"] = 1000,
    };

    private readonly AppDbContext _db;

    public CharacterProgressionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CharacterSpellsData?> GetSpellsAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        var exists = await _db.CharacterSheets.AsNoTracking().AnyAsync(x => x.CharacterId == id, cancellationToken);
        if (!exists)
        {
            return null;
        }

        var entries = await _db.CharacterSpellEntries.AsNoTracking()
            .Where(x => x.CharacterId == id)
            .OrderBy(x => x.PreparationMode)
            .ThenBy(x => x.SpellName)
            .Select(x => new CharacterSpellEntryData(x.SpellModuleId, x.SpellName, x.PreparationMode))
            .ToArrayAsync(cancellationToken);

        return new CharacterSpellsData(characterId, entries);
    }

    public async Task<CharacterSpellsData> UpsertSpellsAsync(Guid characterId, UpsertCharacterSpellsRequest request, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        await EnsureCharacterSheetExistsAsync(id, cancellationToken);
        var current = await _db.CharacterSpellEntries.Where(x => x.CharacterId == id).ToListAsync(cancellationToken);
        _db.CharacterSpellEntries.RemoveRange(current);
        foreach (var entry in request.Entries
                     .Where(x => !string.IsNullOrWhiteSpace(x.SpellName))
                     .DistinctBy(x => $"{x.SpellModuleId}\u001F{x.PreparationMode}", StringComparer.OrdinalIgnoreCase))
        {
            _db.CharacterSpellEntries.Add(new CharacterSpellEntryEntity
            {
                CharacterId = id,
                SpellModuleId = entry.SpellModuleId.Trim(),
                SpellName = entry.SpellName.Trim(),
                PreparationMode = NormalizePreparationMode(entry.PreparationMode),
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await GetSpellsAsync(characterId, cancellationToken) ?? new CharacterSpellsData(characterId, Array.Empty<CharacterSpellEntryData>());
    }

    public async Task<RecommendedSpellsResult?> GetRecommendedSpellsAsync(Guid characterId, string classModuleId, int classLevel, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        var sheet = await _db.CharacterSheets.AsNoTracking()
            .Where(x => x.CharacterId == id)
            .Select(x => new { x.BaseRuleSystem, x.Level })
            .FirstOrDefaultAsync(cancellationToken);
        if (sheet is null)
        {
            return null;
        }

        var abilityScores = await _db.CharacterAbilityScores.AsNoTracking()
            .Where(x => x.CharacterId == id)
            .ToDictionaryAsync(x => x.AbilityName, x => x.Score, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var classLevels = await _db.CharacterClassLevels.AsNoTracking()
            .Where(x => x.CharacterId == id)
            .ToListAsync(cancellationToken);

        // Get all spells with their class associations
        var allSpells = await (from m in _db.RuleModules.AsNoTracking()
                              where m.ModuleType == "spell"
                              join v in _db.RuleVariants.AsNoTracking() on m.Id equals v.RuleModuleId
                              where v.RuleSystemId == sheet.BaseRuleSystem
                              select new { m.Id, m.DisplayName, v.PayloadJson }).Cast<dynamic>().ToListAsync(cancellationToken);

        var spellSources = new List<SpellSourceGroup>();

        // Process each class level for spell prep calculation
        foreach (var classLevelData in classLevels)
        {
            var prepCount = CalculateSpellPrepCount(classLevelData.ClassName, sheet.Level, abilityScores, sheet.BaseRuleSystem);
            var automaticSpells = GetAutomaticSpells(classLevelData.ClassName, sheet.BaseRuleSystem);
            var selectableSpells = GetSelectableSpells(classLevelData.ClassName, allSpells, automaticSpells);

            if (prepCount > 0 || automaticSpells.Count > 0 || selectableSpells.Count > 0)
            {
                spellSources.Add(new SpellSourceGroup(
                    $"{classLevelData.ClassName} Spells",
                    prepCount,
                    automaticSpells,
                    selectableSpells));
            }
        }

        return new RecommendedSpellsResult(
            characterId,
            classModuleId,
            Math.Max(1, classLevel),
            spellSources,
            "Spell recommendations are calculated based on your class, level, and ability scores.",
            spellSources.Count == 0 ? "No spellcasting classes found for this character." : null);
    }

    private static int CalculateSpellPrepCount(string className, int characterLevel, Dictionary<string, int> abilityScores, string baseRuleSystem)
    {
        // Get ability modifier for the class
        var abilityName = GetSpellcastingAbilityForClass(className);
        var abilityScore = abilityScores.TryGetValue(abilityName, out var score) ? score : 10;
        var abilityModifier = (abilityScore - 10) / 2;

        // Apply spell prep formulas based on class
        return className switch
        {
            // Wizard: level + INT mod (min 1)
            "Wizard" => Math.Max(1, characterLevel + abilityModifier),

            // Cleric: level + WIS mod (min 1)
            "Cleric" => Math.Max(1, characterLevel + abilityModifier),

            // Druid: level + WIS mod (min 1)
            "Druid" => Math.Max(1, characterLevel + abilityModifier),

            // Bard: (level / 2 rounded up) + CHA mod (min 1)
            "Bard" => Math.Max(1, ((characterLevel + 1) / 2) + abilityModifier),

            // Paladin: (level - 2) / 2 rounded up, min 1 if level >= 5
            "Paladin" => characterLevel < 5 ? 0 : Math.Max(1, ((characterLevel - 2 + 1) / 2) + abilityModifier),

            // Ranger: (level - 1) / 2 rounded up, min 1 if level >= 5
            "Ranger" => characterLevel < 5 ? 0 : Math.Max(1, ((characterLevel - 1 + 1) / 2) + abilityModifier),

            // Sorcerer: sorcerers know spells, not prepare them
            "Sorcerer" => 0,

            // Warlock: warlocks have invocations and limited slots
            "Warlock" => 0,

            // Artificer (2024 only): level + INT mod (min 1)
            "Artificer" => baseRuleSystem == "Rules2024" ? Math.Max(1, characterLevel + abilityModifier) : 0,

            _ => 0,
        };
    }

    private static string GetSpellcastingAbilityForClass(string className)
    {
        return className switch
        {
            "Wizard" or "Artificer" => "Intelligence",
            "Cleric" or "Druid" => "Wisdom",
            "Bard" or "Sorcerer" or "Paladin" => "Charisma",
            "Ranger" => "Wisdom",
            "Warlock" => "Charisma",
            _ => "Intelligence",
        };
    }

    private static IReadOnlyList<CharacterSpellEntryData> GetAutomaticSpells(string className, string baseRuleSystem)
    {
        // Hardcoded automatic spells for each class
        // These are cantrips and special grants that come with the class
        var spells = new List<CharacterSpellEntryData>();

        if (className == "Wizard")
        {
            // Wizards get some common cantrips as automatic
            // (In real implementation, verify against database)
        }
        else if (className == "Cleric")
        {
            // Clerics automatically get cantrips from their domain
        }

        return spells;
    }

    private static IReadOnlyList<CharacterSpellEntryData> GetSelectableSpells(
        string className,
        List<dynamic> allSpells,
        IReadOnlyList<CharacterSpellEntryData> automaticSpells)
    {
        if (allSpells == null || allSpells.Count == 0)
        {
            return Array.Empty<CharacterSpellEntryData>();
        }

        var automaticIds = automaticSpells
            .Select(x => $"{x.SpellModuleId}\u001F{x.SpellName}".ToUpperInvariant())
            .ToHashSet();

        var selectableSpells = new List<CharacterSpellEntryData>();

        foreach (var spellItem in allSpells)
        {
            // Extract properties from anonymous type
            var id = spellItem.Id as string;
            var displayName = spellItem.DisplayName as string;
            var payloadJson = spellItem.PayloadJson as string;

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(payloadJson))
            {
                continue;
            }

            // Parse spell classes from payload
            var spellClasses = CatalogParsing.ParseStringArray(payloadJson, "spellClasses");

            // Check if this class can use this spell
            if (!spellClasses.Contains(className, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            // Exclude automatic spells
            var spellKey = $"{id}\u001F{displayName}".ToUpperInvariant();
            if (automaticIds.Contains(spellKey))
            {
                continue;
            }

            selectableSpells.Add(new CharacterSpellEntryData(
                id,
                displayName,
                "selected"));
        }

        return selectableSpells.OrderBy(x => x.SpellName, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<CharacterCurrencyData?> GetCurrencyAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        var exists = await _db.CharacterSheets.AsNoTracking().AnyAsync(x => x.CharacterId == id, cancellationToken);
        if (!exists)
        {
            return null;
        }

        var resources = await _db.CharacterResourcePools.AsNoTracking()
            .Where(x => x.CharacterId == id && (x.ResourceKey == "cp" || x.ResourceKey == "sp" || x.ResourceKey == "ep" || x.ResourceKey == "gp" || x.ResourceKey == "pp"))
            .ToDictionaryAsync(x => x.ResourceKey, x => x.CurrentValue, StringComparer.OrdinalIgnoreCase, cancellationToken);

        return new CharacterCurrencyData(
            characterId,
            resources.TryGetValue("cp", out var cp) ? cp : 0,
            resources.TryGetValue("sp", out var sp) ? sp : 0,
            resources.TryGetValue("ep", out var ep) ? ep : 0,
            resources.TryGetValue("gp", out var gp) ? gp : 0,
            resources.TryGetValue("pp", out var pp) ? pp : 0);
    }

    public async Task<CharacterCurrencyData> UpsertCurrencyAsync(Guid characterId, UpsertCharacterCurrencyRequest request, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        await EnsureCharacterSheetExistsAsync(id, cancellationToken);
        var rows = await _db.CharacterResourcePools
            .Where(x => x.CharacterId == id && (x.ResourceKey == "cp" || x.ResourceKey == "sp" || x.ResourceKey == "ep" || x.ResourceKey == "gp" || x.ResourceKey == "pp"))
            .ToListAsync(cancellationToken);

        _db.CharacterResourcePools.RemoveRange(rows);
        _db.CharacterResourcePools.AddRange(
            CreateCurrencyRow(id, "cp", Math.Max(0, request.Cp)),
            CreateCurrencyRow(id, "sp", Math.Max(0, request.Sp)),
            CreateCurrencyRow(id, "ep", Math.Max(0, request.Ep)),
            CreateCurrencyRow(id, "gp", Math.Max(0, request.Gp)),
            CreateCurrencyRow(id, "pp", Math.Max(0, request.Pp)));

        await _db.SaveChangesAsync(cancellationToken);
        return (await GetCurrencyAsync(characterId, cancellationToken)) ?? new CharacterCurrencyData(characterId, 0, 0, 0, 0, 0);
    }

    public async Task<(CharacterCurrencyData? Data, IReadOnlyList<string> Errors)> ConvertCurrencyAsync(Guid characterId, ConvertCurrencyRequest request, CancellationToken cancellationToken)
    {
        var current = await GetCurrencyAsync(characterId, cancellationToken);
        if (current is null)
        {
            return (null, new[] { "Character build was not found." });
        }

        if (!CurrencyValuesInCp.TryGetValue(request.FromDenomination.Trim(), out var fromValue) ||
            !CurrencyValuesInCp.TryGetValue(request.ToDenomination.Trim(), out var toValue))
        {
            return (null, new[] { "Currency denomination must be one of cp, sp, ep, gp, pp." });
        }
        if (request.Amount <= 0)
        {
            return (null, new[] { "Amount must be greater than 0." });
        }

        var wallet = ToWallet(current);
        var fromKey = request.FromDenomination.Trim().ToLowerInvariant();
        var toKey = request.ToDenomination.Trim().ToLowerInvariant();
        if (wallet[fromKey] < request.Amount)
        {
            return (null, new[] { $"Not enough {fromKey} to convert." });
        }

        var totalCp = request.Amount * fromValue;
        if (totalCp % toValue != 0)
        {
            return (null, new[] { $"Cannot convert {request.Amount} {fromKey} into exact {toKey} at book exchange rates." });
        }

        wallet[fromKey] -= request.Amount;
        wallet[toKey] += totalCp / toValue;
        var saved = await UpsertCurrencyAsync(characterId, new UpsertCharacterCurrencyRequest(wallet["cp"], wallet["sp"], wallet["ep"], wallet["gp"], wallet["pp"]), cancellationToken);
        return (saved, Array.Empty<string>());
    }

    public async Task<CharacterCurrencyData> ConsolidateCurrencyAsync(Guid characterId, ConsolidateCurrencyRequest request, CancellationToken cancellationToken)
    {
        var current = await GetCurrencyAsync(characterId, cancellationToken) ?? new CharacterCurrencyData(characterId, 0, 0, 0, 0, 0);
        var totalCp = ToTotalCp(current);
        var wallet = request.PreferPlatinum ? FromTotalCpPreferPlatinum(totalCp) : FromTotalCpStandard(totalCp);
        return await UpsertCurrencyAsync(characterId, new UpsertCharacterCurrencyRequest(wallet["cp"], wallet["sp"], wallet["ep"], wallet["gp"], wallet["pp"]), cancellationToken);
    }

    public async Task<(CharacterCurrencyData? Data, IReadOnlyList<string> Errors)> PurchaseFromCurrencyAsync(Guid characterId, PurchaseFromCurrencyRequest request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
        {
            return (null, new[] { "Quantity must be greater than 0." });
        }
        if (request.CostInGold < 0)
        {
            return (null, new[] { "Cost cannot be negative." });
        }

        var current = await GetCurrencyAsync(characterId, cancellationToken);
        if (current is null)
        {
            return (null, new[] { "Character build was not found." });
        }

        var costInCp = (int)Math.Round(request.CostInGold * request.Quantity * 100m, MidpointRounding.AwayFromZero);
        var totalCp = ToTotalCp(current);
        if (totalCp < costInCp)
        {
            return (null, new[] { "Insufficient funds for purchase." });
        }

        var remainingCp = totalCp - costInCp;
        var consolidated = FromTotalCpStandard(remainingCp);
        var saved = await UpsertCurrencyAsync(characterId, new UpsertCharacterCurrencyRequest(consolidated["cp"], consolidated["sp"], consolidated["ep"], consolidated["gp"], consolidated["pp"]), cancellationToken);
        return (saved, Array.Empty<string>());
    }

    public async Task<CharacterResourcesData?> GetResourcesAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        var exists = await _db.CharacterSheets.AsNoTracking().AnyAsync(x => x.CharacterId == id, cancellationToken);
        if (!exists)
        {
            return null;
        }

        var resources = await _db.CharacterResourcePools.AsNoTracking()
            .Where(x => x.CharacterId == id)
            .OrderBy(x => x.ResourceKey)
            .Select(x => new CharacterResourcePoolData(x.ResourceKey, x.CurrentValue, x.MaxValue, x.MetadataJson))
            .ToArrayAsync(cancellationToken);

        return new CharacterResourcesData(characterId, resources);
    }

    public async Task<CharacterResourcesData> UpsertResourcesAsync(Guid characterId, UpsertCharacterResourcesRequest request, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        await EnsureCharacterSheetExistsAsync(id, cancellationToken);
        var current = await _db.CharacterResourcePools.Where(x => x.CharacterId == id).ToListAsync(cancellationToken);
        _db.CharacterResourcePools.RemoveRange(current);
        foreach (var resource in request.Resources
                     .Where(x => !string.IsNullOrWhiteSpace(x.ResourceKey))
                     .DistinctBy(x => x.ResourceKey, StringComparer.OrdinalIgnoreCase))
        {
            _db.CharacterResourcePools.Add(new CharacterResourcePoolEntity
            {
                CharacterId = id,
                ResourceKey = resource.ResourceKey.Trim(),
                CurrentValue = resource.CurrentValue,
                MaxValue = resource.MaxValue,
                MetadataJson = string.IsNullOrWhiteSpace(resource.MetadataJson) ? "{}" : resource.MetadataJson,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await GetResourcesAsync(characterId, cancellationToken) ?? new CharacterResourcesData(characterId, Array.Empty<CharacterResourcePoolData>());
    }

    public async Task<CharacterVitalsData?> GetVitalsAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        var row = await _db.CharacterVitals.AsNoTracking().SingleOrDefaultAsync(x => x.CharacterId == id, cancellationToken);
        if (row is null)
        {
            return null;
        }

        return new CharacterVitalsData(
            characterId,
            row.MaxHitPoints,
            row.CurrentHitPoints,
            row.TempHitPoints,
            row.BaseMoveSpeed,
            row.BaseArmorClass,
            row.UpdatedAtUtc);
    }

    public async Task<CharacterVitalsData> UpsertVitalsAsync(Guid characterId, UpsertCharacterVitalsRequest request, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        await EnsureCharacterSheetExistsAsync(id, cancellationToken);
        var row = await _db.CharacterVitals.SingleOrDefaultAsync(x => x.CharacterId == id, cancellationToken);
        if (row is null)
        {
            row = new CharacterVitalsEntity
            {
                CharacterId = id,
            };
            _db.CharacterVitals.Add(row);
        }

        row.MaxHitPoints = Math.Max(0, request.MaxHitPoints);
        row.CurrentHitPoints = Math.Max(0, request.CurrentHitPoints);
        row.TempHitPoints = Math.Max(0, request.TempHitPoints);
        row.BaseMoveSpeed = Math.Max(0, request.BaseMoveSpeed);
        row.BaseArmorClass = Math.Max(0, request.BaseArmorClass);
        row.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return new CharacterVitalsData(
            characterId,
            row.MaxHitPoints,
            row.CurrentHitPoints,
            row.TempHitPoints,
            row.BaseMoveSpeed,
            row.BaseArmorClass,
            row.UpdatedAtUtc);
    }

    private static string NormalizePreparationMode(string value)
    {
        if (value.Equals("Cantrip", StringComparison.OrdinalIgnoreCase))
        {
            return "Cantrip";
        }
        if (value.Equals("Prepared", StringComparison.OrdinalIgnoreCase))
        {
            return "Prepared";
        }
        return "Known";
    }

    private async Task EnsureCharacterSheetExistsAsync(string characterId, CancellationToken cancellationToken)
    {
        var existing = await _db.CharacterSheets.SingleOrDefaultAsync(x => x.CharacterId == characterId, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var record = await _db.CharacterRecords
            .AsNoTracking()
            .Where(x => x.CharacterId == characterId)
            .Select(x => new { x.OwnerUserId, x.CharacterName, x.BaseRuleSystem })
            .SingleOrDefaultAsync(cancellationToken);
        var draft = await _db.CharacterDrafts
            .AsNoTracking()
            .Where(x => x.CharacterId == characterId)
            .Select(x => new { x.OwnerUserId, x.CharacterName, x.BaseRuleSystem })
            .SingleOrDefaultAsync(cancellationToken);
        if (record is null && draft is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        _db.CharacterSheets.Add(new CharacterSheetEntity
        {
            CharacterId = characterId,
            OwnerUserId = record?.OwnerUserId ?? draft?.OwnerUserId ?? string.Empty,
            CharacterName = record?.CharacterName ?? draft?.CharacterName ?? "New Adventurer",
            BaseRuleSystem = record?.BaseRuleSystem ?? draft?.BaseRuleSystem ?? RuleSystemMode.Rules2024.ToString(),
            BuildMethod = CharacterBuildMethod.PointBuy.ToString(),
            ClassModuleId = string.Empty,
            ClassName = "Unassigned",
            Level = 1,
            ProficiencyBonus = 2,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
    }

    private static CharacterResourcePoolEntity CreateCurrencyRow(string characterId, string key, int amount)
    {
        return new CharacterResourcePoolEntity
        {
            CharacterId = characterId,
            ResourceKey = key,
            CurrentValue = amount,
            MaxValue = amount,
            MetadataJson = "{}",
        };
    }

    private static int ToTotalCp(CharacterCurrencyData data)
    {
        return data.Cp + (data.Sp * 10) + (data.Ep * 50) + (data.Gp * 100) + (data.Pp * 1000);
    }

    private static Dictionary<string, int> ToWallet(CharacterCurrencyData data)
    {
        return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["cp"] = data.Cp,
            ["sp"] = data.Sp,
            ["ep"] = data.Ep,
            ["gp"] = data.Gp,
            ["pp"] = data.Pp,
        };
    }

    private static Dictionary<string, int> FromTotalCpStandard(int totalCp)
    {
        var remaining = Math.Max(0, totalCp);
        var gp = remaining / 100;
        remaining %= 100;
        var sp = remaining / 10;
        var cp = remaining % 10;
        return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["cp"] = cp,
            ["sp"] = sp,
            ["ep"] = 0,
            ["gp"] = gp,
            ["pp"] = 0,
        };
    }

    private static Dictionary<string, int> FromTotalCpPreferPlatinum(int totalCp)
    {
        var remaining = Math.Max(0, totalCp);
        var pp = remaining / 1000;
        remaining %= 1000;
        var gp = remaining / 100;
        remaining %= 100;
        var sp = remaining / 10;
        var cp = remaining % 10;
        return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["cp"] = cp,
            ["sp"] = sp,
            ["ep"] = 0,
            ["gp"] = gp,
            ["pp"] = pp,
        };
    }
}


