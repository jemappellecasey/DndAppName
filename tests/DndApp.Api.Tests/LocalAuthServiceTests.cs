using DndApp.Api.Auth;
using DndApp.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DndApp.Api.Tests;

public sealed class LocalAuthServiceTests
{
    [Fact]
    public async Task RegisterAndLogin_PersistUsersAndSessions()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new LocalAuthService(fixture.Db);

        var registered = await service.RegisterAsync(new LocalRegisterRequest("Casey", "password123"), CancellationToken.None);
        Assert.Equal("Casey", registered.UserName);
        Assert.NotEmpty(registered.SessionToken);

        var login = await service.LoginAsync(new LocalLoginRequest("Casey", "password123"), CancellationToken.None);
        Assert.Equal(registered.UserId, login.UserId);

        var me = await service.GetSessionAsync(login.SessionToken, CancellationToken.None);
        Assert.NotNull(me);
        Assert.Equal("Casey", me.UserName);
    }

    [Fact]
    public async Task Register_RejectsDuplicateUserName()
    {
        await using var fixture = await CreateFixtureAsync();
        var service = new LocalAuthService(fixture.Db);

        await service.RegisterAsync(new LocalRegisterRequest("Casey", "password123"), CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAsync(new LocalRegisterRequest("casey", "password123"), CancellationToken.None));
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
