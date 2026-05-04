namespace DndApp.Api.Data;

public sealed class RuleModuleEntity
{
    public string Id { get; set; } = string.Empty;
    public string ContentSourceId { get; set; } = string.Empty;
    public string ModuleType { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string VersionTag { get; set; } = string.Empty;
}

public sealed class RuleVariantEntity
{
    public string Id { get; set; } = string.Empty;
    public string RuleModuleId { get; set; } = string.Empty;
    public string RuleSystemId { get; set; } = string.Empty;
    public string CompatibilityTagsJson { get; set; } = "[]";
    public string PayloadJson { get; set; } = "{}";
}

public sealed class PrerequisiteEntity
{
    public string Id { get; set; } = string.Empty;
    public string RuleModuleId { get; set; } = string.Empty;
    public string PredicateJson { get; set; } = "{}";
}

public sealed class ConstraintEntity
{
    public string Id { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string ConstraintJson { get; set; } = "{}";
}

public sealed class ItemDefinitionEntity
{
    public string Id { get; set; } = string.Empty;
    public string RuleModuleId { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
    public bool RequiresAttunement { get; set; }
    public decimal GoldValue { get; set; }
    public decimal Weight { get; set; }
    public bool IsWeapon { get; set; }
    public string DamageDice { get; set; } = string.Empty;
    public string WeaponAbility { get; set; } = string.Empty;
    public int AttackBonus { get; set; }
    public int DamageBonus { get; set; }
    public string ChargesModelJson { get; set; } = "{}";
}

public sealed class ItemEffectEntity
{
    public string Id { get; set; } = string.Empty;
    public string ItemDefinitionId { get; set; } = string.Empty;
    public string EffectType { get; set; } = string.Empty;
    public string EffectPayloadJson { get; set; } = "{}";
    public string ConditionJson { get; set; } = "{}";
}

// Automatic Spell Grant Entities

public sealed class ClassSpellGrantEntity
{
    public string Id { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty; // "2014" or "2024"
    public string SpellId { get; set; } = string.Empty;
    public int MinLevel { get; set; }
    public bool IsAlwaysPrepared { get; set; }
    public string SourceDescription { get; set; } = string.Empty;
}

public sealed class SubclassSpellGrantEntity
{
    public string Id { get; set; } = string.Empty;
    public string SubclassId { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty; // "2014" or "2024"
    public string SpellId { get; set; } = string.Empty;
    public int MinLevel { get; set; }
    public string SourceDescription { get; set; } = string.Empty;
}

public sealed class RaceSpellGrantEntity
{
    public string Id { get; set; } = string.Empty;
    public string RaceOrSpeciesId { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty; // "2014" or "2024"
    public string SpellId { get; set; } = string.Empty;
    public string SourceDescription { get; set; } = string.Empty;
}

public sealed class BackgroundSpellGrantEntity
{
    public string Id { get; set; } = string.Empty;
    public string BackgroundId { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty; // "2014" or "2024"
    public string SpellId { get; set; } = string.Empty;
    public string SourceDescription { get; set; } = string.Empty;
}

public sealed class OriginSpellGrantEntity
{
    public string Id { get; set; } = string.Empty;
    public string OriginId { get; set; } = string.Empty;
    public string SpellId { get; set; } = string.Empty;
    public string SourceDescription { get; set; } = string.Empty;
}

public sealed class FeatSpellGrantEntity
{
    public string Id { get; set; } = string.Empty;
    public string FeatId { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty; // "2014" or "2024"
    public string GrantType { get; set; } = string.Empty; // "Fixed", "Selection", "SpellsKnown"
    public string SpellIdsJson { get; set; } = "[]"; // Array of spell IDs
    public int SelectionCount { get; set; } // How many to choose if Selection/SpellsKnown
    public string SourceDescription { get; set; } = string.Empty;
}
