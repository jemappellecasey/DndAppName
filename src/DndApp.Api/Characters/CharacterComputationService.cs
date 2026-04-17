using System.Text.Json;
using DndApp.Api.Data;
using DndApp.Api.Items;
using DndApp.Api.Mechanics;
using Microsoft.EntityFrameworkCore;

namespace DndApp.Api.Characters;

public interface ICharacterComputationService
{
    Task<(CheckResult? Result, IReadOnlyList<string> Errors)> ComputeCheckAsync(Guid characterId, PersistedComputeCheckRequest request, CancellationToken cancellationToken);
    Task<(SaveResult? Result, IReadOnlyList<string> Errors)> ComputeSaveAsync(Guid characterId, PersistedComputeSaveRequest request, CancellationToken cancellationToken);
    Task<(AttackResult? Result, IReadOnlyList<string> Errors)> ComputeAttackAsync(Guid characterId, PersistedComputeAttackRequest request, CancellationToken cancellationToken);
    Task<(CharacterDerivedStatsResponse? Result, IReadOnlyList<string> Errors)> GetDerivedStatsAsync(Guid characterId, CancellationToken cancellationToken);
}

public sealed class CharacterComputationService : ICharacterComputationService
{
    private static readonly string[] Skills =
    {
        "Acrobatics",
        "Animal Handling",
        "Arcana",
        "Athletics",
        "Deception",
        "History",
        "Insight",
        "Intimidation",
        "Investigation",
        "Medicine",
        "Nature",
        "Perception",
        "Performance",
        "Persuasion",
        "Religion",
        "Sleight of Hand",
        "Stealth",
        "Survival",
    };

