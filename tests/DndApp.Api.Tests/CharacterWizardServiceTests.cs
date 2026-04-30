using DndApp.Api.Characters;
using DndApp.Api.Data;
using DndApp.Api.Mechanics;
using DndApp.Api.MixedRules;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DndApp.Api.Tests;

public sealed class CharacterWizardServiceTests
{
    [Fact]
    public async Task StartDraft_AllowsEmptyName_UsesDefault()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new CharacterWizardService(fixture.Db, new MixedRulesResolutionService(), new ExperienceService());

        var result = await service.StartDraftAsync(
            new StartCharacterWizardRequest(
                SessionToken: "local:user-1",
                CharacterName: null,
                RulesProfile: new RulesProfile(RuleSystemMode.Rules2024, MixedModeEnabled: false, OverlaySources: Array.Empty<string>())),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Draft);
        Assert.Equal("New Adventurer", result.Draft.CharacterName);
    }

    [Fact]
    public async Task ListCharacters_SupportsActiveAndArchivedFilters()
    {
        await using var fixture = await CreateFixtureAsync();
        var now = DateTimeOffset.UtcNow;
        fixture.Db.CharacterRecords.AddRange(
            new CharacterRecordEntity
            {
                CharacterId = Guid.NewGuid().ToString(),
                OwnerUserId = "local:user-1",
                CharacterName = "Active One",
                BaseRuleSystem = RuleSystemMode.Rules2024.ToString(),
                MixedModeEnabled = false,
                IsArchived = false,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            },
            new CharacterRecordEntity
            {
                CharacterId = Guid.NewGuid().ToString(),
                OwnerUserId = "local:user-1",
                CharacterName = "Archived One",
                BaseRuleSystem = RuleSystemMode.Rules2024.ToString(),
                MixedModeEnabled = false,
                IsArchived = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now.AddMinutes(1),
            });
        await fixture.Db.SaveChangesAsync();

        var service = new CharacterWizardService(fixture.Db, new MixedRulesResolutionService(), new ExperienceService());
        var activeOnly = await service.ListCharactersAsync(includeArchived: false, archivedOnly: false, ownerUserId: "local:user-1", CancellationToken.None);
        var archivedOnly = await service.ListCharactersAsync(includeArchived: true, archivedOnly: true, ownerUserId: "local:user-1", CancellationToken.None);

        Assert.Single(activeOnly);
        Assert.Equal("Active One", activeOnly[0].CharacterName);
        Assert.Single(archivedOnly);
        Assert.Equal("Archived One", archivedOnly[0].CharacterName);
    }

    [Fact]
    public async Task RestoreAndDeleteCharacter_UpdatesAndRemovesCharacterData()
    {
        await using var fixture = await CreateFixtureAsync();
        var characterId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        fixture.Db.CharacterRecords.Add(new CharacterRecordEntity
        {
            CharacterId = characterId.ToString(),
            OwnerUserId = "local:user-1",
            CharacterName = "Archived Character",
            BaseRuleSystem = RuleSystemMode.Rules2024.ToString(),
            MixedModeEnabled = false,
            IsArchived = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
        fixture.Db.CharacterDrafts.Add(new CharacterDraftEntity
        {
            CharacterId = characterId.ToString(),
            OwnerUserId = "local:user-1",
            CharacterName = "Archived Character",
            BaseRuleSystem = RuleSystemMode.Rules2024.ToString(),
            MixedModeEnabled = false,
            OverlaySourcesJson = "[]",
            IsFinalized = false,
            StepsJson = "[]",
            WarningsJson = "[]",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
        fixture.Db.CharacterSheets.Add(new CharacterSheetEntity
        {
            CharacterId = characterId.ToString(),
            OwnerUserId = "local:user-1",
            CharacterName = "Archived Character",
            BaseRuleSystem = RuleSystemMode.Rules2024.ToString(),
            BuildMethod = "Manual",
            ClassModuleId = "class-fighter",
            ClassName = "Fighter",
            Level = 1,
            ProficiencyBonus = 2,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
        await fixture.Db.SaveChangesAsync();

        var service = new CharacterWizardService(fixture.Db, new MixedRulesResolutionService(), new ExperienceService());
        var restored = await service.RestoreCharacterAsync(characterId, CancellationToken.None);
        var deleted = await service.DeleteCharacterAsync(characterId, CancellationToken.None);

        Assert.NotNull(restored);
        Assert.False(restored.IsArchived);
        Assert.True(deleted);
        Assert.False(await fixture.Db.CharacterRecords.AnyAsync(x => x.CharacterId == characterId.ToString()));
        Assert.False(await fixture.Db.CharacterDrafts.AnyAsync(x => x.CharacterId == characterId.ToString()));
        Assert.False(await fixture.Db.CharacterSheets.AnyAsync(x => x.CharacterId == characterId.ToString()));
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
