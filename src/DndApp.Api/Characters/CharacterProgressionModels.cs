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

public sealed record SpellSourceGroup(
    string SourceName,
    int PrepareCount,
    IReadOnlyList<CharacterSpellEntryData> AutomaticSpells,
    IReadOnlyList<CharacterSpellEntryData> SelectableSpells);

public sealed record RecommendedSpellsResult(
    Guid CharacterId,
    string ClassModuleId,
    int ClassLevel,
    IReadOnlyList<SpellSourceGroup> SpellSources,
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

public sealed record CharacterCurrencyData(
    Guid CharacterId,
    int Cp,
    int Sp,
    int Ep,
    int Gp,
    int Pp);

public sealed record UpsertCharacterCurrencyRequest(
    int Cp,
    int Sp,
    int Ep,
    int Gp,
    int Pp);

public sealed record ConvertCurrencyRequest(
    string FromDenomination,
    string ToDenomination,
    int Amount);

public sealed record ConsolidateCurrencyRequest(
    bool PreferPlatinum);

public sealed record PurchaseFromCurrencyRequest(
    decimal CostInGold,
    int Quantity);

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

// Automatic Spell Grant Models

public sealed record SpellVariantComparison(
    string SpellSlug,
    string SpellName,
    SpellVariantData? Variant2014,
    SpellVariantData? Variant2024,
    bool BothEditionsAvailable);

public sealed record SpellVariantData(
    string SpellId,
    string Name,
    int Level,
    string School,
    string CastingTime,
    string RangeText,
    string Duration,
    bool Ritual,
    bool Concentration,
    string Description);

public sealed record AutomaticSpellGrant(
    string SpellId,
    string SpellName,
    string SourceFeature,
    string SourceType,
    int MinLevel,
    bool HasMultipleEditions,
    SpellVariantComparison? Variants);

public sealed record FeatSpellChoice(
    string FeatId,
    string FeatName,
    string GrantType,
    int SelectionCount,
    IReadOnlyList<string> AvailableSpellIds);