    private static readonly Dictionary<string, string> SkillAbilityMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Acrobatics"] = "Dexterity",
        ["Animal Handling"] = "Wisdom",
        ["Arcana"] = "Intelligence",
        ["Athletics"] = "Strength",
        ["Deception"] = "Charisma",
        ["History"] = "Intelligence",
        ["Insight"] = "Wisdom",
        ["Intimidation"] = "Charisma",
        ["Investigation"] = "Intelligence",
        ["Medicine"] = "Wisdom",
        ["Nature"] = "Intelligence",
        ["Perception"] = "Wisdom",
        ["Performance"] = "Charisma",
        ["Persuasion"] = "Charisma",
        ["Religion"] = "Intelligence",
        ["Sleight of Hand"] = "Dexterity",
        ["Stealth"] = "Dexterity",
        ["Survival"] = "Wisdom",
    };

    private readonly AppDbContext _db;
    private readonly IItemEffectPipelineService _pipeline;
    private readonly ICalculationEngineService _calculation;

    public CharacterComputationService(
        AppDbContext db,
        IItemEffectPipelineService pipeline,
        ICalculationEngineService calculation)
    {
        _db = db;
        _pipeline = pipeline;
        _calculation = calculation;
    }

    public async Task<(CheckResult? Result, IReadOnlyList<string> Errors)> ComputeCheckAsync(
        Guid characterId,
        PersistedComputeCheckRequest request,
        CancellationToken cancellationToken)
    {
        if (!SkillAbilityMap.TryGetValue(request.SkillName, out var abilityName))
        {
            return (null, new[] { $"Unknown skill '{request.SkillName}'." });
        }

        var loaded = await LoadStateAsync(characterId, cancellationToken);
        if (loaded.Errors.Count > 0)
        {
            return (null, loaded.Errors);
        }

        var abilityModifier = Modifier(loaded.AbilityScores[abilityName]);
        var isProficient = loaded.ProficientSkills.Contains(request.SkillName, StringComparer.OrdinalIgnoreCase);
        var baseBeforeItems = abilityModifier + (isProficient ? loaded.ProficiencyBonus : 0);
        var derivedValue = loaded.Derived.AbilityChecks.TryGetValue(request.SkillName, out var fromItems) ? fromItems : baseBeforeItems;
        var inventoryModifier = derivedValue - baseBeforeItems;

        var result = _calculation.ComputeCheck(new ComputeCheckRequest(
            SkillName: request.SkillName,
            AbilityModifier: abilityModifier,
            ProficiencyBonus: loaded.ProficiencyBonus,
            IsProficient: isProficient,
            HasExpertise: request.HasExpertise && isProficient,
            AdditionalModifier: request.AdditionalModifier + inventoryModifier,
            AdvantageState: request.AdvantageState,
            RollDice: request.RollDice));

        return (result, Array.Empty<string>());
    }

    public async Task<(SaveResult? Result, IReadOnlyList<string> Errors)> ComputeSaveAsync(
        Guid characterId,
        PersistedComputeSaveRequest request,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadStateAsync(characterId, cancellationToken);
        if (loaded.Errors.Count > 0)
        {
            return (null, loaded.Errors);
        }

        if (!loaded.AbilityScores.TryGetValue(request.AbilityName, out var score))
        {
            return (null, new[] { $"Unknown ability '{request.AbilityName}'." });
        }

        var abilityModifier = Modifier(score);
        var baseBeforeItems = abilityModifier + (request.IsProficient ? loaded.ProficiencyBonus : 0);
        var derivedValue = loaded.Derived.SavingThrows.TryGetValue(request.AbilityName, out var fromItems) ? fromItems : abilityModifier;
        var inventoryModifier = derivedValue - abilityModifier;
        var extra = request.AdditionalModifier + inventoryModifier + (request.IsProficient ? 0 : 0);

        var result = _calculation.ComputeSave(new ComputeSaveRequest(
            AbilityName: request.AbilityName,
            AbilityModifier: abilityModifier,
            ProficiencyBonus: loaded.ProficiencyBonus,
            IsProficient: request.IsProficient,
            AdditionalModifier: extra,
            AdvantageState: request.AdvantageState,
            RollDice: request.RollDice));

        return (result, Array.Empty<string>());
    }

    public async Task<(AttackResult? Result, IReadOnlyList<string> Errors)> ComputeAttackAsync(
        Guid characterId,
        PersistedComputeAttackRequest request,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadStateAsync(characterId, cancellationToken);
        if (loaded.Errors.Count > 0)
        {
            return (null, loaded.Errors);
        }

        if (!loaded.AbilityScores.TryGetValue(request.AbilityName, out var score))
        {
            return (null, new[] { $"Unknown ability '{request.AbilityName}'." });
        }

        var abilityModifier = Modifier(score);
        var result = _calculation.ComputeAttack(new ComputeAttackRequest(
            WeaponName: request.WeaponName,
            AbilityModifier: abilityModifier,
            ProficiencyBonus: loaded.ProficiencyBonus,
            IsProficientWithWeapon: request.IsProficientWithWeapon,
            AdditionalAttackModifier: request.AdditionalAttackModifier,
            DamageDice: request.DamageDice,
            AdditionalDamageModifier: request.AdditionalDamageModifier,
            AdvantageState: request.AdvantageState,
            RollDice: request.RollDice));

        return (result, Array.Empty<string>());
    }

    public async Task<(CharacterDerivedStatsResponse? Result, IReadOnlyList<string> Errors)> GetDerivedStatsAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadStateAsync(characterId, cancellationToken);
        if (loaded.Errors.Count > 0)
        {
            return (null, loaded.Errors);
        }

        return (new CharacterDerivedStatsResponse(
            characterId,
            loaded.Derived.ArmorClass,
            loaded.Derived.MoveSpeed,
            loaded.Derived.SavingThrows,
            loaded.Derived.AbilityChecks,
            loaded.Derived.AvailableSpells,
            loaded.ActiveInventoryItemIds), Array.Empty<string>());
    }

    private async Task<LoadStateResult> LoadStateAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        var sheet = await _db.CharacterSheets.AsNoTracking().SingleOrDefaultAsync(x => x.CharacterId == id, cancellationToken);
        if (sheet is null)
        {
            return LoadStateResult.FromError("Character build was not found.");
        }

        var abilityScores = await _db.CharacterAbilityScores.AsNoTracking()
            .Where(x => x.CharacterId == id)
            .ToDictionaryAsync(x => x.AbilityName, x => x.Score, cancellationToken);
        if (abilityScores.Count == 0)
        {
            return LoadStateResult.FromError("Character ability scores are missing.");
        }

        var proficientSkills = await _db.CharacterSkillProficiencies.AsNoTracking()
            .Where(x => x.CharacterId == id)
            .Select(x => x.SkillName)
            .ToArrayAsync(cancellationToken);

        var baseStats = BuildBaseStats(abilityScores, proficientSkills, sheet.ProficiencyBonus);
        var inventoryRows = await _db.CharacterInventoryItems.AsNoTracking()
            .Where(x => x.CharacterId == id)
            .ToListAsync(cancellationToken);
        var inventoryDefinitionIds = inventoryRows.Select(x => x.ItemDefinitionId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var definitions = await _db.ItemDefinitions.AsNoTracking()
            .Where(x => inventoryDefinitionIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var effects = await _db.ItemEffects.AsNoTracking()
            .Where(x => inventoryDefinitionIds.Contains(x.ItemDefinitionId))
            .ToListAsync(cancellationToken);
        var effectsByDefinition = effects
            .GroupBy(x => x.ItemDefinitionId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.OrdinalIgnoreCase);

        var itemStates = inventoryRows.Select(row =>
        {
            if (!definitions.TryGetValue(row.ItemDefinitionId, out var definition))
            {
                return null;
            }
            var itemEffects = effectsByDefinition.TryGetValue(row.ItemDefinitionId, out var foundEffects)
                ? foundEffects.Select(MapEffect).ToArray()
                : Array.Empty<ItemEffect>();
            return new CharacterItemState(
                row.InventoryItemId,
                row.ItemName,
                definition.RequiresAttunement,
                row.IsEquipped,
                row.IsAttuned,
                itemEffects);
        }).Where(x => x is not null).Select(x => x!).ToArray();

        var pipelineResult = _pipeline.ApplyEffects(new ApplyItemEffectsRequest(baseStats, itemStates));
        return new LoadStateResult(
            ProficiencyBonus: sheet.ProficiencyBonus,
            AbilityScores: abilityScores,
            ProficientSkills: proficientSkills,
            Derived: pipelineResult.DerivedStats,
            ActiveInventoryItemIds: itemStates.Where(x => x.IsEquipped).Select(x => x.ItemId).ToArray(),
            Errors: Array.Empty<string>());
    }

    private static BaseStats BuildBaseStats(
        IReadOnlyDictionary<string, int> abilityScores,
        IReadOnlyList<string> proficientSkills,
        int proficiencyBonus)
    {
        var abilityModifiers = abilityScores.ToDictionary(x => x.Key, x => Modifier(x.Value), StringComparer.OrdinalIgnoreCase);
        var abilityChecks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in Skills)
        {
            if (!SkillAbilityMap.TryGetValue(skill, out var abilityName))
            {
                continue;
            }
            var baseValue = abilityModifiers.TryGetValue(abilityName, out var mod) ? mod : 0;
            if (proficientSkills.Contains(skill, StringComparer.OrdinalIgnoreCase))
            {
                baseValue += proficiencyBonus;
            }
            abilityChecks[skill] = baseValue;
        }

        var armorClass = 10 + (abilityModifiers.TryGetValue("Dexterity", out var dex) ? dex : 0);
        return new BaseStats(
            ArmorClass: armorClass,
            MoveSpeed: 30,
            SavingThrows: abilityModifiers,
            AbilityChecks: abilityChecks,
            AvailableSpells: Array.Empty<string>());
    }

    private static int Modifier(int score) => (int)Math.Floor((score - 10) / 2.0);

    private static ItemEffect MapEffect(ItemEffectEntity effect)
    {
        var payload = ParseObject(effect.EffectPayloadJson);
        var numericValue = ReadInt(payload, "numericValue")
            ?? ReadInt(payload, "value")
            ?? ReadInt(payload, "bonus")
            ?? ReadInt(payload, "modifier")
            ?? 0;
        var target = ReadString(payload, "target")
            ?? ReadString(payload, "ability")
            ?? ReadString(payload, "skill");
        var grantedSpell = ReadString(payload, "grantedSpell")
            ?? ReadString(payload, "spellName");
        var description = ReadString(payload, "description") ?? effect.EffectType;
        return new ItemEffect(
            MapEffectType(effect.EffectType),
            target,
            numericValue,
            grantedSpell,
            description);
    }

    private static ItemEffectType MapEffectType(string effectType)
    {
        var normalized = effectType.Trim().ToLowerInvariant();
        if (normalized.Contains("ac"))
        {
            return ItemEffectType.AcBonus;
        }
        if (normalized.Contains("move") || normalized.Contains("speed"))
        {
            return ItemEffectType.MoveSpeedBonus;
        }
        if (normalized.Contains("saving"))
        {
            return ItemEffectType.SavingThrowBonus;
        }
        if (normalized.Contains("spell"))
        {
            return ItemEffectType.GrantSpell;
        }
        return ItemEffectType.AbilityCheckBonus;
    }

    private static Dictionary<string, JsonElement>? ParseObject(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }
            return doc.RootElement.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.Clone(), StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return null;
        }
    }

    private static int? ReadInt(Dictionary<string, JsonElement>? payload, string key)
    {
        if (payload is null || !payload.TryGetValue(key, out var value))
        {
            return null;
        }
        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var n) => n,
            JsonValueKind.String when int.TryParse(value.GetString(), out var n) => n,
            _ => null
        };
    }

    private static string? ReadString(Dictionary<string, JsonElement>? payload, string key)
    {
        if (payload is null || !payload.TryGetValue(key, out var value))
        {
            return null;
        }
        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private sealed record LoadStateResult(
        int ProficiencyBonus,
        IReadOnlyDictionary<string, int> AbilityScores,
        IReadOnlyList<string> ProficientSkills,
        DerivedStats Derived,
        IReadOnlyList<string> ActiveInventoryItemIds,
        IReadOnlyList<string> Errors)
    {
        public static LoadStateResult FromError(string error)
        {
            return new LoadStateResult(
                0,
                new Dictionary<string, int>(),
                Array.Empty<string>(),
                new DerivedStats(0, 0, new Dictionary<string, int>(), new Dictionary<string, int>(), Array.Empty<string>()),
                Array.Empty<string>(),
                new[] { error });
        }
    }
}
