namespace DndApp.Api.Characters;

public sealed record CharacterSpellEntryData(
    string SpellModuleId,
    string SpellName,
    string PreparationMode);

public sealed record CharacterSpellsData(
    Guid CharacterId,
    IReadOnlyList<CharacterSpellEntryData> Entries);

public sealed record UpsertCharacterSpellsRequest(
    IReadOnlyList<CharacterSpellEntryData> Entries);

public sealed record RecommendedSpellsResult(
    Guid CharacterId,
    string ClassModuleId,
    int ClassLevel,
    IReadOnlyList<CharacterSpellEntryData> RecommendedSpells,
    string AdvisoryMessage,
    string? DataGap);

public sealed record CharacterResourcePoolData(
    string ResourceKey,
    int CurrentValue,
    int MaxValue,
    string MetadataJson);

public sealed record CharacterResourcesData(
    Guid CharacterId,
    IReadOnlyList<CharacterResourcePoolData> Resources);

public sealed record UpsertCharacterResourcesRequest(
    IReadOnlyList<CharacterResourcePoolData> Resources);

public sealed record CharacterVitalsData(
    Guid CharacterId,
    int MaxHitPoints,
    int CurrentHitPoints,
    int TempHitPoints,
    int BaseMoveSpeed,
    int BaseArmorClass,
    DateTimeOffset UpdatedAtUtc);

public sealed record UpsertCharacterVitalsRequest(
    int MaxHitPoints,
    int CurrentHitPoints,
    int TempHitPoints,
    int BaseMoveSpeed,
    int BaseArmorClass);
