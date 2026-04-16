namespace DndApp.Api.Items;

public interface IItemEffectPipelineService
{
    ItemEffectPipelineResult ApplyEffects(ApplyItemEffectsRequest request);
}

public sealed class ItemEffectPipelineService : IItemEffectPipelineService
{
    public ItemEffectPipelineResult ApplyEffects(ApplyItemEffectsRequest request)
    {
        var armorClass = request.BaseStats.ArmorClass;
        var moveSpeed = request.BaseStats.MoveSpeed;
        var savingThrows = new Dictionary<string, int>(request.BaseStats.SavingThrows, StringComparer.OrdinalIgnoreCase);
        var abilityChecks = new Dictionary<string, int>(request.BaseStats.AbilityChecks, StringComparer.OrdinalIgnoreCase);
        var availableSpells = new HashSet<string>(request.BaseStats.AvailableSpells, StringComparer.OrdinalIgnoreCase);
        var breakdown = new List<ItemEffectBreakdownEntry>();

        foreach (var item in request.Items)
        {
            var isActive = item.IsEquipped && (!item.RequiresAttunement || item.IsAttuned);
            if (!isActive)
            {
                continue;
            }

            foreach (var effect in item.Effects)
            {
                var target = effect.Target ?? string.Empty;
                switch (effect.Type)
                {
                    case ItemEffectType.AcBonus:
                        armorClass += effect.NumericValue;
                        break;
                    case ItemEffectType.MoveSpeedBonus:
                        moveSpeed += effect.NumericValue;
                        break;
                    case ItemEffectType.SavingThrowBonus:
                        ApplyNamedBonus(savingThrows, target, effect.NumericValue);
                        break;
                    case ItemEffectType.AbilityCheckBonus:
                        ApplyNamedBonus(abilityChecks, target, effect.NumericValue);
                        break;
                    case ItemEffectType.GrantSpell:
                        if (!string.IsNullOrWhiteSpace(effect.GrantedSpell))
                        {
                            availableSpells.Add(effect.GrantedSpell);
                        }
                        break;
                }

                breakdown.Add(
                    new ItemEffectBreakdownEntry(
                        item.ItemName,
                        effect.Type.ToString(),
                        target,
                        effect.NumericValue,
                        effect.Description));
            }
        }

        var derived = new DerivedStats(
            armorClass,
            moveSpeed,
            savingThrows,
            abilityChecks,
            availableSpells.OrderBy(x => x).ToArray());

        return new ItemEffectPipelineResult(derived, breakdown);
    }

    private static void ApplyNamedBonus(IDictionary<string, int> values, string target, int bonus)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return;
        }

        if (values.TryGetValue(target, out var existing))
        {
            values[target] = existing + bonus;
            return;
        }

        values[target] = bonus;
    }
}
