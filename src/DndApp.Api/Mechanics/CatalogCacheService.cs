using DndApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DndApp.Api.Mechanics;

public interface ICatalogCacheService
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ClassCatalogEntry>> GetClassesAsync(string ruleSystem, CancellationToken cancellationToken);
    Task<IReadOnlyList<SpellCatalogEntry>> GetSpellsAsync(string ruleSystem, CancellationToken cancellationToken);
    Task<IReadOnlyList<ItemCatalogEntry>> GetItemsAsync(string ruleSystem, CancellationToken cancellationToken);
    Task<IReadOnlyList<BackgroundCatalogEntry>> GetBackgroundsAsync(string ruleSystem, CancellationToken cancellationToken);
    Task InvalidateClassCacheAsync();
    Task InvalidateSpellCacheAsync();
    Task InvalidateItemCacheAsync();
    Task InvalidateBackgroundCacheAsync();
}

public sealed class CatalogCacheService : ICatalogCacheService
{
    private readonly IServiceProvider _serviceProvider;
    private volatile Dictionary<string, IReadOnlyList<ClassCatalogEntry>>? _classCacheBySystem;
    private volatile Dictionary<string, IReadOnlyList<SpellCatalogEntry>>? _spellCacheBySystem;
    private volatile Dictionary<string, IReadOnlyList<ItemCatalogEntry>>? _itemCacheBySystem;
    private volatile Dictionary<string, IReadOnlyList<BackgroundCatalogEntry>>? _backgroundCacheBySystem;

