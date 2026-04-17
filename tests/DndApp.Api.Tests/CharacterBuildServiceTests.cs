using DndApp.Api.Characters;
using DndApp.Api.Data;
using DndApp.Api.MixedRules;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DndApp.Api.Tests;

public sealed class CharacterBuildServiceTests
{
    [Fact]
    public async Task UpsertBuild_PersistsAndReadsBack()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new CharacterBuildService(fixture.Db, new RuleValidationService(fixture.Db));
        var characterId = Guid.NewGuid();

        var upsert = await service.UpsertBuildAsync(
            characterId,
            new UpsertCharacterBuildRequest(
                CharacterName: "Tessa",
                BaseRuleSystem: RuleSystemMode.Rules2024,
                BuildMethod: CharacterBuildMethod.PointBuy,
                ClassModuleId: "class-fighter",
                ClassName: "Fighter",
                Level: 3,
                ProficiencyBonus: 2,
                AbilityScores: new Dictionary<string, int>
                {
                    ["Strength"] = 16,
                    ["Dexterity"] = 12,
                    ["Constitution"] = 14,
                    ["Intelligence"] = 10,
                    ["Wisdom"] = 10,
                    ["Charisma"] = 8,
                },
                ProficientSkills: new[] { "Athletics", "Perception" }),
            CancellationToken.None);

        Assert.Empty(upsert.Errors);
        Assert.NotNull(upsert.Data);

        var fromDb = await service.GetBuildAsync(characterId, CancellationToken.None);
        Assert.NotNull(fromDb);
        Assert.Equal("Tessa", fromDb.CharacterName);
        Assert.Equal("class-fighter", fromDb.ClassModuleId);
        Assert.Equal(16, fromDb.AbilityScores["Strength"]);
        Assert.Contains("Athletics", fromDb.ProficientSkills);
    }

    [Fact]
    public async Task PatchBuild_UpdatesSubset()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new CharacterBuildService(fixture.Db, new RuleValidationService(fixture.Db));
        var characterId = Guid.NewGuid();

        await service.UpsertBuildAsync(
            characterId,
            new UpsertCharacterBuildRequest(
                CharacterName: "Miri",
                BaseRuleSystem: RuleSystemMode.Rules2014,
                BuildMethod: CharacterBuildMethod.Manual,
                ClassModuleId: "class-rogue",
                ClassName: "Rogue",
                Level: 1,
                ProficiencyBonus: 2,
                AbilityScores: new Dictionary<string, int>
                {
                    ["Strength"] = 8,
                    ["Dexterity"] = 16,
                    ["Constitution"] = 12,
                    ["Intelligence"] = 14,
                    ["Wisdom"] = 10,
                    ["Charisma"] = 13,
                },
                ProficientSkills: new[] { "Stealth" }),
            CancellationToken.None);

        var patched = await service.PatchBuildAsync(
            characterId,
            new PatchCharacterBuildRequest(
                CharacterName: null,
                BaseRuleSystem: null,
                BuildMethod: null,
                ClassModuleId: null,
                ClassName: null,
                Level: 2,
                ProficiencyBonus: 2,
                AbilityScores: null,
                ProficientSkills: new[] { "Stealth", "Acrobatics" }),
            CancellationToken.None);

        Assert.Empty(patched.Errors);
        Assert.NotNull(patched.Data);
        Assert.Equal(2, patched.Data.Level);
        Assert.Contains("Acrobatics", patched.Data.ProficientSkills);
    }

    [Fact]
    public async Task UpsertBuild_RejectsIncompatibleClassSource()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new CharacterBuildService(fixture.Db, new RuleValidationService(fixture.Db));
        var characterId = Guid.NewGuid();

        var upsert = await service.UpsertBuildAsync(
            characterId,
            new UpsertCharacterBuildRequest(
                CharacterName: "Bad Source",
                BaseRuleSystem: RuleSystemMode.Rules2024,
                BuildMethod: CharacterBuildMethod.PointBuy,
                ClassModuleId: "class-rogue",
                ClassName: "Rogue",
                Level: 1,
                ProficiencyBonus: 2,
                AbilityScores: new Dictionary<string, int>
                {
                    ["Strength"] = 10,
                    ["Dexterity"] = 10,
                    ["Constitution"] = 10,
                    ["Intelligence"] = 10,
                    ["Wisdom"] = 10,
                    ["Charisma"] = 10,
                },
                ProficientSkills: Array.Empty<string>()),
            CancellationToken.None);

        Assert.NotEmpty(upsert.Errors);
        Assert.Contains(upsert.Errors, x => x.Contains("incompatible", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<DbFixture> CreateFixtureAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        db.RuleSystems.AddRange(new[]
        {
            new RuleSystemEntity { Id = "rules-2014", Name = "Rules 2014" },
            new RuleSystemEntity { Id = "rules-2024", Name = "Rules 2024" },
        });
        db.ContentSources.AddRange(new[]
        {
            new ContentSourceEntity { Id = "content-2014", RuleSystemId = "rules-2014", Code = "PHB2014", Name = "PHB 2014" },
            new ContentSourceEntity { Id = "content-2024", RuleSystemId = "rules-2024", Code = "PHB2024", Name = "PHB 2024" },
        });
        db.RuleModules.AddRange(new[]
        {
            new RuleModuleEntity
            {
                Id = "class-fighter",
                ContentSourceId = "content-2024",
                ModuleType = "class",
                Slug = "fighter",
                DisplayName = "Fighter",
                VersionTag = "v1",
            },
            new RuleModuleEntity
            {
                Id = "class-rogue",
                ContentSourceId = "content-2014",
                ModuleType = "class",
                Slug = "rogue",
                DisplayName = "Rogue",
                VersionTag = "v1",
            },
        });
        await db.SaveChangesAsync();
        return new DbFixture(connection, db);
    }

    private sealed class DbFixture : IAsyncDisposable
    {
        public DbFixture(SqliteConnection connection, AppDbContext db)
        {
            Connection = connection;
            Db = db;
        }

        public SqliteConnection Connection { get; }
        public AppDbContext Db { get; }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
