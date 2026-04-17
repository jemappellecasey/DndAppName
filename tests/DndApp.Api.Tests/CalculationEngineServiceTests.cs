using DndApp.Api.Mechanics;

namespace DndApp.Api.Tests;

public sealed class CalculationEngineServiceTests
{
    [Fact]
    public void ComputeCheck_ExpertiseDoublesProficiency_WhenRollDisabled()
    {
        var service = new CalculationEngineService();
        var request = new ComputeCheckRequest(
            "Stealth",
            AbilityModifier: 3,
            ProficiencyBonus: 2,
            IsProficient: true,
            HasExpertise: true,
            AdditionalModifier: 1,
            AdvantageState.None,
            RollDice: false);

        var result = service.ComputeCheck(request);

        Assert.Equal("Stealth", result.SkillName);
        Assert.Equal(8, result.TotalModifier);
        Assert.Null(result.Total);
    }

    [Fact]
    public void ComputeAttack_UsesProficiencyWhenWeaponProficient()
    {
        var service = new CalculationEngineService();
        var request = new ComputeAttackRequest(
            "Longsword",
            AbilityModifier: 4,
            ProficiencyBonus: 3,
            IsProficientWithWeapon: true,
            AdditionalAttackModifier: 1,
            DamageDice: "1d8",
            AdditionalDamageModifier: 2,
            AdvantageState.None,
            RollDice: false);

        var result = service.ComputeAttack(request);

        Assert.Equal(8, result.AttackModifier);
        Assert.Equal(6, result.DamageModifier);
        Assert.Null(result.AttackTotal);
        Assert.Null(result.DamageTotal);
    }
}
