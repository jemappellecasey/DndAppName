namespace DndApp.Api.Items;

public interface IItemEffectPipelineService
{
    ItemEffectPipelineResult ApplyEffects(ApplyItemEffectsRequest request);
    InventoryStateUpdateResult UpdateItemState(UpdateInventoryItemStateRequest request);
    object GetAttunementGuidance();
}

public sealed class ItemEffectPipelineService : IItemEffectPipelineService
{
    private const int DefaultAttunementCap = 3;

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

    public InventoryStateUpdateResult UpdateItemState(UpdateInventoryItemStateRequest request)
    {
        var warnings = new List<string>();
        var errors = new List<string>();
        var updatedItems = request.Items.ToList();
        var index = updatedItems.FindIndex(x => string.Equals(x.ItemId, request.ItemId, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            errors.Add($"Item '{request.ItemId}' was not found in inventory.");
            return new InventoryStateUpdateResult(
                updatedItems,
                ApplyEffects(new ApplyItemEffectsRequest(request.BaseStats, updatedItems)),
                CountActiveAttunements(updatedItems),
                DefaultAttunementCap,
                warnings,
                errors);
        }

        var item = updatedItems[index];
        var nextEquipped = request.IsEquipped ?? item.IsEquipped;
        var nextAttuned = request.IsAttuned ?? item.IsAttuned;

        if (!item.RequiresAttunement && nextAttuned)
        {
            nextAttuned = false;
            warnings.Add($"Item '{item.ItemName}' does not require attunement; attuned state was ignored.");
        }

        if (!nextEquipped && nextAttuned)
        {
            nextAttuned = false;
            warnings.Add($"Item '{item.ItemName}' cannot remain attuned while unequipped; attuned state was cleared.");
        }

        if (item.RequiresAttunement && nextAttuned)
        {
            var activeAttuned = updatedItems.Count(x => x.ItemId != item.ItemId && x.IsEquipped && x.IsAttuned && x.RequiresAttunement);
            if (activeAttuned >= DefaultAttunementCap)
            {
                nextAttuned = false;
                errors.Add($"Attunement cap of {DefaultAttunementCap} reached; '{item.ItemName}' was not attuned.");
            }
        }

        updatedItems[index] = item with
        {
            IsEquipped = nextEquipped,
            IsAttuned = nextAttuned
        };

        var pipeline = ApplyEffects(new ApplyItemEffectsRequest(request.BaseStats, updatedItems));
        return new InventoryStateUpdateResult(
            updatedItems,
            pipeline,
            CountActiveAttunements(updatedItems),
            DefaultAttunementCap,
            warnings,
            errors);
    }

    public object GetAttunementGuidance()
    {
        return new
        {
            attunementCap = DefaultAttunementCap,
            rules = new[]
            {
                "Only equipped items can be attuned.",
                "Only items that require attunement can be attuned.",
                $"A maximum of {DefaultAttunementCap} attuned items can be active at once."
            }
        };
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

    private static int CountActiveAttunements(IEnumerable<CharacterItemState> items)
    {
        return items.Count(x => x.RequiresAttunement && x.IsEquipped && x.IsAttuned);
    }
}
