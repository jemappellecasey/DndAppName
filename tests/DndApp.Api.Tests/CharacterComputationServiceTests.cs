using DndApp.Api.Characters;
using DndApp.Api.Data;
using DndApp.Api.Items;
using DndApp.Api.Mechanics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DndApp.Api.Tests;

public sealed class CharacterComputationServiceTests
{
    [Fact]
    public async Task ComputeCheck_UsesPersistedBuildAndInventoryBonuses()
    {
        await using var fixture = await CreateFixtureAsync();
        var characterId = Guid.NewGuid();
        var id = characterId.ToString();

        fixture.Db.CharacterSheets.Add(new CharacterSheetEntity
        {
            CharacterId = id,
            CharacterName = "Calc Tester",
            BaseRuleSystem = "Rules2024",
            BuildMethod = "Manual",
            ClassModuleId = "class-rogue",
            ClassName = "Rogue",
            Level = 3,
            ProficiencyBonus = 2,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        fixture.Db.CharacterAbilityScores.AddRange(new[]
        {
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Strength", Score = 10 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Dexterity", Score = 16 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Constitution", Score = 10 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Intelligence", Score = 10 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Wisdom", Score = 10 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Charisma", Score = 10 },
        });
        fixture.Db.CharacterSkillProficiencies.Add(new CharacterSkillProficiencyEntity
        {
            CharacterId = id,
            SkillName = "Stealth",
            TrainingLevel = "Proficient",
        });

        fixture.Db.RuleSystems.Add(new RuleSystemEntity { Id = "rules-2024", Name = "Rules 2024" });
        fixture.Db.ContentSources.Add(new ContentSourceEntity
        {
            Id = "content-source-test",
            RuleSystemId = "rules-2024",
            Code = "TEST",
            Name = "Test Source",
        });
        fixture.Db.RuleModules.Add(new RuleModuleEntity
        {
            Id = "module-item-1",
            ContentSourceId = "content-source-test",
            ModuleType = "item",
            Slug = "gloves",
            DisplayName = "Gloves of Sneaking",
            VersionTag = "v1",
        });
        fixture.Db.ItemDefinitions.Add(new ItemDefinitionEntity
        {
            Id = "item-1",
            RuleModuleId = "module-item-1",
            ItemType = "WondrousItem",
            Rarity = "Rare",
            RequiresAttunement = false,
            ChargesModelJson = "{}",
        });
        fixture.Db.ItemEffects.Add(new ItemEffectEntity
        {
            Id = "effect-1",
            ItemDefinitionId = "item-1",
            EffectType = "AbilityCheckBonus",
            EffectPayloadJson = "{\"target\":\"Stealth\",\"numericValue\":1,\"description\":\"+1 stealth\"}",
            ConditionJson = "{}",
        });
        fixture.Db.CharacterInventoryItems.Add(new CharacterInventoryItemEntity
        {
            InventoryItemId = "inv-1",
            CharacterId = id,
            ItemDefinitionId = "item-1",
            ItemName = "Gloves of Sneaking",
            RequiresAttunement = false,
            IsEquipped = true,
            IsAttuned = false,
            AddedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });

        await fixture.Db.SaveChangesAsync();

        var service = new CharacterComputationService(
            fixture.Db,
            new ItemEffectPipelineService(),
            new CalculationEngineService());
        var computed = await service.ComputeCheckAsync(
            characterId,
            new PersistedComputeCheckRequest(
                SkillName: "Stealth",
                AdvantageState: AdvantageState.None,
                RollDice: false,
                AdditionalModifier: 0,
                HasExpertise: false),
            CancellationToken.None);

        Assert.Empty(computed.Errors);
        Assert.NotNull(computed.Result);
        Assert.Equal("Stealth", computed.Result.SkillName);
        Assert.Equal(6, computed.Result.TotalModifier);
    }

    [Fact]
    public async Task ComputeCheck_UsesPersistedExpertiseWhenTrainingLevelIsExpertise()
    {
        await using var fixture = await CreateFixtureAsync();
        var characterId = Guid.NewGuid();
        var id = characterId.ToString();

        fixture.Db.CharacterSheets.Add(new CharacterSheetEntity
        {
            CharacterId = id,
            CharacterName = "Expert Tester",
            BaseRuleSystem = "Rules2024",
            BuildMethod = "Manual",
            ClassModuleId = "class-rogue",
            ClassName = "Rogue",
            Level = 3,
            ProficiencyBonus = 2,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        fixture.Db.CharacterAbilityScores.AddRange(new[]
        {
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Strength", Score = 10 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Dexterity", Score = 16 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Constitution", Score = 10 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Intelligence", Score = 10 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Wisdom", Score = 10 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Charisma", Score = 10 },
        });
        fixture.Db.CharacterSkillProficiencies.Add(new CharacterSkillProficiencyEntity
        {
            CharacterId = id,
            SkillName = "Stealth",
            TrainingLevel = "Expertise",
        });
        await fixture.Db.SaveChangesAsync();

        var service = new CharacterComputationService(
            fixture.Db,
            new ItemEffectPipelineService(),
            new CalculationEngineService());
        var computed = await service.ComputeCheckAsync(
            characterId,
            new PersistedComputeCheckRequest(
                SkillName: "Stealth",
                AdvantageState: AdvantageState.None,
                RollDice: false,
                AdditionalModifier: 0,
                HasExpertise: false),
            CancellationToken.None);

        Assert.Empty(computed.Errors);
        Assert.NotNull(computed.Result);
        Assert.Equal(7, computed.Result.TotalModifier);
    }

    [Fact]
    public async Task ComputeSave_DerivesProficiencyFromPersistedClassWhenRequestOmitsOverride()
    {
        await using var fixture = await CreateFixtureAsync();
        var characterId = Guid.NewGuid();
        var id = characterId.ToString();

        fixture.Db.CharacterSheets.Add(new CharacterSheetEntity
        {
            CharacterId = id,
            CharacterName = "Save Tester",
            BaseRuleSystem = "Rules2024",
            BuildMethod = "Manual",
            ClassModuleId = "class-rogue",
            ClassName = "Rogue",
            Level = 3,
            ProficiencyBonus = 2,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        fixture.Db.CharacterAbilityScores.AddRange(new[]
        {
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Strength", Score = 10 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Dexterity", Score = 16 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Constitution", Score = 10 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Intelligence", Score = 14 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Wisdom", Score = 10 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Charisma", Score = 10 },
        });
        await fixture.Db.SaveChangesAsync();

        var service = new CharacterComputationService(
            fixture.Db,
            new ItemEffectPipelineService(),
            new CalculationEngineService());
        var computed = await service.ComputeSaveAsync(
            characterId,
            new PersistedComputeSaveRequest(
                AbilityName: "Dexterity",
                AdvantageState: AdvantageState.None,
                RollDice: false,
                AdditionalModifier: 0,
                IsProficient: null),
            CancellationToken.None);

        Assert.Empty(computed.Errors);
        Assert.NotNull(computed.Result);
        Assert.Equal(5, computed.Result.TotalModifier);
    }

    [Fact]
    public async Task GetDerivedStats_UsesPersistedVitalsAndSpells()
    {
        await using var fixture = await CreateFixtureAsync();
        var characterId = Guid.NewGuid();
        var id = characterId.ToString();

        fixture.Db.CharacterSheets.Add(new CharacterSheetEntity
        {
            CharacterId = id,
            CharacterName = "Derived Tester",
            BaseRuleSystem = "Rules2024",
            BuildMethod = "Manual",
            ClassModuleId = "class-fighter",
            ClassName = "Fighter",
            Level = 5,
            ProficiencyBonus = 3,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        fixture.Db.CharacterAbilityScores.AddRange(new[]
        {
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Strength", Score = 16 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Dexterity", Score = 14 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Constitution", Score = 14 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Intelligence", Score = 10 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Wisdom", Score = 10 },
            new CharacterAbilityScoreEntity { CharacterId = id, AbilityName = "Charisma", Score = 8 },
        });
        fixture.Db.CharacterSpellEntries.Add(new CharacterSpellEntryEntity
        {
            CharacterId = id,
            SpellModuleId = "spell-shield",
            SpellName = "Shield",
            PreparationMode = "Known",
        });
        fixture.Db.CharacterVitals.Add(new CharacterVitalsEntity
        {
            CharacterId = id,
            MaxHitPoints = 38,
            CurrentHitPoints = 31,
            TempHitPoints = 5,
            BaseMoveSpeed = 35,
            BaseArmorClass = 16,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await fixture.Db.SaveChangesAsync();

        var service = new CharacterComputationService(
            fixture.Db,
            new ItemEffectPipelineService(),
            new CalculationEngineService());
        var result = await service.GetDerivedStatsAsync(characterId, CancellationToken.None);

        Assert.Empty(result.Errors);
        Assert.NotNull(result.Result);
        Assert.Equal(16, result.Result.ArmorClass);
        Assert.Equal(35, result.Result.MoveSpeed);
        Assert.Equal(38, result.Result.MaxHitPoints);
        Assert.Contains("Shield", result.Result.AvailableSpells);
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
