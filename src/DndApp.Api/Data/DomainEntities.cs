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
