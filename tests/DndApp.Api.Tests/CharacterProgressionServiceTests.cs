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
        Assert.Equal(1, purchase.Data.Ep);

        var converted = await service.ConvertCurrencyAsync(characterId, new ConvertCurrencyRequest("gp", "sp", 1), CancellationToken.None);
        Assert.Empty(converted.Errors);
        Assert.NotNull(converted.Data);
        Assert.Equal(6, converted.Data.Gp);
        Assert.Equal(10, converted.Data.Sp);
        Assert.Equal(1, converted.Data.Ep);

        var consolidated = await service.ConsolidateCurrencyAsync(characterId, new ConsolidateCurrencyRequest(false), CancellationToken.None);
        Assert.Equal(7, consolidated.Gp);
        Assert.Equal(0, consolidated.Sp);
        Assert.Equal(1, consolidated.Ep);
    }

    [Fact]
    public async Task RecommendedSpells_ReturnsAdvisoryDataGap_WhenCuratedSourceMissing()
    {
        await using var fixture = await CreateFixtureAsync();
        var characterId = Guid.NewGuid();
        fixture.Db.CharacterSheets.Add(new CharacterSheetEntity
        {
            CharacterId = characterId.ToString(),
            OwnerUserId = "local:test-owner",
            CharacterName = "Spells Test",
            BaseRuleSystem = "Rules2024",
            BuildMethod = "PointBuy",
            ClassModuleId = "class-wizard",
            ClassName = "Wizard",
            Level = 1,
            ProficiencyBonus = 2,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await fixture.Db.SaveChangesAsync();

        var service = new CharacterProgressionService(fixture.Db);
        var result = await service.GetRecommendedSpellsAsync(characterId, "class-wizard", 1, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Empty(result.RecommendedSpells);
        Assert.False(string.IsNullOrWhiteSpace(result.AdvisoryMessage));
        Assert.False(string.IsNullOrWhiteSpace(result.DataGap));
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
