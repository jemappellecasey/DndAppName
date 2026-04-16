namespace DndApp.Api.Mechanics;

public enum AdvantageState
{
    None,
    Advantage,
    Disadvantage,
}

public sealed record ComputeCheckRequest(
    string SkillName,
    int AbilityModifier,
    int ProficiencyBonus,
    bool IsProficient,
    bool HasExpertise,
    int AdditionalModifier,
    AdvantageState AdvantageState,
    bool RollDice);

public sealed record ComputeSaveRequest(
    string AbilityName,
    int AbilityModifier,
    int ProficiencyBonus,
    bool IsProficient,
    int AdditionalModifier,
    AdvantageState AdvantageState,
    bool RollDice);

public sealed record ComputeAttackRequest(
    string WeaponName,
    int AbilityModifier,
    int ProficiencyBonus,
    bool IsProficientWithWeapon,
    int AdditionalAttackModifier,
    string DamageDice,
    int AdditionalDamageModifier,
    AdvantageState AdvantageState,
    bool RollDice);

public sealed record RollDetails(
    int? RollA,
    int? RollB,
    int? ChosenRoll,
    string Reason);

public sealed record CheckResult(
    string SkillName,
    int TotalModifier,
    RollDetails Roll,
    int? Total);

public sealed record SaveResult(
    string AbilityName,
    int TotalModifier,
    RollDetails Roll,
    int? Total);

public sealed record AttackResult(
    string WeaponName,
    int AttackModifier,
    RollDetails AttackRoll,
    int? AttackTotal,
    string DamageDice,
    int DamageModifier,
    int? DamageRoll,
    int? DamageTotal);
