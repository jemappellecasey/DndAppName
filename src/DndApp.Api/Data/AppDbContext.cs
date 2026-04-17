using Microsoft.EntityFrameworkCore;

namespace DndApp.Api.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<RuleSystemEntity> RuleSystems => Set<RuleSystemEntity>();
    public DbSet<ContentSourceEntity> ContentSources => Set<ContentSourceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RuleSystemEntity>(entity =>
        {
            entity.ToTable("rule_system");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(40);
            entity.Property(x => x.Name).HasMaxLength(80);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<ContentSourceEntity>(entity =>
        {
            entity.ToTable("content_source");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(40);
            entity.Property(x => x.Code).HasMaxLength(40);
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.RuleSystemId).HasMaxLength(40);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasOne<RuleSystemEntity>()
                .WithMany()
                .HasForeignKey(x => x.RuleSystemId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public sealed class RuleSystemEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class ContentSourceEntity
{
    public string Id { get; set; } = string.Empty;
    public string RuleSystemId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
