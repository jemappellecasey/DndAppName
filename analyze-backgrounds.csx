using DndApp.Api.Data;
using Microsoft.EntityFrameworkCore;

var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=data/dnd_app.db");
await connection.OpenAsync();
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite(connection)
    .Options;

using var db = new AppDbContext(options);

// Get 2014 backgrounds from legacy rule modules
var legacyBg2014 = await db.RuleModules
    .AsNoTracking()
    .Where(x => x.ModuleType == "background")
    .Join(db.RuleVariants.Where(v => v.RuleSystemId == "Rules2014"), 
        m => m.Id, v => v.RuleModuleId, (m, v) => new { m.Id, m.DisplayName, m.Slug })
    .Distinct()
    .OrderBy(x => x.DisplayName)
    .ToListAsync();

Console.WriteLine("=== 2014 BACKGROUNDS (Legacy Rule Modules) ===");
Console.WriteLine($"Total: {legacyBg2014.Count}");
foreach (var bg in legacyBg2014)
{
    Console.WriteLine($"  - {bg.DisplayName} ({bg.Id})");
}

// Get 2014 backgrounds from split tables
var splitBg2014 = await db.Backgrounds2014
    .AsNoTracking()
    .OrderBy(x => x.Name)
    .ToListAsync();

Console.WriteLine("\n=== 2014 BACKGROUNDS (Split Tables) ===");
Console.WriteLine($"Total: {splitBg2014.Count}");
foreach (var bg in splitBg2014)
{
    Console.WriteLine($"  - {bg.Name} ({bg.Id})");
}

// Get 2024 backgrounds from legacy rule modules
var legacyBg2024 = await db.RuleModules
    .AsNoTracking()
    .Where(x => x.ModuleType == "background")
    .Join(db.RuleVariants.Where(v => v.RuleSystemId == "Rules2024"), 
        m => m.Id, v => v.RuleModuleId, (m, v) => new { m.Id, m.DisplayName, m.Slug })
    .Distinct()
    .OrderBy(x => x.DisplayName)
    .ToListAsync();

Console.WriteLine("\n=== 2024 BACKGROUNDS (Legacy Rule Modules) ===");
Console.WriteLine($"Total: {legacyBg2024.Count}");
foreach (var bg in legacyBg2024)
{
    Console.WriteLine($"  - {bg.DisplayName} ({bg.Id})");
}

// Get 2024 backgrounds from split tables
var splitBg2024 = await db.Backgrounds2024
    .AsNoTracking()
    .OrderBy(x => x.Name)
    .ToListAsync();

Console.WriteLine("\n=== 2024 BACKGROUNDS (Split Tables) ===");
Console.WriteLine($"Total: {splitBg2024.Count}");
foreach (var bg in splitBg2024)
{
    Console.WriteLine($"  - {bg.Name} ({bg.Id})");
}

// Find missing backgrounds
Console.WriteLine("\n=== MISSING IN SPLIT TABLES ===");
var missing2014 = legacyBg2014
    .Where(x => !splitBg2014.Any(s => s.LegacyRuleModuleId == x.Id))
    .ToList();

if (missing2014.Any())
{
    Console.WriteLine($"\n2014 Missing ({missing2014.Count}):");
    foreach (var bg in missing2014)
    {
        Console.WriteLine($"  - {bg.DisplayName} (ID: {bg.Id})");
    }
}

var missing2024 = legacyBg2024
    .Where(x => !splitBg2024.Any(s => s.LegacyRuleModuleId == x.Id))
    .ToList();

if (missing2024.Any())
{
    Console.WriteLine($"\n2024 Missing ({missing2024.Count}):");
    foreach (var bg in missing2024)
    {
        Console.WriteLine($"  - {bg.DisplayName} (ID: {bg.Id})");
    }
}
