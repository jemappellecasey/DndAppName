using DndApp.Api.Items;

namespace DndApp.Api.Characters;

public sealed record AddInventoryItemRequest(string ItemDefinitionId);

public sealed record PatchInventoryItemStateRequest(bool? IsEquipped, bool? IsAttuned);

public sealed record CharacterInventoryItemData(
    string InventoryItemId,
    string ItemDefinitionId,
    string ItemName,
    bool RequiresAttunement,
    bool IsEquipped,
    bool IsAttuned);

public sealed record CharacterInventoryState(
    Guid CharacterId,
    IReadOnlyList<CharacterInventoryItemData> Items,
    ItemEffectPipelineResult PipelineResult,
    int ActiveAttunementCount,
    int AttunementCap,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);
