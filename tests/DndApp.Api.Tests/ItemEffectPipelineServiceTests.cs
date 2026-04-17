using DndApp.Api.Items;

namespace DndApp.Api.Tests;

public sealed class ItemEffectPipelineServiceTests
{
    [Fact]
    public void ApplyEffects_IgnoresUnequippedItems()
    {
        var service = new ItemEffectPipelineService();
        var request = new ApplyItemEffectsRequest(
            new BaseStats(
                ArmorClass: 15,
                MoveSpeed: 30,
                SavingThrows: new Dictionary<string, int> { ["Dexterity"] = 2 },
                AbilityChecks: new Dictionary<string, int> { ["Stealth"] = 3 },
                AvailableSpells: Array.Empty<string>()),
            new[]
            {
                new CharacterItemState(
                    "cloak-1",
                    "Cloak of Protection",
                    RequiresAttunement: true,
                    IsEquipped: false,
                    IsAttuned: true,
                    Effects: new[]
                    {
                        new ItemEffect(ItemEffectType.AcBonus, null, 1, null, "+1 AC")
                    })
            });

        var result = service.ApplyEffects(request);

        Assert.Equal(15, result.DerivedStats.ArmorClass);
        Assert.Empty(result.Breakdown);
    }

    [Fact]
    public void UpdateItemState_EnforcesAttunementCap()
    {
        var service = new ItemEffectPipelineService();
        var request = new UpdateInventoryItemStateRequest(
            new BaseStats(
                ArmorClass: 10,
                MoveSpeed: 30,
                SavingThrows: new Dictionary<string, int>(),
                AbilityChecks: new Dictionary<string, int>(),
                AvailableSpells: Array.Empty<string>()),
            new[]
            {
                new CharacterItemState("i1","Item 1", true, true, true, Array.Empty<ItemEffect>()),
                new CharacterItemState("i2","Item 2", true, true, true, Array.Empty<ItemEffect>()),
                new CharacterItemState("i3","Item 3", true, true, true, Array.Empty<ItemEffect>()),
                new CharacterItemState("i4","Item 4", true, true, false, Array.Empty<ItemEffect>()),
            },
            ItemId: "i4",
            IsEquipped: true,
            IsAttuned: true);

        var result = service.UpdateItemState(request);
        var updated = result.Items.Single(x => x.ItemId == "i4");

        Assert.False(updated.IsAttuned);
        Assert.Contains(result.Errors, e => e.Contains("Attunement cap", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, result.ActiveAttunementCount);
    }
}
