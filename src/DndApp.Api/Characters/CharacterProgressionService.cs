using DndApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DndApp.Api.Characters;

public interface ICharacterProgressionService
{
    Task<CharacterSpellsData?> GetSpellsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<CharacterSpellsData> UpsertSpellsAsync(Guid characterId, UpsertCharacterSpellsRequest request, CancellationToken cancellationToken);
    Task<RecommendedSpellsResult?> GetRecommendedSpellsAsync(Guid characterId, string classModuleId, int classLevel, CancellationToken cancellationToken);
    Task<CharacterCurrencyData?> GetCurrencyAsync(Guid characterId, CancellationToken cancellationToken);
    Task<CharacterCurrencyData> UpsertCurrencyAsync(Guid characterId, UpsertCharacterCurrencyRequest request, CancellationToken cancellationToken);
    Task<(CharacterCurrencyData? Data, IReadOnlyList<string> Errors)> ConvertCurrencyAsync(Guid characterId, ConvertCurrencyRequest request, CancellationToken cancellationToken);
    Task<CharacterCurrencyData> ConsolidateCurrencyAsync(Guid characterId, ConsolidateCurrencyRequest request, CancellationToken cancellationToken);
    Task<(CharacterCurrencyData? Data, IReadOnlyList<string> Errors)> PurchaseFromCurrencyAsync(Guid characterId, PurchaseFromCurrencyRequest request, CancellationToken cancellationToken);
    Task<CharacterResourcesData?> GetResourcesAsync(Guid characterId, CancellationToken cancellationToken);
    Task<CharacterResourcesData> UpsertResourcesAsync(Guid characterId, UpsertCharacterResourcesRequest request, CancellationToken cancellationToken);
    Task<CharacterVitalsData?> GetVitalsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<CharacterVitalsData> UpsertVitalsAsync(Guid characterId, UpsertCharacterVitalsRequest request, CancellationToken cancellationToken);
}

