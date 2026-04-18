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
    public DbSet<RuleModuleEntity> RuleModules => Set<RuleModuleEntity>();
    public DbSet<RuleVariantEntity> RuleVariants => Set<RuleVariantEntity>();
    public DbSet<PrerequisiteEntity> Prerequisites => Set<PrerequisiteEntity>();
    public DbSet<ConstraintEntity> Constraints => Set<ConstraintEntity>();
    public DbSet<ItemDefinitionEntity> ItemDefinitions => Set<ItemDefinitionEntity>();
    public DbSet<ItemEffectEntity> ItemEffects => Set<ItemEffectEntity>();
    public DbSet<CharacterSheetEntity> CharacterSheets => Set<CharacterSheetEntity>();
    public DbSet<CharacterAbilityScoreEntity> CharacterAbilityScores => Set<CharacterAbilityScoreEntity>();
    public DbSet<CharacterSkillProficiencyEntity> CharacterSkillProficiencies => Set<CharacterSkillProficiencyEntity>();
    public DbSet<CharacterInventoryItemEntity> CharacterInventoryItems => Set<CharacterInventoryItemEntity>();
    public DbSet<UserAccountEntity> UserAccounts => Set<UserAccountEntity>();
    public DbSet<UserSessionEntity> UserSessions => Set<UserSessionEntity>();
    public DbSet<IngestionRunEntity> IngestionRuns => Set<IngestionRunEntity>();
    public DbSet<ReviewQueueEntity> ReviewQueue => Set<ReviewQueueEntity>();
    public DbSet<CorrectionOverrideEntity> CorrectionOverrides => Set<CorrectionOverrideEntity>();
    public DbSet<ImportReportEntity> ImportReports => Set<ImportReportEntity>();

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

        modelBuilder.Entity<RuleModuleEntity>(entity =>
        {
            entity.ToTable("rule_module");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.ContentSourceId).HasMaxLength(40);
            entity.Property(x => x.ModuleType).HasMaxLength(64);
            entity.Property(x => x.Slug).HasMaxLength(160);
            entity.Property(x => x.DisplayName).HasMaxLength(300);
            entity.Property(x => x.VersionTag).HasMaxLength(40);
            entity.HasIndex(x => new { x.ContentSourceId, x.Slug, x.VersionTag }).IsUnique();
            entity.HasOne<ContentSourceEntity>()
                .WithMany()
                .HasForeignKey(x => x.ContentSourceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RuleVariantEntity>(entity =>
        {
            entity.ToTable("rule_variant");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.RuleModuleId).HasMaxLength(64);
            entity.Property(x => x.RuleSystemId).HasMaxLength(40);
            entity.Property(x => x.CompatibilityTagsJson).HasColumnType("TEXT");
            entity.Property(x => x.PayloadJson).HasColumnType("TEXT");
            entity.HasIndex(x => new { x.RuleModuleId, x.RuleSystemId }).IsUnique();
            entity.HasOne<RuleModuleEntity>()
                .WithMany()
                .HasForeignKey(x => x.RuleModuleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<RuleSystemEntity>()
                .WithMany()
                .HasForeignKey(x => x.RuleSystemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PrerequisiteEntity>(entity =>
        {
            entity.ToTable("prerequisite");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.RuleModuleId).HasMaxLength(64);
            entity.Property(x => x.PredicateJson).HasColumnType("TEXT");
            entity.HasOne<RuleModuleEntity>()
                .WithMany()
                .HasForeignKey(x => x.RuleModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConstraintEntity>(entity =>
        {
            entity.ToTable("constraint");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.Scope).HasMaxLength(80);
            entity.Property(x => x.ConstraintJson).HasColumnType("TEXT");
        });

        modelBuilder.Entity<ItemDefinitionEntity>(entity =>
        {
            entity.ToTable("item_definition");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.RuleModuleId).HasMaxLength(64);
            entity.Property(x => x.ItemType).HasMaxLength(80);
            entity.Property(x => x.Rarity).HasMaxLength(40);
            entity.Property(x => x.GoldValue).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Weight).HasColumnType("decimal(18,2)");
            entity.Property(x => x.DamageDice).HasMaxLength(40);
            entity.Property(x => x.WeaponAbility).HasMaxLength(24);
            entity.Property(x => x.ChargesModelJson).HasColumnType("TEXT");
            entity.HasOne<RuleModuleEntity>()
                .WithMany()
                .HasForeignKey(x => x.RuleModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ItemEffectEntity>(entity =>
        {
            entity.ToTable("item_effect");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.ItemDefinitionId).HasMaxLength(64);
            entity.Property(x => x.EffectType).HasMaxLength(80);
            entity.Property(x => x.EffectPayloadJson).HasColumnType("TEXT");
            entity.Property(x => x.ConditionJson).HasColumnType("TEXT");
            entity.HasOne<ItemDefinitionEntity>()
                .WithMany()
                .HasForeignKey(x => x.ItemDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CharacterSheetEntity>(entity =>
        {
            entity.ToTable("character_sheet");
            entity.HasKey(x => x.CharacterId);
            entity.Property(x => x.CharacterId).HasMaxLength(36);
            entity.Property(x => x.OwnerUserId).HasMaxLength(64);
            entity.Property(x => x.CharacterName).HasMaxLength(160);
            entity.Property(x => x.BaseRuleSystem).HasMaxLength(24);
            entity.Property(x => x.BuildMethod).HasMaxLength(24);
            entity.Property(x => x.ClassModuleId).HasMaxLength(64);
            entity.Property(x => x.ClassName).HasMaxLength(160);
            entity.HasIndex(x => x.ClassModuleId);
            entity.HasIndex(x => x.OwnerUserId);
        });

        modelBuilder.Entity<CharacterAbilityScoreEntity>(entity =>
        {
            entity.ToTable("character_ability_score");
            entity.HasKey(x => new { x.CharacterId, x.AbilityName });
            entity.Property(x => x.CharacterId).HasMaxLength(36);
            entity.Property(x => x.AbilityName).HasMaxLength(24);
            entity.HasOne<CharacterSheetEntity>()
                .WithMany()
                .HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CharacterSkillProficiencyEntity>(entity =>
        {
            entity.ToTable("character_skill_proficiency");
            entity.HasKey(x => new { x.CharacterId, x.SkillName });
            entity.Property(x => x.CharacterId).HasMaxLength(36);
            entity.Property(x => x.SkillName).HasMaxLength(64);
            entity.HasOne<CharacterSheetEntity>()
                .WithMany()
                .HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CharacterInventoryItemEntity>(entity =>
        {
            entity.ToTable("character_inventory_item");
            entity.HasKey(x => x.InventoryItemId);
            entity.Property(x => x.InventoryItemId).HasMaxLength(64);
            entity.Property(x => x.CharacterId).HasMaxLength(36);
            entity.Property(x => x.ItemDefinitionId).HasMaxLength(64);
            entity.Property(x => x.ItemName).HasMaxLength(300);
            entity.Property(x => x.Quantity).HasDefaultValue(1);
            entity.HasIndex(x => x.CharacterId);
            entity.HasIndex(x => x.ItemDefinitionId);
            entity.HasOne<CharacterSheetEntity>()
                .WithMany()
                .HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ItemDefinitionEntity>()
                .WithMany()
                .HasForeignKey(x => x.ItemDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserAccountEntity>(entity =>
        {
            entity.ToTable("user_account");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.UserId).HasMaxLength(64);
            entity.Property(x => x.UserName).HasMaxLength(120);
            entity.Property(x => x.NormalizedUserName).HasMaxLength(120);
            entity.Property(x => x.PasswordHash).HasMaxLength(512);
            entity.HasIndex(x => x.NormalizedUserName).IsUnique();
        });

        modelBuilder.Entity<UserSessionEntity>(entity =>
        {
            entity.ToTable("user_session");
            entity.HasKey(x => x.SessionToken);
            entity.Property(x => x.SessionToken).HasMaxLength(128);
            entity.Property(x => x.UserId).HasMaxLength(64);
            entity.HasIndex(x => x.UserId);
            entity.HasOne<UserAccountEntity>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IngestionRunEntity>(entity =>
        {
            entity.ToTable("ingestion_run");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.SourceCode).HasMaxLength(40);
            entity.Property(x => x.VersionTag).HasMaxLength(40);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.Checksum).HasMaxLength(128);
            entity.HasIndex(x => new { x.SourceCode, x.VersionTag, x.StartedAtUtc });
        });

        modelBuilder.Entity<ReviewQueueEntity>(entity =>
        {
            entity.ToTable("review_queue");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.IngestionRunId).HasMaxLength(64);
            entity.Property(x => x.QueueType).HasMaxLength(40);
            entity.Property(x => x.ReferenceId).HasMaxLength(128);
            entity.Property(x => x.Confidence).HasPrecision(5, 4);
            entity.Property(x => x.Notes).HasColumnType("TEXT");
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.HasIndex(x => new { x.IngestionRunId, x.Status });
            entity.HasOne<IngestionRunEntity>()
                .WithMany()
                .HasForeignKey(x => x.IngestionRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CorrectionOverrideEntity>(entity =>
        {
            entity.ToTable("correction_override");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.ReviewQueueId).HasMaxLength(64);
            entity.Property(x => x.OverrideJson).HasColumnType("TEXT");
            entity.Property(x => x.AppliedBy).HasMaxLength(80);
            entity.HasIndex(x => x.ReviewQueueId);
            entity.HasOne<ReviewQueueEntity>()
                .WithMany()
                .HasForeignKey(x => x.ReviewQueueId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ImportReportEntity>(entity =>
        {
            entity.ToTable("import_report");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.IngestionRunId).HasMaxLength(64);
            entity.Property(x => x.ReportJson).HasColumnType("TEXT");
            entity.HasIndex(x => x.IngestionRunId);
            entity.HasOne<IngestionRunEntity>()
                .WithMany()
                .HasForeignKey(x => x.IngestionRunId)
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
