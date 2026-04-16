namespace DndApp.Api.Items;

public enum ItemEffectType
{
    AcBonus,
    MoveSpeedBonus,
    SavingThrowBonus,
    AbilityCheckBonus,
    GrantSpell,
}

public sealed record ItemEffect(
    ItemEffectType Type,
    string? Target,
    int NumericValue,
    string? GrantedSpell,
    string Description);

public sealed record CharacterItemState(
    string ItemId,
    string ItemName,
    bool RequiresAttunement,
    bool IsEquipped,
    bool IsAttuned,
    IReadOnlyList<ItemEffect> Effects);

public sealed record BaseStats(
    int ArmorClass,
    int MoveSpeed,
    Dictionary<string, int> SavingThrows,
    Dictionary<string, int> AbilityChecks,
    IReadOnlyList<string> AvailableSpells);

public sealed record ApplyItemEffectsRequest(
    BaseStats BaseStats,
    IReadOnlyList<CharacterItemState> Items);

public sealed record ItemEffectBreakdownEntry(
    string ItemName,
    string EffectType,
    string Target,
    int Value,
    string Description);

public sealed record DerivedStats(
    int ArmorClass,
    int MoveSpeed,
    Dictionary<string, int> SavingThrows,
    Dictionary<string, int> AbilityChecks,
    IReadOnlyList<string> AvailableSpells);

public sealed record ItemEffectPipelineResult(
    DerivedStats DerivedStats,
    IReadOnlyList<ItemEffectBreakdownEntry> Breakdown);
