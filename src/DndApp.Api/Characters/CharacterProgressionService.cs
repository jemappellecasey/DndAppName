using DndApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DndApp.Api.Characters;

public interface ICharacterProgressionService
{
    Task<CharacterSpellsData?> GetSpellsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<CharacterSpellsData> UpsertSpellsAsync(Guid characterId, UpsertCharacterSpellsRequest request, CancellationToken cancellationToken);
    Task<RecommendedSpellsResult?> GetRecommendedSpellsAsync(Guid characterId, string classModuleId, int classLevel, CancellationToken cancellationToken);
    Task<CharacterResourcesData?> GetResourcesAsync(Guid characterId, CancellationToken cancellationToken);
    Task<CharacterResourcesData> UpsertResourcesAsync(Guid characterId, UpsertCharacterResourcesRequest request, CancellationToken cancellationToken);
    Task<CharacterVitalsData?> GetVitalsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<CharacterVitalsData> UpsertVitalsAsync(Guid characterId, UpsertCharacterVitalsRequest request, CancellationToken cancellationToken);
}

public sealed class CharacterProgressionService : ICharacterProgressionService
{
    private readonly AppDbContext _db;

    public CharacterProgressionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CharacterSpellsData?> GetSpellsAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        var exists = await _db.CharacterSheets.AsNoTracking().AnyAsync(x => x.CharacterId == id, cancellationToken);
        if (!exists)
        {
            return null;
        }

        var entries = await _db.CharacterSpellEntries.AsNoTracking()
            .Where(x => x.CharacterId == id)
            .OrderBy(x => x.PreparationMode)
            .ThenBy(x => x.SpellName)
            .Select(x => new CharacterSpellEntryData(x.SpellModuleId, x.SpellName, x.PreparationMode))
            .ToArrayAsync(cancellationToken);

        return new CharacterSpellsData(characterId, entries);
    }

    public async Task<CharacterSpellsData> UpsertSpellsAsync(Guid characterId, UpsertCharacterSpellsRequest request, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        var current = await _db.CharacterSpellEntries.Where(x => x.CharacterId == id).ToListAsync(cancellationToken);
        _db.CharacterSpellEntries.RemoveRange(current);
        foreach (var entry in request.Entries
                     .Where(x => !string.IsNullOrWhiteSpace(x.SpellName))
                     .DistinctBy(x => $"{x.SpellModuleId}\u001F{x.PreparationMode}", StringComparer.OrdinalIgnoreCase))
        {
            _db.CharacterSpellEntries.Add(new CharacterSpellEntryEntity
            {
                CharacterId = id,
                SpellModuleId = entry.SpellModuleId.Trim(),
                SpellName = entry.SpellName.Trim(),
                PreparationMode = NormalizePreparationMode(entry.PreparationMode),
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await GetSpellsAsync(characterId, cancellationToken) ?? new CharacterSpellsData(characterId, Array.Empty<CharacterSpellEntryData>());
    }

    public async Task<RecommendedSpellsResult?> GetRecommendedSpellsAsync(Guid characterId, string classModuleId, int classLevel, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        var exists = await _db.CharacterSheets.AsNoTracking().AnyAsync(x => x.CharacterId == id, cancellationToken);
        if (!exists)
        {
            return null;
        }

        // Recommended spells are advisory and require a future curated class/level source.
        // We intentionally return a data-gap state until that source is available.
        return new RecommendedSpellsResult(
            characterId,
            classModuleId,
            Math.Max(1, classLevel),
            Array.Empty<CharacterSpellEntryData>(),
            "Recommended spells are advisory only and do not account for multiclassing.",
            "Curated recommended spell list is not available yet for this class/level.");
    }

    public async Task<CharacterResourcesData?> GetResourcesAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        var exists = await _db.CharacterSheets.AsNoTracking().AnyAsync(x => x.CharacterId == id, cancellationToken);
        if (!exists)
        {
            return null;
        }

        var resources = await _db.CharacterResourcePools.AsNoTracking()
            .Where(x => x.CharacterId == id)
            .OrderBy(x => x.ResourceKey)
            .Select(x => new CharacterResourcePoolData(x.ResourceKey, x.CurrentValue, x.MaxValue, x.MetadataJson))
            .ToArrayAsync(cancellationToken);

        return new CharacterResourcesData(characterId, resources);
    }

    public async Task<CharacterResourcesData> UpsertResourcesAsync(Guid characterId, UpsertCharacterResourcesRequest request, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        var current = await _db.CharacterResourcePools.Where(x => x.CharacterId == id).ToListAsync(cancellationToken);
        _db.CharacterResourcePools.RemoveRange(current);
        foreach (var resource in request.Resources
                     .Where(x => !string.IsNullOrWhiteSpace(x.ResourceKey))
                     .DistinctBy(x => x.ResourceKey, StringComparer.OrdinalIgnoreCase))
        {
            _db.CharacterResourcePools.Add(new CharacterResourcePoolEntity
            {
                CharacterId = id,
                ResourceKey = resource.ResourceKey.Trim(),
                CurrentValue = resource.CurrentValue,
                MaxValue = resource.MaxValue,
                MetadataJson = string.IsNullOrWhiteSpace(resource.MetadataJson) ? "{}" : resource.MetadataJson,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await GetResourcesAsync(characterId, cancellationToken) ?? new CharacterResourcesData(characterId, Array.Empty<CharacterResourcePoolData>());
    }

    public async Task<CharacterVitalsData?> GetVitalsAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        var row = await _db.CharacterVitals.AsNoTracking().SingleOrDefaultAsync(x => x.CharacterId == id, cancellationToken);
        if (row is null)
        {
            return null;
        }

        return new CharacterVitalsData(
            characterId,
            row.MaxHitPoints,
            row.CurrentHitPoints,
            row.TempHitPoints,
            row.BaseMoveSpeed,
            row.BaseArmorClass,
            row.UpdatedAtUtc);
    }

    public async Task<CharacterVitalsData> UpsertVitalsAsync(Guid characterId, UpsertCharacterVitalsRequest request, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        var row = await _db.CharacterVitals.SingleOrDefaultAsync(x => x.CharacterId == id, cancellationToken);
        if (row is null)
        {
            row = new CharacterVitalsEntity
            {
                CharacterId = id,
            };
            _db.CharacterVitals.Add(row);
        }

        row.MaxHitPoints = Math.Max(0, request.MaxHitPoints);
        row.CurrentHitPoints = Math.Max(0, request.CurrentHitPoints);
        row.TempHitPoints = Math.Max(0, request.TempHitPoints);
        row.BaseMoveSpeed = Math.Max(0, request.BaseMoveSpeed);
        row.BaseArmorClass = Math.Max(0, request.BaseArmorClass);
        row.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return new CharacterVitalsData(
            characterId,
            row.MaxHitPoints,
            row.CurrentHitPoints,
            row.TempHitPoints,
            row.BaseMoveSpeed,
            row.BaseArmorClass,
            row.UpdatedAtUtc);
    }

    private static string NormalizePreparationMode(string value)
    {
        if (value.Equals("Cantrip", StringComparison.OrdinalIgnoreCase))
        {
            return "Cantrip";
        }
        if (value.Equals("Prepared", StringComparison.OrdinalIgnoreCase))
        {
            return "Prepared";
        }
        return "Known";
    }
}