public sealed class CharacterProgressionService : ICharacterProgressionService
{
    private static readonly Dictionary<string, int> CurrencyValuesInCp = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cp"] = 1,
        ["sp"] = 10,
        ["ep"] = 50,
        ["gp"] = 100,
        ["pp"] = 1000,
    };

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

    public async Task<CharacterCurrencyData?> GetCurrencyAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        var exists = await _db.CharacterSheets.AsNoTracking().AnyAsync(x => x.CharacterId == id, cancellationToken);
        if (!exists)
        {
            return null;
        }

        var resources = await _db.CharacterResourcePools.AsNoTracking()
            .Where(x => x.CharacterId == id && (x.ResourceKey == "cp" || x.ResourceKey == "sp" || x.ResourceKey == "ep" || x.ResourceKey == "gp" || x.ResourceKey == "pp"))
            .ToDictionaryAsync(x => x.ResourceKey, x => x.CurrentValue, StringComparer.OrdinalIgnoreCase, cancellationToken);

        return new CharacterCurrencyData(
            characterId,
            resources.TryGetValue("cp", out var cp) ? cp : 0,
            resources.TryGetValue("sp", out var sp) ? sp : 0,
            resources.TryGetValue("ep", out var ep) ? ep : 0,
            resources.TryGetValue("gp", out var gp) ? gp : 0,
            resources.TryGetValue("pp", out var pp) ? pp : 0);
    }

    public async Task<CharacterCurrencyData> UpsertCurrencyAsync(Guid characterId, UpsertCharacterCurrencyRequest request, CancellationToken cancellationToken)
    {
        var id = characterId.ToString();
        var rows = await _db.CharacterResourcePools
            .Where(x => x.CharacterId == id && (x.ResourceKey == "cp" || x.ResourceKey == "sp" || x.ResourceKey == "ep" || x.ResourceKey == "gp" || x.ResourceKey == "pp"))
            .ToListAsync(cancellationToken);

        _db.CharacterResourcePools.RemoveRange(rows);
        _db.CharacterResourcePools.AddRange(
            CreateCurrencyRow(id, "cp", Math.Max(0, request.Cp)),
            CreateCurrencyRow(id, "sp", Math.Max(0, request.Sp)),
            CreateCurrencyRow(id, "ep", Math.Max(0, request.Ep)),
            CreateCurrencyRow(id, "gp", Math.Max(0, request.Gp)),
            CreateCurrencyRow(id, "pp", Math.Max(0, request.Pp)));

        await _db.SaveChangesAsync(cancellationToken);
        return (await GetCurrencyAsync(characterId, cancellationToken)) ?? new CharacterCurrencyData(characterId, 0, 0, 0, 0, 0);
    }

    public async Task<(CharacterCurrencyData? Data, IReadOnlyList<string> Errors)> ConvertCurrencyAsync(Guid characterId, ConvertCurrencyRequest request, CancellationToken cancellationToken)
    {
        var current = await GetCurrencyAsync(characterId, cancellationToken);
        if (current is null)
        {
            return (null, new[] { "Character build was not found." });
        }

        if (!CurrencyValuesInCp.TryGetValue(request.FromDenomination.Trim(), out var fromValue) ||
            !CurrencyValuesInCp.TryGetValue(request.ToDenomination.Trim(), out var toValue))
        {
            return (null, new[] { "Currency denomination must be one of cp, sp, ep, gp, pp." });
        }
        if (request.Amount <= 0)
        {
            return (null, new[] { "Amount must be greater than 0." });
        }

        var wallet = ToWallet(current);
        var fromKey = request.FromDenomination.Trim().ToLowerInvariant();
        var toKey = request.ToDenomination.Trim().ToLowerInvariant();
        if (wallet[fromKey] < request.Amount)
        {
            return (null, new[] { $"Not enough {fromKey} to convert." });
        }

        var totalCp = request.Amount * fromValue;
        if (totalCp % toValue != 0)
        {
            return (null, new[] { $"Cannot convert {request.Amount} {fromKey} into exact {toKey} at book exchange rates." });
        }

        wallet[fromKey] -= request.Amount;
        wallet[toKey] += totalCp / toValue;
        var saved = await UpsertCurrencyAsync(characterId, new UpsertCharacterCurrencyRequest(wallet["cp"], wallet["sp"], wallet["ep"], wallet["gp"], wallet["pp"]), cancellationToken);
        return (saved, Array.Empty<string>());
    }

    public async Task<CharacterCurrencyData> ConsolidateCurrencyAsync(Guid characterId, ConsolidateCurrencyRequest request, CancellationToken cancellationToken)
    {
        var current = await GetCurrencyAsync(characterId, cancellationToken) ?? new CharacterCurrencyData(characterId, 0, 0, 0, 0, 0);
        var totalCp = ToTotalCp(current);
        var wallet = request.PreferPlatinum ? FromTotalCpPreferPlatinum(totalCp) : FromTotalCpStandard(totalCp);
        return await UpsertCurrencyAsync(characterId, new UpsertCharacterCurrencyRequest(wallet["cp"], wallet["sp"], wallet["ep"], wallet["gp"], wallet["pp"]), cancellationToken);
    }

    public async Task<(CharacterCurrencyData? Data, IReadOnlyList<string> Errors)> PurchaseFromCurrencyAsync(Guid characterId, PurchaseFromCurrencyRequest request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
        {
            return (null, new[] { "Quantity must be greater than 0." });
        }
        if (request.CostInGold < 0)
        {
            return (null, new[] { "Cost cannot be negative." });
        }

        var current = await GetCurrencyAsync(characterId, cancellationToken);
        if (current is null)
        {
            return (null, new[] { "Character build was not found." });
        }

        var costInCp = (int)Math.Round(request.CostInGold * request.Quantity * 100m, MidpointRounding.AwayFromZero);
        var totalCp = ToTotalCp(current);
        if (totalCp < costInCp)
        {
            return (null, new[] { "Insufficient funds for purchase." });
        }

        var remainingCp = totalCp - costInCp;
        var consolidated = FromTotalCpStandard(remainingCp);
        var saved = await UpsertCurrencyAsync(characterId, new UpsertCharacterCurrencyRequest(consolidated["cp"], consolidated["sp"], consolidated["ep"], consolidated["gp"], consolidated["pp"]), cancellationToken);
        return (saved, Array.Empty<string>());
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

    private static CharacterResourcePoolEntity CreateCurrencyRow(string characterId, string key, int amount)
    {
        return new CharacterResourcePoolEntity
        {
            CharacterId = characterId,
            ResourceKey = key,
            CurrentValue = amount,
            MaxValue = amount,
            MetadataJson = "{}",
        };
    }

    private static int ToTotalCp(CharacterCurrencyData data)
    {
        return data.Cp + (data.Sp * 10) + (data.Ep * 50) + (data.Gp * 100) + (data.Pp * 1000);
    }

    private static Dictionary<string, int> ToWallet(CharacterCurrencyData data)
    {
        return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["cp"] = data.Cp,
            ["sp"] = data.Sp,
            ["ep"] = data.Ep,
            ["gp"] = data.Gp,
            ["pp"] = data.Pp,
        };
    }

    private static Dictionary<string, int> FromTotalCpStandard(int totalCp)
    {
        var remaining = Math.Max(0, totalCp);
        var pp = remaining / 1000;
        remaining %= 1000;
        var gp = remaining / 100;
        remaining %= 100;
        var ep = remaining / 50;
        remaining %= 50;
        var sp = remaining / 10;
        var cp = remaining % 10;
        return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["cp"] = cp,
            ["sp"] = sp,
            ["ep"] = ep,
            ["gp"] = gp,
            ["pp"] = pp,
        };
    }

    private static Dictionary<string, int> FromTotalCpPreferPlatinum(int totalCp)
    {
        // Prefer larger platinum stacks, then greedily resolve remaining value.
        return FromTotalCpStandard(totalCp);
    }
}
