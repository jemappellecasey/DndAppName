using DndApp.Api.Mechanics;

namespace DndApp.Api.Characters;

public sealed record PersistedComputeCheckRequest(
    string SkillName,
    AdvantageState AdvantageState,
    bool RollDice,
    int AdditionalModifier,
    bool HasExpertise);

public sealed record PersistedComputeSaveRequest(
    string AbilityName,
    AdvantageState AdvantageState,
    bool RollDice,
    int AdditionalModifier,
    bool? IsProficient);

public sealed record PersistedComputeAttackRequest(
    string WeaponName,
    string AbilityName,
    bool IsProficientWithWeapon,
    int AdditionalAttackModifier,
    string DamageDice,
    int AdditionalDamageModifier,
    AdvantageState AdvantageState,
    bool RollDice);

public sealed record CharacterDerivedStatsResponse(
    Guid CharacterId,
    int ArmorClass,
    int MoveSpeed,
    int MaxHitPoints,
    int CurrentHitPoints,
    int TempHitPoints,
    IReadOnlyDictionary<string, int> SavingThrows,
    IReadOnlyDictionary<string, int> AbilityChecks,
    IReadOnlyList<string> AvailableSpells,
    IReadOnlyList<string> ActiveInventoryItemIds,
    IReadOnlyList<string> SaveProficiencies);
