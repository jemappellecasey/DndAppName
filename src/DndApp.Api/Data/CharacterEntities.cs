namespace DndApp.Api.Data;

public sealed class CharacterSheetEntity
{
    public string CharacterId { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public string BaseRuleSystem { get; set; } = string.Empty;
    public string BuildMethod { get; set; } = string.Empty;
    public string ClassModuleId { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public int Level { get; set; }
    public int ProficiencyBonus { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class CharacterAbilityScoreEntity
{
    public string CharacterId { get; set; } = string.Empty;
    public string AbilityName { get; set; } = string.Empty;
    public int Score { get; set; }
}

public sealed class CharacterSkillProficiencyEntity
{
    public string CharacterId { get; set; } = string.Empty;
    public string SkillName { get; set; } = string.Empty;
}

public sealed class CharacterInventoryItemEntity
{
    public string InventoryItemId { get; set; } = string.Empty;
    public string CharacterId { get; set; } = string.Empty;
    public string ItemDefinitionId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public bool RequiresAttunement { get; set; }
    public int Quantity { get; set; }
    public bool IsEquipped { get; set; }
    public bool IsAttuned { get; set; }
    public DateTimeOffset AddedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class CharacterRecordEntity
{
    public string CharacterId { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public string BaseRuleSystem { get; set; } = string.Empty;
    public bool MixedModeEnabled { get; set; }
    public string OverlaySourcesJson { get; set; } = "[]";
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class CharacterDraftEntity
{
    public string CharacterId { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public string BaseRuleSystem { get; set; } = string.Empty;
    public bool MixedModeEnabled { get; set; }
    public string OverlaySourcesJson { get; set; } = "[]";
    public bool IsFinalized { get; set; }
    public string StepsJson { get; set; } = "[]";
    public string WarningsJson { get; set; } = "[]";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class CharacterHistoryEntity
{
    public string EntryId { get; set; } = string.Empty;
    public string CharacterId { get; set; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
