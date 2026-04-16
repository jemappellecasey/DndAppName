using System.Text.RegularExpressions;

namespace DndApp.Api.Mechanics;

public interface ICalculationEngineService
{
    CheckResult ComputeCheck(ComputeCheckRequest request);
    SaveResult ComputeSave(ComputeSaveRequest request);
    AttackResult ComputeAttack(ComputeAttackRequest request);
}

public sealed class CalculationEngineService : ICalculationEngineService
{
    public CheckResult ComputeCheck(ComputeCheckRequest request)
    {
        var proficiency = request.IsProficient ? request.ProficiencyBonus : 0;
        if (request.HasExpertise && request.IsProficient)
        {
            proficiency *= 2;
        }

        var totalModifier = request.AbilityModifier + proficiency + request.AdditionalModifier;
        var roll = BuildD20Roll(request.AdvantageState, request.RollDice);
        var total = roll.ChosenRoll.HasValue ? roll.ChosenRoll + totalModifier : null;

        return new CheckResult(request.SkillName, totalModifier, roll, total);
    }

    public SaveResult ComputeSave(ComputeSaveRequest request)
    {
        var proficiency = request.IsProficient ? request.ProficiencyBonus : 0;
        var totalModifier = request.AbilityModifier + proficiency + request.AdditionalModifier;
        var roll = BuildD20Roll(request.AdvantageState, request.RollDice);
        var total = roll.ChosenRoll.HasValue ? roll.ChosenRoll + totalModifier : null;

        return new SaveResult(request.AbilityName, totalModifier, roll, total);
    }

    public AttackResult ComputeAttack(ComputeAttackRequest request)
    {
        var proficiency = request.IsProficientWithWeapon ? request.ProficiencyBonus : 0;
        var attackModifier = request.AbilityModifier + proficiency + request.AdditionalAttackModifier;
        var attackRoll = BuildD20Roll(request.AdvantageState, request.RollDice);
        var attackTotal = attackRoll.ChosenRoll.HasValue ? attackRoll.ChosenRoll + attackModifier : null;

        var damageModifier = request.AbilityModifier + request.AdditionalDamageModifier;
        int? damageRoll = null;
        int? damageTotal = null;
        if (request.RollDice)
        {
            damageRoll = RollDiceNotation(request.DamageDice);
            damageTotal = damageRoll + damageModifier;
        }

        return new AttackResult(
            request.WeaponName,
            attackModifier,
            attackRoll,
            attackTotal,
            request.DamageDice,
            damageModifier,
            damageRoll,
            damageTotal);
    }

    private static RollDetails BuildD20Roll(AdvantageState advantageState, bool rollDice)
    {
        if (!rollDice)
        {
            return new RollDetails(null, null, null, "No roll requested");
        }

        var a = Random.Shared.Next(1, 21);
        return advantageState switch
        {
            AdvantageState.Advantage => BuildDualRoll(a, true),
            AdvantageState.Disadvantage => BuildDualRoll(a, false),
            _ => new RollDetails(a, null, a, "Normal roll"),
        };
    }

    private static RollDetails BuildDualRoll(int first, bool chooseHigher)
    {
        var second = Random.Shared.Next(1, 21);
        var chosen = chooseHigher ? Math.Max(first, second) : Math.Min(first, second);
        var reason = chooseHigher ? "Advantage" : "Disadvantage";
        return new RollDetails(first, second, chosen, reason);
    }

    private static int RollDiceNotation(string dice)
    {
        var match = Regex.Match(dice.Trim(), @"^(?<count>\d+)d(?<sides>\d+)$", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            throw new ArgumentException($"Unsupported damage dice format '{dice}'. Expected NdM format, e.g. 1d8.");
        }

        var count = int.Parse(match.Groups["count"].Value);
        var sides = int.Parse(match.Groups["sides"].Value);
        if (count <= 0 || sides <= 0)
        {
            throw new ArgumentException("Dice count and sides must be positive.");
        }

        var total = 0;
        for (var i = 0; i < count; i++)
        {
            total += Random.Shared.Next(1, sides + 1);
        }

        return total;
    }
}
