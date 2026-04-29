using DndApp.Api.Characters;
using DndApp.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DndApp.Api.Tests;

public sealed class CharacterProgressionServiceTests
{
    [Fact]
    public async Task Currency_PurchaseAndConsolidate_WorkAtBookRates()
    {
        await using var fixture = await CreateFixtureAsync();
        var characterId = Guid.NewGuid();
        fixture.Db.CharacterSheets.Add(new CharacterSheetEntity
        {
            CharacterId = characterId.ToString(),
            OwnerUserId = "local:test-owner",
            CharacterName = "Currency Test",
            BaseRuleSystem = "Rules2024",
            BuildMethod = "PointBuy",
            ClassModuleId = "class-fighter",
            ClassName = "Fighter",
            Level = 1,
            ProficiencyBonus = 2,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await fixture.Db.SaveChangesAsync();

        var service = new CharacterProgressionService(fixture.Db);
        await service.UpsertCurrencyAsync(characterId, new UpsertCharacterCurrencyRequest(0, 0, 0, 15, 0), CancellationToken.None);
        var purchase = await service.PurchaseFromCurrencyAsync(characterId, new PurchaseFromCurrencyRequest(7.5m, 1), CancellationToken.None);
        Assert.Empty(purchase.Errors);
        Assert.NotNull(purchase.Data);
        Assert.Equal(7, purchase.Data.Gp);
        Assert.Equal(5, purchase.Data.Sp);
        Assert.Equal(0, purchase.Data.Ep);

        var converted = await service.ConvertCurrencyAsync(characterId, new ConvertCurrencyRequest("gp", "sp", 1), CancellationToken.None);
        Assert.Empty(converted.Errors);
        Assert.NotNull(converted.Data);
        Assert.Equal(6, converted.Data.Gp);
        Assert.Equal(15, converted.Data.Sp);
        Assert.Equal(0, converted.Data.Ep);

        var consolidated = await service.ConsolidateCurrencyAsync(characterId, new ConsolidateCurrencyRequest(false), CancellationToken.None);
        Assert.Equal(7, consolidated.Gp);
        Assert.Equal(5, consolidated.Sp);
        Assert.Equal(0, consolidated.Ep);
        Assert.Equal(0, consolidated.Pp);
    }

    [Fact]
    public async Task RecommendedSpells_CalculatesWizardPrepCount_BasedOnLevelAndIntMod()
    {
        await using var fixture = await CreateFixtureAsync();
        var characterId = Guid.NewGuid();
        fixture.Db.CharacterSheets.Add(new CharacterSheetEntity
        {
            CharacterId = characterId.ToString(),
            OwnerUserId = "local:test-owner",
            CharacterName = "Wizard Test",
            BaseRuleSystem = "Rules2024",
            BuildMethod = "PointBuy",
            ClassModuleId = "class-wizard",
            ClassName = "Wizard",
            Level = 5,
            ProficiencyBonus = 3,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        fixture.Db.CharacterAbilityScores.AddRange(
            new CharacterAbilityScoreEntity { CharacterId = characterId.ToString(), AbilityName = "Intelligence", Score = 16 },
            new CharacterAbilityScoreEntity { CharacterId = characterId.ToString(), AbilityName = "Wisdom", Score = 10 }
        );
        fixture.Db.CharacterClassLevels.Add(new CharacterClassLevelEntity
        {
            CharacterId = characterId.ToString(),
            ClassModuleId = "class-wizard",
            ClassName = "Wizard",
            Level = 5,
            SortOrder = 0,
        });
        await fixture.Db.SaveChangesAsync();

        var service = new CharacterProgressionService(fixture.Db);
        var result = await service.GetRecommendedSpellsAsync(characterId, "class-wizard", 5, CancellationToken.None);
        Assert.NotNull(result);
        var wizardSource = result.SpellSources.FirstOrDefault(x => x.SourceName == "Wizard Spells");
        Assert.NotNull(wizardSource);
        // Wizard: level (5) + INT mod (+3) = 8 spells
        Assert.Equal(8, wizardSource.PrepareCount);
    }

    [Fact]
    public async Task RecommendedSpells_CalculatesClericPrepCount_BasedOnLevelAndWisMod()
    {
        await using var fixture = await CreateFixtureAsync();
        var characterId = Guid.NewGuid();
        fixture.Db.CharacterSheets.Add(new CharacterSheetEntity
        {
            CharacterId = characterId.ToString(),
            OwnerUserId = "local:test-owner",
            CharacterName = "Cleric Test",
            BaseRuleSystem = "Rules2024",
            BuildMethod = "PointBuy",
            ClassModuleId = "class-cleric",
            ClassName = "Cleric",
            Level = 3,
            ProficiencyBonus = 2,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        fixture.Db.CharacterAbilityScores.AddRange(
            new CharacterAbilityScoreEntity { CharacterId = characterId.ToString(), AbilityName = "Wisdom", Score = 14 },
            new CharacterAbilityScoreEntity { CharacterId = characterId.ToString(), AbilityName = "Intelligence", Score = 10 }
        );
        fixture.Db.CharacterClassLevels.Add(new CharacterClassLevelEntity
        {
            CharacterId = characterId.ToString(),
            ClassModuleId = "class-cleric",
            ClassName = "Cleric",
            Level = 3,
            SortOrder = 0,
        });
        await fixture.Db.SaveChangesAsync();

        var service = new CharacterProgressionService(fixture.Db);
        var result = await service.GetRecommendedSpellsAsync(characterId, "class-cleric", 3, CancellationToken.None);
        Assert.NotNull(result);
        var clericSource = result.SpellSources.FirstOrDefault(x => x.SourceName == "Cleric Spells");
        Assert.NotNull(clericSource);
        // Cleric: level (3) + WIS mod (+2) = 5 spells
        Assert.Equal(5, clericSource.PrepareCount);
    }

    [Fact]
    public async Task RecommendedSpells_CalculatesBardPrepCount_BasedOnHalfLevelAndChaMod()
    {
        await using var fixture = await CreateFixtureAsync();
        var characterId = Guid.NewGuid();
        fixture.Db.CharacterSheets.Add(new CharacterSheetEntity
        {
            CharacterId = characterId.ToString(),
            OwnerUserId = "local:test-owner",
            CharacterName = "Bard Test",
            BaseRuleSystem = "Rules2024",
            BuildMethod = "PointBuy",
            ClassModuleId = "class-bard",
            ClassName = "Bard",
            Level = 4,
            ProficiencyBonus = 2,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        fixture.Db.CharacterAbilityScores.AddRange(
            new CharacterAbilityScoreEntity { CharacterId = characterId.ToString(), AbilityName = "Charisma", Score = 15 },
            new CharacterAbilityScoreEntity { CharacterId = characterId.ToString(), AbilityName = "Intelligence", Score = 10 }
        );
        fixture.Db.CharacterClassLevels.Add(new CharacterClassLevelEntity
        {
            CharacterId = characterId.ToString(),
            ClassModuleId = "class-bard",
            ClassName = "Bard",
            Level = 4,
            SortOrder = 0,
        });
        await fixture.Db.SaveChangesAsync();

        var service = new CharacterProgressionService(fixture.Db);
        var result = await service.GetRecommendedSpellsAsync(characterId, "class-bard", 4, CancellationToken.None);
        Assert.NotNull(result);
        var bardSource = result.SpellSources.FirstOrDefault(x => x.SourceName == "Bard Spells");
        Assert.NotNull(bardSource);
        // Bard: (level + 1) / 2 = (4 + 1) / 2 = 2, + CHA mod (+2) = 4 spells
        Assert.Equal(4, bardSource.PrepareCount);
    }

    [Fact]
    public async Task RecommendedSpells_PaladinRequiresLevel5_BeforeSpellPrep()
    {
        await using var fixture = await CreateFixtureAsync();
        var characterId = Guid.NewGuid();
        fixture.Db.CharacterSheets.Add(new CharacterSheetEntity
        {
            CharacterId = characterId.ToString(),
            OwnerUserId = "local:test-owner",
            CharacterName = "Paladin Test",
            BaseRuleSystem = "Rules2024",
            BuildMethod = "PointBuy",
            ClassModuleId = "class-paladin",
            ClassName = "Paladin",
            Level = 3,
            ProficiencyBonus = 2,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        fixture.Db.CharacterAbilityScores.AddRange(
            new CharacterAbilityScoreEntity { CharacterId = characterId.ToString(), AbilityName = "Charisma", Score = 16 }
        );
        fixture.Db.CharacterClassLevels.Add(new CharacterClassLevelEntity
        {
            CharacterId = characterId.ToString(),
            ClassModuleId = "class-paladin",
            ClassName = "Paladin",
            Level = 3,
            SortOrder = 0,
        });
        await fixture.Db.SaveChangesAsync();

        var service = new CharacterProgressionService(fixture.Db);
        var result = await service.GetRecommendedSpellsAsync(characterId, "class-paladin", 3, CancellationToken.None);
        Assert.NotNull(result);
        var paladinSource = result.SpellSources.FirstOrDefault(x => x.SourceName == "Paladin Spells");
        Assert.Null(paladinSource);
    }

    [Fact]
    public async Task RecommendedSpells_SorcererReturnsZeroPrepCount()
    {
        await using var fixture = await CreateFixtureAsync();
        var characterId = Guid.NewGuid();
        fixture.Db.CharacterSheets.Add(new CharacterSheetEntity
        {
            CharacterId = characterId.ToString(),
            OwnerUserId = "local:test-owner",
            CharacterName = "Sorcerer Test",
            BaseRuleSystem = "Rules2024",
            BuildMethod = "PointBuy",
            ClassModuleId = "class-sorcerer",
            ClassName = "Sorcerer",
            Level = 5,
            ProficiencyBonus = 3,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        fixture.Db.CharacterAbilityScores.AddRange(
            new CharacterAbilityScoreEntity { CharacterId = characterId.ToString(), AbilityName = "Charisma", Score = 16 }
        );
        fixture.Db.CharacterClassLevels.Add(new CharacterClassLevelEntity
        {
            CharacterId = characterId.ToString(),
            ClassModuleId = "class-sorcerer",
            ClassName = "Sorcerer",
            Level = 5,
            SortOrder = 0,
        });
        await fixture.Db.SaveChangesAsync();

        var service = new CharacterProgressionService(fixture.Db);
        var result = await service.GetRecommendedSpellsAsync(characterId, "class-sorcerer", 5, CancellationToken.None);
        Assert.NotNull(result);
        var sorcererSource = result.SpellSources.FirstOrDefault(x => x.SourceName == "Sorcerer Spells");
        Assert.Null(sorcererSource);
    }

    [Fact]
    public async Task UpsertVitals_CreatesCharacterSheet_FromDraftOwner()
    {
        await using var fixture = await CreateFixtureAsync();
        var characterId = Guid.NewGuid();
        fixture.Db.CharacterDrafts.Add(new CharacterDraftEntity
        {
            CharacterId = characterId.ToString(),
            OwnerUserId = "local:test-owner",
            CharacterName = "Draft Vitals Test",
            BaseRuleSystem = "Rules2024",
            MixedModeEnabled = false,
            OverlaySourcesJson = "[]",
            IsFinalized = false,
            StepsJson = "[]",
            WarningsJson = "[]",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await fixture.Db.SaveChangesAsync();

        var service = new CharacterProgressionService(fixture.Db);
        var saved = await service.UpsertVitalsAsync(
            characterId,
            new UpsertCharacterVitalsRequest(15, 12, 3, 30, 16),
            CancellationToken.None);

        Assert.Equal(characterId, saved.CharacterId);
        var sheet = await fixture.Db.CharacterSheets.SingleOrDefaultAsync(x => x.CharacterId == characterId.ToString());
        Assert.NotNull(sheet);
        Assert.Equal("local:test-owner", sheet.OwnerUserId);
        Assert.Equal("Draft Vitals Test", sheet.CharacterName);
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
