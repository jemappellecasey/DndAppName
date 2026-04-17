using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DndApp.Api.Data;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var provider = (configuration["Database:Provider"] ?? "sqlite").Trim().ToLowerInvariant();
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        if (provider == "sqlite")
        {
            var connection = configuration["Database:ConnectionStrings:Sqlite"] ?? "Data Source=dndapp.sqlite";
            optionsBuilder.UseSqlite(connection);
        }
        else
        {
            throw new InvalidOperationException(
                $"Unsupported provider '{provider}' in design-time factory for this phase. Use 'sqlite' for now.");
        }

        return new AppDbContext(optionsBuilder.Options);
    }
}
