using System.Text.Json;
using DndApp.Api.Data;
using DndApp.Api.Items;
using Microsoft.EntityFrameworkCore;

namespace DndApp.Api.Characters;

public interface ICharacterInventoryService
{
    Task<CharacterInventoryState?> GetInventoryAsync(Guid characterId, CancellationToken cancellationToken);
    Task<(CharacterInventoryState? State, IReadOnlyList<string> Errors)> AddItemAsync(Guid characterId, AddInventoryItemRequest request, CancellationToken cancellationToken);
    Task<(CharacterInventoryState? State, IReadOnlyList<string> Errors)> UpdateItemStateAsync(Guid characterId, string inventoryItemId, PatchInventoryItemStateRequest request, CancellationToken cancellationToken);
    Task<bool> RemoveItemAsync(Guid characterId, string inventoryItemId, CancellationToken cancellationToken);
}

public sealed class CharacterInventoryService : ICharacterInventoryService
{
    private readonly AppDbContext _db;
    private readonly IItemEffectPipelineService _pipeline;

    public CharacterInventoryService(AppDbContext db, IItemEffectPipelineService pipeline)
    {
        _db = db;
        _pipeline = pipeline;
    }

    public Task<CharacterInventoryState?> GetInventoryAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return BuildInventoryStateAsync(characterId, Array.Empty<string>(), Array.Empty<string>(), cancellationToken);
    }

    public async Task<(CharacterInventoryState? State, IReadOnlyList<string> Errors)> AddItemAsync(
        Guid characterId,
        AddInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ItemDefinitionId))
        {
            return (null, new[] { "Item definition id is required." });
        }

        var characterIdText = characterId.ToString();
        var hasBuild = await _db.CharacterSheets.AnyAsync(x => x.CharacterId == characterIdText, cancellationToken);
        if (!hasBuild)
        {
            return (null, new[] { "Character build was not found. Create character build before inventory operations." });
        }

        var itemDefinition = await _db.ItemDefinitions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.ItemDefinitionId, cancellationToken);
        if (itemDefinition is null)
        {
            return (null, new[] { $"Item definition '{request.ItemDefinitionId}' was not found." });
        }

        var itemName = await (
            from module in _db.RuleModules
            where module.Id == itemDefinition.RuleModuleId
            select module.DisplayName)
            .SingleOrDefaultAsync(cancellationToken) ?? request.ItemDefinitionId;

        var now = DateTimeOffset.UtcNow;
        var inventoryItem = new CharacterInventoryItemEntity
        {
            InventoryItemId = Guid.NewGuid().ToString("N"),
            CharacterId = characterIdText,
            ItemDefinitionId = itemDefinition.Id,
            ItemName = itemName,
            RequiresAttunement = itemDefinition.RequiresAttunement,
            IsEquipped = false,
            IsAttuned = false,
            AddedAtUtc = now,
            UpdatedAtUtc = now,
        };

        _db.CharacterInventoryItems.Add(inventoryItem);
        await _db.SaveChangesAsync(cancellationToken);

        var state = await BuildInventoryStateAsync(characterId, Array.Empty<string>(), Array.Empty<string>(), cancellationToken);
        return (state, Array.Empty<string>());
    }

    public async Task<(CharacterInventoryState? State, IReadOnlyList<string> Errors)> UpdateItemStateAsync(
        Guid characterId,
        string inventoryItemId,
        PatchInventoryItemStateRequest request,
        CancellationToken cancellationToken)
    {
        var characterIdText = characterId.ToString();
        var inventory = await _db.CharacterInventoryItems
            .Where(x => x.CharacterId == characterIdText)
            .ToListAsync(cancellationToken);
        inventory = inventory.OrderBy(x => x.AddedAtUtc).ToList();
        if (inventory.Count == 0)
        {
            return (null, new[] { "Inventory is empty." });
        }

        var target = inventory.SingleOrDefault(x => x.InventoryItemId == inventoryItemId);
        if (target is null)
        {
            return (null, new[] { $"Inventory item '{inventoryItemId}' was not found." });
        }

        var itemDefinitions = await _db.ItemDefinitions.AsNoTracking()
            .Where(x => inventory.Select(y => y.ItemDefinitionId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var effects = await _db.ItemEffects.AsNoTracking()
            .Where(x => inventory.Select(y => y.ItemDefinitionId).Contains(x.ItemDefinitionId))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var effectsByDefinition = effects
            .GroupBy(x => x.ItemDefinitionId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.OrdinalIgnoreCase);

        var itemStates = inventory.Select(row =>
        {
            var definition = itemDefinitions[row.ItemDefinitionId];
            var rowEffects = effectsByDefinition.TryGetValue(row.ItemDefinitionId, out var foundEffects)
                ? foundEffects
                : Array.Empty<ItemEffectEntity>();
            return new CharacterItemState(
                ItemId: row.InventoryItemId,
                ItemName: row.ItemName,
                RequiresAttunement: definition.RequiresAttunement,
                IsEquipped: row.IsEquipped,
                IsAttuned: row.IsAttuned,
                Effects: rowEffects.Select(MapEffect).ToArray());
        }).ToArray();

        var updateResult = _pipeline.UpdateItemState(new UpdateInventoryItemStateRequest(
            BaseStats: BuildBaseStats(),
            Items: itemStates,
            ItemId: inventoryItemId,
            IsEquipped: request.IsEquipped,
            IsAttuned: request.IsAttuned));

        var updatedById = updateResult.Items.ToDictionary(x => x.ItemId, StringComparer.OrdinalIgnoreCase);
        var now = DateTimeOffset.UtcNow;
        foreach (var row in inventory)
        {
            if (!updatedById.TryGetValue(row.InventoryItemId, out var updated))
            {
                continue;
            }
            row.IsEquipped = updated.IsEquipped;
            row.IsAttuned = updated.IsAttuned;
            row.UpdatedAtUtc = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
        var state = await BuildInventoryStateAsync(characterId, updateResult.Warnings, updateResult.Errors, cancellationToken);
        return (state, Array.Empty<string>());
    }

    public async Task<bool> RemoveItemAsync(Guid characterId, string inventoryItemId, CancellationToken cancellationToken)
    {
        var characterIdText = characterId.ToString();
        var target = await _db.CharacterInventoryItems.SingleOrDefaultAsync(
            x => x.CharacterId == characterIdText && x.InventoryItemId == inventoryItemId,
            cancellationToken);
        if (target is null)
        {
            return false;
        }

        _db.CharacterInventoryItems.Remove(target);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<CharacterInventoryState?> BuildInventoryStateAsync(
        Guid characterId,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> errors,
        CancellationToken cancellationToken)
    {
        var characterIdText = characterId.ToString();
        var hasBuild = await _db.CharacterSheets.AnyAsync(x => x.CharacterId == characterIdText, cancellationToken);
        if (!hasBuild)
        {
            return null;
        }

        var inventory = await _db.CharacterInventoryItems.AsNoTracking()
            .Where(x => x.CharacterId == characterIdText)
            .ToListAsync(cancellationToken);
        inventory = inventory.OrderBy(x => x.AddedAtUtc).ToList();

        var definitionIds = inventory.Select(x => x.ItemDefinitionId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var definitions = await _db.ItemDefinitions.AsNoTracking()
            .Where(x => definitionIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var effects = await _db.ItemEffects.AsNoTracking()
            .Where(x => definitionIds.Contains(x.ItemDefinitionId))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var effectsByDefinition = effects
            .GroupBy(x => x.ItemDefinitionId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.OrdinalIgnoreCase);

        var itemStates = inventory.Select(row =>
        {
            var definition = definitions[row.ItemDefinitionId];
            var rowEffects = effectsByDefinition.TryGetValue(row.ItemDefinitionId, out var foundEffects)
                ? foundEffects
                : Array.Empty<ItemEffectEntity>();
            return new CharacterItemState(
                ItemId: row.InventoryItemId,
                ItemName: row.ItemName,
                RequiresAttunement: definition.RequiresAttunement,
                IsEquipped: row.IsEquipped,
                IsAttuned: row.IsAttuned,
                Effects: rowEffects.Select(MapEffect).ToArray());
        }).ToArray();

        var pipelineResult = _pipeline.ApplyEffects(new ApplyItemEffectsRequest(BuildBaseStats(), itemStates));
        var activeAttunementCount = itemStates.Count(x => x.RequiresAttunement && x.IsEquipped && x.IsAttuned);
        var inventoryRows = itemStates.Select(x => new CharacterInventoryItemData(
            x.ItemId,
            inventory.Single(row => row.InventoryItemId == x.ItemId).ItemDefinitionId,
            x.ItemName,
            x.RequiresAttunement,
            x.IsEquipped,
            x.IsAttuned)).ToArray();

        return new CharacterInventoryState(
            characterId,
            inventoryRows,
            pipelineResult,
            activeAttunementCount,
            3,
            warnings,
            errors);
    }

    private static BaseStats BuildBaseStats()
    {
        return new BaseStats(
            ArmorClass: 10,
            MoveSpeed: 30,
            SavingThrows: new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            AbilityChecks: new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            AvailableSpells: Array.Empty<string>());
    }

    private static ItemEffect MapEffect(ItemEffectEntity effect)
    {
        var payload = ParseObject(effect.EffectPayloadJson);
        var numericValue = ReadInt(payload, "numericValue")
            ?? ReadInt(payload, "value")
            ?? ReadInt(payload, "bonus")
            ?? ReadInt(payload, "modifier")
            ?? 0;
        var target = ReadString(payload, "target")
            ?? ReadString(payload, "ability")
            ?? ReadString(payload, "skill");
        var grantedSpell = ReadString(payload, "grantedSpell")
            ?? ReadString(payload, "spellName");
        var description = ReadString(payload, "description") ?? effect.EffectType;

        return new ItemEffect(
            MapEffectType(effect.EffectType),
            target,
            numericValue,
            grantedSpell,
            description);
    }

    private static ItemEffectType MapEffectType(string effectType)
    {
        var normalized = effectType.Trim().ToLowerInvariant();
        if (normalized.Contains("ac"))
        {
            return ItemEffectType.AcBonus;
        }
        if (normalized.Contains("move") || normalized.Contains("speed"))
        {
            return ItemEffectType.MoveSpeedBonus;
        }
        if (normalized.Contains("saving"))
        {
            return ItemEffectType.SavingThrowBonus;
        }
        if (normalized.Contains("spell"))
        {
            return ItemEffectType.GrantSpell;
        }
        return ItemEffectType.AbilityCheckBonus;
    }

    private static Dictionary<string, JsonElement>? ParseObject(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return doc.RootElement.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.Clone(), StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return null;
        }
    }

    private static int? ReadInt(Dictionary<string, JsonElement>? payload, string key)
    {
        if (payload is null || !payload.TryGetValue(key, out var value))
        {
            return null;
        }
        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var n) => n,
            JsonValueKind.String when int.TryParse(value.GetString(), out var n) => n,
            _ => null
        };
    }

    private static string? ReadString(Dictionary<string, JsonElement>? payload, string key)
    {
        if (payload is null || !payload.TryGetValue(key, out var value))
        {
            return null;
        }
        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