    public CatalogCacheService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(
            LoadClassCacheAsync(cancellationToken),
            LoadSpellCacheAsync(cancellationToken),
            LoadItemCacheAsync(cancellationToken),
            LoadBackgroundCacheAsync(cancellationToken));
    }

    public async Task<IReadOnlyList<ClassCatalogEntry>> GetClassesAsync(string ruleSystem, CancellationToken cancellationToken)
    {
        if (_classCacheBySystem is null)
        {
            await LoadClassCacheAsync(cancellationToken);
        }

        var system = ruleSystem.Trim().ToLowerInvariant();
        return _classCacheBySystem!.TryGetValue(system, out var classes)
            ? classes
            : Array.Empty<ClassCatalogEntry>();
    }

    public async Task<IReadOnlyList<SpellCatalogEntry>> GetSpellsAsync(string ruleSystem, CancellationToken cancellationToken)
    {
        if (_spellCacheBySystem is null)
        {
            await LoadSpellCacheAsync(cancellationToken);
        }

        var system = ruleSystem.Trim().ToLowerInvariant();
        return _spellCacheBySystem!.TryGetValue(system, out var spells)
            ? spells
            : Array.Empty<SpellCatalogEntry>();
    }

    public async Task<IReadOnlyList<ItemCatalogEntry>> GetItemsAsync(string ruleSystem, CancellationToken cancellationToken)
    {
        if (_itemCacheBySystem is null)
        {
            await LoadItemCacheAsync(cancellationToken);
        }

        var system = ruleSystem.Trim().ToLowerInvariant();
        return _itemCacheBySystem!.TryGetValue(system, out var items)
            ? items
            : Array.Empty<ItemCatalogEntry>();
    }

    public async Task<IReadOnlyList<BackgroundCatalogEntry>> GetBackgroundsAsync(string ruleSystem, CancellationToken cancellationToken)
    {
        if (_backgroundCacheBySystem is null)
        {
            await LoadBackgroundCacheAsync(cancellationToken);
        }

        var system = ruleSystem.Trim().ToLowerInvariant();
        return _backgroundCacheBySystem!.TryGetValue(system, out var backgrounds)
            ? backgrounds
            : Array.Empty<BackgroundCatalogEntry>();
    }

    public async Task InvalidateClassCacheAsync()
    {
        _classCacheBySystem = null;
        await Task.CompletedTask;
    }

    public async Task InvalidateSpellCacheAsync()
    {
        _spellCacheBySystem = null;
        await Task.CompletedTask;
    }

    public async Task InvalidateItemCacheAsync()
    {
        _itemCacheBySystem = null;
        await Task.CompletedTask;
    }

    public async Task InvalidateBackgroundCacheAsync()
    {
        _backgroundCacheBySystem = null;
        await Task.CompletedTask;
    }

    private async Task LoadClassCacheAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var classes2014 = await (
            from c in db.Classes2014
            join source in db.ContentSources on c.ContentSourceId equals source.Id
            select new ClassCatalogEntry(c.Id, c.Name, source.Code, "2014"))
            .ToListAsync(cancellationToken);

        var classes2024 = await (
            from c in db.Classes2024
            join source in db.ContentSources on c.ContentSourceId equals source.Id
            select new ClassCatalogEntry(c.Id, c.Name, source.Code, "2024"))
            .ToListAsync(cancellationToken);

        _classCacheBySystem = new Dictionary<string, IReadOnlyList<ClassCatalogEntry>>(StringComparer.OrdinalIgnoreCase)
        {
            ["rules-2014"] = classes2014.AsReadOnly(),
            ["rules-2024"] = classes2024.AsReadOnly(),
        };
    }

    private async Task LoadSpellCacheAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var spells2014 = await (
            from s in db.Spells
            where s.EditionYear == 2014
            join source in db.ContentSources on s.ContentSourceId equals source.Id
            select new SpellCatalogEntry(s.Id, s.Name, s.Level, s.School, source.Code, "2014"))
            .ToListAsync(cancellationToken);

        var spells2024 = await (
            from s in db.Spells
            where s.EditionYear == 2024
            join source in db.ContentSources on s.ContentSourceId equals source.Id
            select new SpellCatalogEntry(s.Id, s.Name, s.Level, s.School, source.Code, "2024"))
            .ToListAsync(cancellationToken);

        _spellCacheBySystem = new Dictionary<string, IReadOnlyList<SpellCatalogEntry>>(StringComparer.OrdinalIgnoreCase)
        {
            ["rules-2014"] = spells2014.AsReadOnly(),
            ["rules-2024"] = spells2024.AsReadOnly(),
        };
    }

    private async Task LoadItemCacheAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var items2014 = await (
            from i in db.Items2014
            join source in db.ContentSources on i.ContentSourceId equals source.Id
            select new ItemCatalogEntry(i.Id, i.Name, i.ItemType, source.Code, "2014"))
            .ToListAsync(cancellationToken);

        var items2024 = await (
            from i in db.Items2024
            join source in db.ContentSources on i.ContentSourceId equals source.Id
            select new ItemCatalogEntry(i.Id, i.Name, i.ItemType, source.Code, "2024"))
            .ToListAsync(cancellationToken);

        _itemCacheBySystem = new Dictionary<string, IReadOnlyList<ItemCatalogEntry>>(StringComparer.OrdinalIgnoreCase)
        {
            ["rules-2014"] = items2014.AsReadOnly(),
            ["rules-2024"] = items2024.AsReadOnly(),
        };
    }

    private async Task LoadBackgroundCacheAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var backgrounds2014 = await (
            from b in db.Backgrounds2014
            join source in db.ContentSources on b.ContentSourceId equals source.Id
            select new BackgroundCatalogEntry(b.Id, b.Name, source.Code, "2014"))
            .ToListAsync(cancellationToken);

        var backgrounds2024 = await (
            from b in db.Backgrounds2024
            join source in db.ContentSources on b.ContentSourceId equals source.Id
            select new BackgroundCatalogEntry(b.Id, b.Name, source.Code, "2024"))
            .ToListAsync(cancellationToken);

        _backgroundCacheBySystem = new Dictionary<string, IReadOnlyList<BackgroundCatalogEntry>>(StringComparer.OrdinalIgnoreCase)
        {
            ["rules-2014"] = backgrounds2014.AsReadOnly(),
            ["rules-2024"] = backgrounds2024.AsReadOnly(),
        };
    }
}

public sealed record ClassCatalogEntry(string Id, string Name, string SourceCode, string Edition);
public sealed record SpellCatalogEntry(string Id, string Name, int Level, string School, string SourceCode, string Edition);
public sealed record ItemCatalogEntry(string Id, string Name, string ItemType, string SourceCode, string Edition);
public sealed record BackgroundCatalogEntry(string Id, string Name, string SourceCode, string Edition);
