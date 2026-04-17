using DndApp.Api.Characters;
using DndApp.Api.Data;
using DndApp.Api.Items;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DndApp.Api.Tests;

public sealed class CharacterInventoryServiceTests
{
    [Fact]
    public async Task AddItem_AddsPersistedInventoryRow()
    {
        await using var fixture = await CreateFixtureAsync();
        var characterId = Guid.NewGuid();
        SeedCharacterBuild(fixture.Db, characterId);
        SeedItemDefinition(fixture.Db, "item-cloak", true, "Cloak");
        await fixture.Db.SaveChangesAsync();
        var service = new CharacterInventoryService(fixture.Db, new ItemEffectPipelineService());

        var added = await service.AddItemAsync(characterId, new AddInventoryItemRequest("item-cloak"), CancellationToken.None);

        Assert.Empty(added.Errors);
        Assert.NotNull(added.State);
        Assert.Single(added.State.Items);
        Assert.Equal("item-cloak", added.State.Items[0].ItemDefinitionId);
        Assert.False(added.State.Items[0].IsEquipped);
    }

    [Fact]
    public async Task UpdateItemState_EnforcesAttunementCap()
    {
        await using var fixture = await CreateFixtureAsync();
        var characterId = Guid.NewGuid();
        SeedCharacterBuild(fixture.Db, characterId);
        SeedItemDefinition(fixture.Db, "item-1", true, "Item 1");
        SeedItemDefinition(fixture.Db, "item-2", true, "Item 2");
        SeedItemDefinition(fixture.Db, "item-3", true, "Item 3");
        SeedItemDefinition(fixture.Db, "item-4", true, "Item 4");
        await fixture.Db.SaveChangesAsync();
        var service = new CharacterInventoryService(fixture.Db, new ItemEffectPipelineService());

        var i1 = (await service.AddItemAsync(characterId, new AddInventoryItemRequest("item-1"), CancellationToken.None)).State!.Items[0];
        var i2 = (await service.AddItemAsync(characterId, new AddInventoryItemRequest("item-2"), CancellationToken.None)).State!.Items[1];
        var i3 = (await service.AddItemAsync(characterId, new AddInventoryItemRequest("item-3"), CancellationToken.None)).State!.Items[2];
        var i4 = (await service.AddItemAsync(characterId, new AddInventoryItemRequest("item-4"), CancellationToken.None)).State!.Items[3];

        await service.UpdateItemStateAsync(characterId, i1.InventoryItemId, new PatchInventoryItemStateRequest(true, true), CancellationToken.None);
        await service.UpdateItemStateAsync(characterId, i2.InventoryItemId, new PatchInventoryItemStateRequest(true, true), CancellationToken.None);
        await service.UpdateItemStateAsync(characterId, i3.InventoryItemId, new PatchInventoryItemStateRequest(true, true), CancellationToken.None);
        var fourth = await service.UpdateItemStateAsync(characterId, i4.InventoryItemId, new PatchInventoryItemStateRequest(true, true), CancellationToken.None);

        Assert.NotNull(fourth.State);
        Assert.Equal(3, fourth.State.ActiveAttunementCount);
        Assert.Contains(fourth.State.Errors, x => x.Contains("Attunement cap", StringComparison.OrdinalIgnoreCase));
        var fourthItem = fourth.State.Items.Single(x => x.InventoryItemId == i4.InventoryItemId);
        Assert.False(fourthItem.IsAttuned);
    }

    private static void SeedCharacterBuild(AppDbContext db, Guid characterId)
    {
        var id = characterId.ToString();
        db.CharacterSheets.Add(new CharacterSheetEntity
        {
            CharacterId = id,
            CharacterName = "Inventory Tester",
            BaseRuleSystem = "Rules2024",
            BuildMethod = "Manual",
            ClassModuleId = "class-fighter",
            ClassName = "Fighter",
            Level = 1,
            ProficiencyBonus = 2,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        db.CharacterAbilityScores.AddRange(new[]
        {
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Strength", Score = 10 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Dexterity", Score = 10 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Constitution", Score = 10 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Intelligence", Score = 10 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Wisdom", Score = 10 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Charisma", Score = 10 },
        });
    }

    private static void SeedItemDefinition(AppDbContext db, string itemId, bool requiresAttunement, string moduleDisplayName)
    {
        var moduleId = $"module-{itemId}";
        db.RuleModules.Add(new RuleModuleEntity
        {
            Id = moduleId,
            ContentSourceId = "content-source-test",
            ModuleType = "item",
            Slug = itemId,
            DisplayName = moduleDisplayName,
            VersionTag = "v1",
        });
        db.ItemDefinitions.Add(new ItemDefinitionEntity
        {
            Id = itemId,
            RuleModuleId = moduleId,
            ItemType = "WondrousItem",
            Rarity = "Rare",
            RequiresAttunement = requiresAttunement,
            ChargesModelJson = "{}",
        });
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
        db.RuleSystems.Add(new RuleSystemEntity
        {
            Id = "rules-2024",
            Name = "Rules 2024",
        });
        db.ContentSources.Add(new ContentSourceEntity
        {
            Id = "content-source-test",
            RuleSystemId = "rules-2024",
            Code = "TEST",
            Name = "Test Source",
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
