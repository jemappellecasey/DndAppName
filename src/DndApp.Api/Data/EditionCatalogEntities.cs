namespace DndApp.Api.Data;

public sealed class Race2014Entity
{
    public string Id { get; set; } = string.Empty;
    public string ContentSourceId { get; set; } = string.Empty;
    public string LegacyRuleModuleId { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsSubrace { get; set; }
    public string ParentRaceSlug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AbilityBonusesJson { get; set; } = "{}";
    public string LanguagesJson { get; set; } = "[]";
    public string TraitsJson { get; set; } = "[]";
    public string EditionPayloadJson { get; set; } = "{}";
}

public sealed class Species2024Entity
{
    public string Id { get; set; } = string.Empty;
    public string ContentSourceId { get; set; } = string.Empty;
    public string LegacyRuleModuleId { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AbilityBonusesJson { get; set; } = "{}";
    public string LanguagesJson { get; set; } = "[]";
    public string TraitsJson { get; set; } = "[]";
    public string EditionPayloadJson { get; set; } = "{}";
}

public sealed class Background2014Entity
{
    public string Id { get; set; } = string.Empty;
    public string ContentSourceId { get; set; } = string.Empty;
    public string LegacyRuleModuleId { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SkillProficienciesJson { get; set; } = "[]";
    public string ToolProficienciesJson { get; set; } = "[]";
    public string LanguageChoicesJson { get; set; } = "[]";
    public string EquipmentJson { get; set; } = "[]";
    public string EditionPayloadJson { get; set; } = "{}";
}

public sealed class Background2024Entity
{
    public string Id { get; set; } = string.Empty;
    public string ContentSourceId { get; set; } = string.Empty;
    public string LegacyRuleModuleId { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SkillProficienciesJson { get; set; } = "[]";
    public string ToolProficienciesJson { get; set; } = "[]";
    public string LanguageChoicesJson { get; set; } = "[]";
    public string GrantedFeatSlug { get; set; } = string.Empty;
    public string EditionPayloadJson { get; set; } = "{}";
}

public sealed class Feat2014Entity
{
    public string Id { get; set; } = string.Empty;
    public string ContentSourceId { get; set; } = string.Empty;
    public string LegacyRuleModuleId { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PrerequisitesJson { get; set; } = "{}";
    public string EditionPayloadJson { get; set; } = "{}";
}

public sealed class Feat2024Entity
{
    public string Id { get; set; } = string.Empty;
    public string ContentSourceId { get; set; } = string.Empty;
    public string LegacyRuleModuleId { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string PrerequisitesJson { get; set; } = "{}";
    public string EditionPayloadJson { get; set; } = "{}";
}

public sealed class SpellEntity
{
    public string Id { get; set; } = string.Empty;
    public string ContentSourceId { get; set; } = string.Empty;
    public string LegacyRuleModuleId { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int EditionYear { get; set; }
    public int Level { get; set; }
    public string School { get; set; } = string.Empty;
    public string CastingTime { get; set; } = string.Empty;
    public string RangeText { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public bool Ritual { get; set; }
    public bool Concentration { get; set; }
    public string Description { get; set; } = string.Empty;
    public string EditionPayloadJson { get; set; } = "{}";
}

public sealed class Item2014Entity
{
    public string Id { get; set; } = string.Empty;
    public string ContentSourceId { get; set; } = string.Empty;
    public string LegacyRuleModuleId { get; set; } = string.Empty;
    public string LegacyItemDefinitionId { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
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
    public string Description { get; set; } = string.Empty;
    public string EditionPayloadJson { get; set; } = "{}";
}

public sealed class Item2024Entity
{
    public string Id { get; set; } = string.Empty;
    public string ContentSourceId { get; set; } = string.Empty;
    public string LegacyRuleModuleId { get; set; } = string.Empty;
    public string LegacyItemDefinitionId { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
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
    public string Description { get; set; } = string.Empty;
    public string EditionPayloadJson { get; set; } = "{}";
}
