using DndApp.Api.Items;

namespace DndApp.Api.Characters;

public sealed record AddInventoryItemRequest(string ItemDefinitionId, int Quantity = 1);

public sealed record PatchInventoryItemStateRequest(bool? IsEquipped, bool? IsAttuned, int? Quantity = null);

public sealed record CharacterInventoryItemData(
    string InventoryItemId,
    string ItemDefinitionId,
    string ItemName,
    string ItemType,
    decimal GoldValue,
    decimal Weight,
    int Quantity,
    bool RequiresAttunement,
    bool IsEquipped,
    bool IsAttuned,
    bool IsWeapon,
    string DamageDice,
    string WeaponAbility,
    int AttackBonus,
    int DamageBonus);

public sealed record CharacterInventoryState(
    Guid CharacterId,
    IReadOnlyList<CharacterInventoryItemData> Items,
    ItemEffectPipelineResult PipelineResult,
    int ActiveAttunementCount,
    int AttunementCap,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);
