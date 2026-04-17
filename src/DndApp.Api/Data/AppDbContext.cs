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
    public DbSet<SourceBookEntity> SourceBooks => Set<SourceBookEntity>();
    public DbSet<SourceChapterEntity> SourceChapters => Set<SourceChapterEntity>();
    public DbSet<SourceSectionEntity> SourceSections => Set<SourceSectionEntity>();
    public DbSet<SourceBlockEntity> SourceBlocks => Set<SourceBlockEntity>();

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

        modelBuilder.Entity<SourceBookEntity>(entity =>
        {
            entity.ToTable("source_book");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.SourceCode).HasMaxLength(40);
            entity.Property(x => x.FileName).HasMaxLength(260);
            entity.Property(x => x.VersionTag).HasMaxLength(40);
            entity.HasIndex(x => new { x.SourceCode, x.VersionTag }).IsUnique();
        });

        modelBuilder.Entity<SourceChapterEntity>(entity =>
        {
            entity.ToTable("source_chapter");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.SourceBookId).HasMaxLength(64);
            entity.Property(x => x.Title).HasMaxLength(300);
            entity.HasIndex(x => new { x.SourceBookId, x.ChapterOrder }).IsUnique();
            entity.HasOne<SourceBookEntity>()
                .WithMany()
                .HasForeignKey(x => x.SourceBookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SourceSectionEntity>(entity =>
        {
            entity.ToTable("source_section");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.SourceChapterId).HasMaxLength(64);
            entity.Property(x => x.Title).HasMaxLength(400);
            entity.HasIndex(x => new { x.SourceChapterId, x.SectionOrder }).IsUnique();
            entity.HasOne<SourceChapterEntity>()
                .WithMany()
                .HasForeignKey(x => x.SourceChapterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SourceBlockEntity>(entity =>
        {
            entity.ToTable("source_block");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.SourceSectionId).HasMaxLength(64);
            entity.Property(x => x.BlockType).HasMaxLength(40);
            entity.Property(x => x.RawText).HasColumnType("TEXT");
            entity.Property(x => x.ParseConfidence).HasPrecision(5, 4);
            entity.HasOne<SourceSectionEntity>()
                .WithMany()
                .HasForeignKey(x => x.SourceSectionId)
                .OnDelete(DeleteBehavior.Cascade);
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
