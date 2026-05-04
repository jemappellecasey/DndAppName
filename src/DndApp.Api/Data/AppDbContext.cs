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
    public DbSet<Race2014Entity> Races2014 => Set<Race2014Entity>();
    public DbSet<Species2024Entity> Species2024 => Set<Species2024Entity>();
    public DbSet<Background2014Entity> Backgrounds2014 => Set<Background2014Entity>();
    public DbSet<Background2024Entity> Backgrounds2024 => Set<Background2024Entity>();
    public DbSet<Feat2014Entity> Feats2014 => Set<Feat2014Entity>();
    public DbSet<Feat2024Entity> Feats2024 => Set<Feat2024Entity>();
    public DbSet<SpellEntity> Spells => Set<SpellEntity>();
    public DbSet<Class2014Entity> Classes2014 => Set<Class2014Entity>();
    public DbSet<Class2024Entity> Classes2024 => Set<Class2024Entity>();
    public DbSet<Subclass2014Entity> Subclasses2014 => Set<Subclass2014Entity>();
    public DbSet<Subclass2024Entity> Subclasses2024 => Set<Subclass2024Entity>();
    public DbSet<Item2014Entity> Items2014 => Set<Item2014Entity>();
    public DbSet<Item2024Entity> Items2024 => Set<Item2024Entity>();
    public DbSet<CharacterSheetEntity> CharacterSheets => Set<CharacterSheetEntity>();
    public DbSet<CharacterAbilityScoreEntity> CharacterAbilityScores => Set<CharacterAbilityScoreEntity>();
    public DbSet<CharacterSkillProficiencyEntity> CharacterSkillProficiencies => Set<CharacterSkillProficiencyEntity>();
    public DbSet<CharacterInventoryItemEntity> CharacterInventoryItems => Set<CharacterInventoryItemEntity>();
    public DbSet<CharacterRecordEntity> CharacterRecords => Set<CharacterRecordEntity>();
    public DbSet<CharacterDraftEntity> CharacterDrafts => Set<CharacterDraftEntity>();
    public DbSet<CharacterHistoryEntity> CharacterHistoryEntries => Set<CharacterHistoryEntity>();
    public DbSet<CharacterClassLevelEntity> CharacterClassLevels => Set<CharacterClassLevelEntity>();
    public DbSet<CharacterSelectedModuleEntity> CharacterSelectedModules => Set<CharacterSelectedModuleEntity>();
    public DbSet<CharacterSpellEntryEntity> CharacterSpellEntries => Set<CharacterSpellEntryEntity>();
    public DbSet<CharacterResourcePoolEntity> CharacterResourcePools => Set<CharacterResourcePoolEntity>();
    public DbSet<CharacterVitalsEntity> CharacterVitals => Set<CharacterVitalsEntity>();
    public DbSet<UserAccountEntity> UserAccounts => Set<UserAccountEntity>();
      public DbSet<UserSessionEntity> UserSessions => Set<UserSessionEntity>();
    public DbSet<IngestionRunEntity> IngestionRuns => Set<IngestionRunEntity>();
    public DbSet<ReviewQueueEntity> ReviewQueue => Set<ReviewQueueEntity>();
    public DbSet<CorrectionOverrideEntity> CorrectionOverrides => Set<CorrectionOverrideEntity>();
    public DbSet<ImportReportEntity> ImportReports => Set<ImportReportEntity>();
    public DbSet<SkillProficiencySourceEntity> SkillProficiencySources => Set<SkillProficiencySourceEntity>();
    public DbSet<CharacterExperienceEntity> CharacterExperience => Set<CharacterExperienceEntity>();
    public DbSet<CharacterLevelProgressionEntity> CharacterLevelProgression => Set<CharacterLevelProgressionEntity>();
    public DbSet<CharacterLevelUpChoiceEntity> CharacterLevelUpChoices => Set<CharacterLevelUpChoiceEntity>();
    public DbSet<ClassSpellGrantEntity> ClassSpellGrants => Set<ClassSpellGrantEntity>();
    public DbSet<SubclassSpellGrantEntity> SubclassSpellGrants => Set<SubclassSpellGrantEntity>();
    public DbSet<RaceSpellGrantEntity> RaceSpellGrants => Set<RaceSpellGrantEntity>();
    public DbSet<BackgroundSpellGrantEntity> BackgroundSpellGrants => Set<BackgroundSpellGrantEntity>();
    public DbSet<OriginSpellGrantEntity> OriginSpellGrants => Set<OriginSpellGrantEntity>();
    public DbSet<FeatSpellGrantEntity> FeatSpellGrants => Set<FeatSpellGrantEntity>();

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

        modelBuilder.Entity<Race2014Entity>(entity =>
        {
            entity.ToTable("catalog_race_2014");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.ContentSourceId).HasMaxLength(40);
            entity.Property(x => x.LegacyRuleModuleId).HasMaxLength(64);
            entity.Property(x => x.Slug).HasMaxLength(160);
            entity.Property(x => x.Name).HasMaxLength(300);
            entity.Property(x => x.ParentRaceSlug).HasMaxLength(160);
            entity.Property(x => x.Description).HasColumnType("TEXT");
            entity.Property(x => x.AbilityBonusesJson).HasColumnType("TEXT");
            entity.Property(x => x.LanguagesJson).HasColumnType("TEXT");
            entity.Property(x => x.TraitsJson).HasColumnType("TEXT");
            entity.Property(x => x.EditionPayloadJson).HasColumnType("TEXT");
            entity.HasIndex(x => new { x.ContentSourceId, x.Slug }).IsUnique();
            entity.HasIndex(x => x.LegacyRuleModuleId);
        });

        modelBuilder.Entity<Species2024Entity>(entity =>
        {
            entity.ToTable("catalog_species_2024");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.ContentSourceId).HasMaxLength(40);
            entity.Property(x => x.LegacyRuleModuleId).HasMaxLength(64);
            entity.Property(x => x.Slug).HasMaxLength(160);
            entity.Property(x => x.Name).HasMaxLength(300);
            entity.Property(x => x.Description).HasColumnType("TEXT");
            entity.Property(x => x.AbilityBonusesJson).HasColumnType("TEXT");
            entity.Property(x => x.LanguagesJson).HasColumnType("TEXT");
            entity.Property(x => x.TraitsJson).HasColumnType("TEXT");
            entity.Property(x => x.EditionPayloadJson).HasColumnType("TEXT");
            entity.HasIndex(x => new { x.ContentSourceId, x.Slug }).IsUnique();
            entity.HasIndex(x => x.LegacyRuleModuleId);
        });

        modelBuilder.Entity<Background2014Entity>(entity =>
        {
            entity.ToTable("catalog_background_2014");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.ContentSourceId).HasMaxLength(40);
            entity.Property(x => x.LegacyRuleModuleId).HasMaxLength(64);
            entity.Property(x => x.Slug).HasMaxLength(160);
            entity.Property(x => x.Name).HasMaxLength(300);
            entity.Property(x => x.Description).HasColumnType("TEXT");
            entity.Property(x => x.SkillProficienciesJson).HasColumnType("TEXT");
            entity.Property(x => x.ToolProficienciesJson).HasColumnType("TEXT");
            entity.Property(x => x.LanguageChoicesJson).HasColumnType("TEXT");
            entity.Property(x => x.EquipmentJson).HasColumnType("TEXT");
            entity.Property(x => x.EditionPayloadJson).HasColumnType("TEXT");
            entity.HasIndex(x => new { x.ContentSourceId, x.Slug }).IsUnique();
            entity.HasIndex(x => x.LegacyRuleModuleId);
        });

        modelBuilder.Entity<Background2024Entity>(entity =>
        {
            entity.ToTable("catalog_background_2024");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.ContentSourceId).HasMaxLength(40);
            entity.Property(x => x.LegacyRuleModuleId).HasMaxLength(64);
            entity.Property(x => x.Slug).HasMaxLength(160);
            entity.Property(x => x.Name).HasMaxLength(300);
            entity.Property(x => x.Description).HasColumnType("TEXT");
            entity.Property(x => x.SkillProficienciesJson).HasColumnType("TEXT");
            entity.Property(x => x.ToolProficienciesJson).HasColumnType("TEXT");
            entity.Property(x => x.LanguageChoicesJson).HasColumnType("TEXT");
            entity.Property(x => x.GrantedFeatSlug).HasMaxLength(160);
            entity.Property(x => x.EditionPayloadJson).HasColumnType("TEXT");
            entity.HasIndex(x => new { x.ContentSourceId, x.Slug }).IsUnique();
            entity.HasIndex(x => x.LegacyRuleModuleId);
        });

        modelBuilder.Entity<Feat2014Entity>(entity =>
        {
            entity.ToTable("catalog_feat_2014");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.ContentSourceId).HasMaxLength(40);
            entity.Property(x => x.LegacyRuleModuleId).HasMaxLength(64);
            entity.Property(x => x.Slug).HasMaxLength(160);
            entity.Property(x => x.Name).HasMaxLength(300);
            entity.Property(x => x.Description).HasColumnType("TEXT");
            entity.Property(x => x.PrerequisitesJson).HasColumnType("TEXT");
            entity.Property(x => x.EditionPayloadJson).HasColumnType("TEXT");
            entity.HasIndex(x => new { x.ContentSourceId, x.Slug }).IsUnique();
            entity.HasIndex(x => x.LegacyRuleModuleId);
        });

        modelBuilder.Entity<Feat2024Entity>(entity =>
        {
            entity.ToTable("catalog_feat_2024");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.ContentSourceId).HasMaxLength(40);
            entity.Property(x => x.LegacyRuleModuleId).HasMaxLength(64);
            entity.Property(x => x.Slug).HasMaxLength(160);
            entity.Property(x => x.Name).HasMaxLength(300);
            entity.Property(x => x.Description).HasColumnType("TEXT");
            entity.Property(x => x.Category).HasMaxLength(80);
            entity.Property(x => x.PrerequisitesJson).HasColumnType("TEXT");
            entity.Property(x => x.EditionPayloadJson).HasColumnType("TEXT");
            entity.HasIndex(x => new { x.ContentSourceId, x.Slug }).IsUnique();
            entity.HasIndex(x => x.LegacyRuleModuleId);
        });

        modelBuilder.Entity<SpellEntity>(entity =>
        {
            entity.ToTable("catalog_spell");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.ContentSourceId).HasMaxLength(40);
            entity.Property(x => x.LegacyRuleModuleId).HasMaxLength(64);
            entity.Property(x => x.SourceSectionId).HasMaxLength(64);
            entity.Property(x => x.Slug).HasMaxLength(160);
            entity.Property(x => x.Name).HasMaxLength(300);
            entity.Property(x => x.EditionYear);
            entity.Property(x => x.School).HasMaxLength(40);
            entity.Property(x => x.CastingTime).HasMaxLength(120);
            entity.Property(x => x.RangeText).HasMaxLength(160);
            entity.Property(x => x.Duration).HasMaxLength(160);
            entity.Property(x => x.Description).HasColumnType("TEXT");
            entity.Property(x => x.IngestionConfidence).HasPrecision(5, 4);
            entity.Property(x => x.StatBlockJson).HasColumnType("TEXT");
            entity.Property(x => x.EditionPayloadJson).HasColumnType("TEXT");
            entity.HasIndex(x => new { x.ContentSourceId, x.Slug, x.EditionYear }).IsUnique();
            entity.HasIndex(x => new { x.EditionYear, x.Level, x.Name });
            entity.HasIndex(x => x.LegacyRuleModuleId);
            entity.HasIndex(x => x.SourceSectionId);
        });

        modelBuilder.Entity<Class2014Entity>(entity =>
        {
            entity.ToTable("catalog_class_2014");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.ContentSourceId).HasMaxLength(40);
            entity.Property(x => x.LegacyRuleModuleId).HasMaxLength(64);
            entity.Property(x => x.Slug).HasMaxLength(160);
            entity.Property(x => x.Name).HasMaxLength(300);
            entity.Property(x => x.Description).HasColumnType("TEXT");
            entity.Property(x => x.HitDie).HasMaxLength(16);
            entity.Property(x => x.PrimaryAbilityJson).HasColumnType("TEXT");
            entity.Property(x => x.SavingThrowAbilitiesJson).HasColumnType("TEXT");
            entity.Property(x => x.SkillProficienciesJson).HasColumnType("TEXT");
            entity.Property(x => x.EditionPayloadJson).HasColumnType("TEXT");
            entity.HasIndex(x => new { x.ContentSourceId, x.Slug }).IsUnique();
            entity.HasIndex(x => x.Name);
            entity.HasIndex(x => x.LegacyRuleModuleId);
        });

        modelBuilder.Entity<Class2024Entity>(entity =>
        {
            entity.ToTable("catalog_class_2024");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.ContentSourceId).HasMaxLength(40);
            entity.Property(x => x.LegacyRuleModuleId).HasMaxLength(64);
            entity.Property(x => x.Slug).HasMaxLength(160);
            entity.Property(x => x.Name).HasMaxLength(300);
            entity.Property(x => x.Description).HasColumnType("TEXT");
            entity.Property(x => x.HitDie).HasMaxLength(16);
            entity.Property(x => x.PrimaryAbilityJson).HasColumnType("TEXT");
            entity.Property(x => x.SavingThrowAbilitiesJson).HasColumnType("TEXT");
            entity.Property(x => x.SkillProficienciesJson).HasColumnType("TEXT");
            entity.Property(x => x.EditionPayloadJson).HasColumnType("TEXT");
            entity.HasIndex(x => new { x.ContentSourceId, x.Slug }).IsUnique();
            entity.HasIndex(x => x.Name);
            entity.HasIndex(x => x.LegacyRuleModuleId);
        });

        modelBuilder.Entity<Subclass2014Entity>(entity =>
        {
            entity.ToTable("catalog_subclass_2014");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.ContentSourceId).HasMaxLength(40);
            entity.Property(x => x.LegacyRuleModuleId).HasMaxLength(64);
            entity.Property(x => x.ParentClassId).HasMaxLength(64);
            entity.Property(x => x.Slug).HasMaxLength(160);
            entity.Property(x => x.Name).HasMaxLength(300);
            entity.Property(x => x.Description).HasColumnType("TEXT");
            entity.Property(x => x.EditionPayloadJson).HasColumnType("TEXT");
            entity.HasIndex(x => new { x.ContentSourceId, x.Slug }).IsUnique();
            entity.HasIndex(x => x.ParentClassId);
            entity.HasIndex(x => new { x.ParentClassId, x.Name });
            entity.HasOne<Class2014Entity>()
                .WithMany()
                .HasForeignKey(x => x.ParentClassId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Subclass2024Entity>(entity =>
        {
            entity.ToTable("catalog_subclass_2024");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.ContentSourceId).HasMaxLength(40);
            entity.Property(x => x.LegacyRuleModuleId).HasMaxLength(64);
            entity.Property(x => x.ParentClassId).HasMaxLength(64);
            entity.Property(x => x.Slug).HasMaxLength(160);
            entity.Property(x => x.Name).HasMaxLength(300);
            entity.Property(x => x.Description).HasColumnType("TEXT");
            entity.Property(x => x.EditionPayloadJson).HasColumnType("TEXT");
            entity.HasIndex(x => new { x.ContentSourceId, x.Slug }).IsUnique();
            entity.HasIndex(x => x.ParentClassId);
            entity.HasIndex(x => new { x.ParentClassId, x.Name });
            entity.HasOne<Class2024Entity>()
                .WithMany()
                .HasForeignKey(x => x.ParentClassId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Item2014Entity>(entity =>
        {
            entity.ToTable("catalog_item_2014");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.ContentSourceId).HasMaxLength(40);
            entity.Property(x => x.LegacyRuleModuleId).HasMaxLength(64);
            entity.Property(x => x.LegacyItemDefinitionId).HasMaxLength(64);
            entity.Property(x => x.Slug).HasMaxLength(160);
            entity.Property(x => x.Name).HasMaxLength(300);
            entity.Property(x => x.ItemType).HasMaxLength(80);
            entity.Property(x => x.Rarity).HasMaxLength(40);
            entity.Property(x => x.GoldValue).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Weight).HasColumnType("decimal(18,2)");
            entity.Property(x => x.DamageDice).HasMaxLength(40);
            entity.Property(x => x.WeaponAbility).HasMaxLength(24);
            entity.Property(x => x.Description).HasColumnType("TEXT");
            entity.Property(x => x.EditionPayloadJson).HasColumnType("TEXT");
            entity.HasIndex(x => new { x.ContentSourceId, x.Slug }).IsUnique();
            entity.HasIndex(x => x.LegacyRuleModuleId);
            entity.HasIndex(x => x.LegacyItemDefinitionId);
        });

        modelBuilder.Entity<Item2024Entity>(entity =>
        {
            entity.ToTable("catalog_item_2024");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.ContentSourceId).HasMaxLength(40);
            entity.Property(x => x.LegacyRuleModuleId).HasMaxLength(64);
            entity.Property(x => x.LegacyItemDefinitionId).HasMaxLength(64);
            entity.Property(x => x.Slug).HasMaxLength(160);
            entity.Property(x => x.Name).HasMaxLength(300);
            entity.Property(x => x.ItemType).HasMaxLength(80);
            entity.Property(x => x.Rarity).HasMaxLength(40);
            entity.Property(x => x.GoldValue).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Weight).HasColumnType("decimal(18,2)");
            entity.Property(x => x.DamageDice).HasMaxLength(40);
            entity.Property(x => x.WeaponAbility).HasMaxLength(24);
            entity.Property(x => x.Description).HasColumnType("TEXT");
            entity.Property(x => x.EditionPayloadJson).HasColumnType("TEXT");
            entity.HasIndex(x => new { x.ContentSourceId, x.Slug }).IsUnique();
            entity.HasIndex(x => x.LegacyRuleModuleId);
            entity.HasIndex(x => x.LegacyItemDefinitionId);
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
            entity.Property(x => x.TrainingLevel).HasMaxLength(24).HasDefaultValue("Proficient");
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

        modelBuilder.Entity<CharacterRecordEntity>(entity =>
        {
            entity.ToTable("character_record");
            entity.HasKey(x => x.CharacterId);
            entity.Property(x => x.CharacterId).HasMaxLength(36);
            entity.Property(x => x.OwnerUserId).HasMaxLength(64);
            entity.Property(x => x.CharacterName).HasMaxLength(160);
            entity.Property(x => x.BaseRuleSystem).HasMaxLength(24);
            entity.Property(x => x.OverlaySourcesJson).HasColumnType("TEXT");
            entity.HasIndex(x => x.OwnerUserId);
            entity.HasIndex(x => x.UpdatedAtUtc);
        });

        modelBuilder.Entity<CharacterDraftEntity>(entity =>
        {
            entity.ToTable("character_draft");
            entity.HasKey(x => x.CharacterId);
            entity.Property(x => x.CharacterId).HasMaxLength(36);
            entity.Property(x => x.OwnerUserId).HasMaxLength(64);
            entity.Property(x => x.CharacterName).HasMaxLength(160);
            entity.Property(x => x.BaseRuleSystem).HasMaxLength(24);
            entity.Property(x => x.OverlaySourcesJson).HasColumnType("TEXT");
            entity.Property(x => x.StepsJson).HasColumnType("TEXT");
            entity.Property(x => x.WarningsJson).HasColumnType("TEXT");
            entity.HasIndex(x => x.OwnerUserId);
            entity.HasIndex(x => x.UpdatedAtUtc);
        });

        modelBuilder.Entity<CharacterHistoryEntity>(entity =>
        {
            entity.ToTable("character_history");
            entity.HasKey(x => x.EntryId);
            entity.Property(x => x.EntryId).HasMaxLength(64);
            entity.Property(x => x.CharacterId).HasMaxLength(36);
            entity.Property(x => x.Action).HasMaxLength(80);
            entity.Property(x => x.ActorUserId).HasMaxLength(64);
            entity.Property(x => x.Details).HasColumnType("TEXT");
            entity.HasIndex(x => new { x.CharacterId, x.TimestampUtc });
        });

        modelBuilder.Entity<CharacterClassLevelEntity>(entity =>
        {
            entity.ToTable("character_class_level");
            entity.HasKey(x => new { x.CharacterId, x.SortOrder });
            entity.Property(x => x.CharacterId).HasMaxLength(36);
            entity.Property(x => x.ClassModuleId).HasMaxLength(64);
            entity.Property(x => x.ClassName).HasMaxLength(160);
            entity.HasIndex(x => new { x.CharacterId, x.ClassModuleId });
            entity.HasOne<CharacterSheetEntity>()
                .WithMany()
                .HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CharacterSelectedModuleEntity>(entity =>
        {
            entity.ToTable("character_selected_module");
            entity.HasKey(x => new { x.CharacterId, x.Slot, x.ModuleId });
            entity.Property(x => x.CharacterId).HasMaxLength(36);
            entity.Property(x => x.Slot).HasMaxLength(40);
            entity.Property(x => x.ModuleId).HasMaxLength(64);
            entity.Property(x => x.DisplayName).HasMaxLength(300);
            entity.Property(x => x.SourceCode).HasMaxLength(40);
            entity.HasOne<CharacterSheetEntity>()
                .WithMany()
                .HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CharacterSpellEntryEntity>(entity =>
        {
            entity.ToTable("character_spell_entry");
            entity.HasKey(x => new { x.CharacterId, x.SpellModuleId, x.PreparationMode });
            entity.Property(x => x.CharacterId).HasMaxLength(36);
            entity.Property(x => x.SpellModuleId).HasMaxLength(64);
            entity.Property(x => x.SpellName).HasMaxLength(160);
            entity.Property(x => x.PreparationMode).HasMaxLength(24);
            entity.HasOne<CharacterSheetEntity>()
                .WithMany()
                .HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CharacterResourcePoolEntity>(entity =>
        {
            entity.ToTable("character_resource_pool");
            entity.HasKey(x => new { x.CharacterId, x.ResourceKey });
            entity.Property(x => x.CharacterId).HasMaxLength(36);
            entity.Property(x => x.ResourceKey).HasMaxLength(64);
            entity.Property(x => x.MetadataJson).HasColumnType("TEXT");
            entity.HasOne<CharacterSheetEntity>()
                .WithMany()
                .HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CharacterVitalsEntity>(entity =>
        {
            entity.ToTable("character_vitals");
            entity.HasKey(x => x.CharacterId);
            entity.Property(x => x.CharacterId).HasMaxLength(36);
            entity.HasOne<CharacterSheetEntity>()
                .WithMany()
                .HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
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

        modelBuilder.Entity<CharacterExperienceEntity>(entity =>
        {
            entity.ToTable("character_experience");
            entity.HasKey(x => x.CharacterId);
            entity.Property(x => x.CharacterId).HasMaxLength(36);
            entity.HasOne<CharacterSheetEntity>()
                .WithMany()
                .HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CharacterLevelProgressionEntity>(entity =>
        {
            entity.ToTable("character_level_progression");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.CharacterId).HasMaxLength(36);
            entity.Property(x => x.FeatOrASIChosenJson).HasColumnType("TEXT");
            entity.HasIndex(x => new { x.CharacterId, x.Level });
            entity.HasOne<CharacterSheetEntity>()
                .WithMany()
                .HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CharacterLevelUpChoiceEntity>(entity =>
        {
            entity.ToTable("character_levelup_choice");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.CharacterId).HasMaxLength(36);
            entity.Property(x => x.ChoiceType).HasMaxLength(40);
            entity.Property(x => x.ChosenAbility).HasMaxLength(24);
            entity.Property(x => x.ChosenFeatId).HasMaxLength(64);
            entity.HasIndex(x => new { x.CharacterId, x.Level, x.IsConfirmed });
            entity.HasOne<CharacterSheetEntity>()
                .WithMany()
                .HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Automatic Spell Grant Entities
        modelBuilder.Entity<ClassSpellGrantEntity>(entity =>
        {
            entity.ToTable("class_spell_grant");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.ClassId).HasMaxLength(64);
            entity.Property(x => x.Edition).HasMaxLength(4);
            entity.Property(x => x.SpellId).HasMaxLength(64);
            entity.Property(x => x.SourceDescription).HasMaxLength(200);
            entity.HasIndex(x => new { x.ClassId, x.Edition });
        });

        modelBuilder.Entity<SubclassSpellGrantEntity>(entity =>
        {
            entity.ToTable("subclass_spell_grant");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.SubclassId).HasMaxLength(64);
            entity.Property(x => x.Edition).HasMaxLength(4);
            entity.Property(x => x.SpellId).HasMaxLength(64);
            entity.Property(x => x.SourceDescription).HasMaxLength(200);
            entity.HasIndex(x => new { x.SubclassId, x.Edition });
        });

        modelBuilder.Entity<RaceSpellGrantEntity>(entity =>
        {
            entity.ToTable("race_spell_grant");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.RaceOrSpeciesId).HasMaxLength(64);
            entity.Property(x => x.Edition).HasMaxLength(4);
            entity.Property(x => x.SpellId).HasMaxLength(64);
            entity.Property(x => x.SourceDescription).HasMaxLength(200);
            entity.HasIndex(x => new { x.RaceOrSpeciesId, x.Edition });
        });

        modelBuilder.Entity<BackgroundSpellGrantEntity>(entity =>
        {
            entity.ToTable("background_spell_grant");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.BackgroundId).HasMaxLength(64);
            entity.Property(x => x.Edition).HasMaxLength(4);
            entity.Property(x => x.SpellId).HasMaxLength(64);
            entity.Property(x => x.SourceDescription).HasMaxLength(200);
            entity.HasIndex(x => new { x.BackgroundId, x.Edition });
        });

        modelBuilder.Entity<OriginSpellGrantEntity>(entity =>
        {
            entity.ToTable("origin_spell_grant");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.OriginId).HasMaxLength(64);
            entity.Property(x => x.SpellId).HasMaxLength(64);
            entity.Property(x => x.SourceDescription).HasMaxLength(200);
            entity.HasIndex(x => x.OriginId);
        });

        modelBuilder.Entity<FeatSpellGrantEntity>(entity =>
        {
            entity.ToTable("feat_spell_grant");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(64);
            entity.Property(x => x.FeatId).HasMaxLength(64);
            entity.Property(x => x.Edition).HasMaxLength(4);
            entity.Property(x => x.GrantType).HasMaxLength(20);
            entity.Property(x => x.SpellIdsJson).HasColumnType("TEXT");
            entity.Property(x => x.SourceDescription).HasMaxLength(200);
            entity.HasIndex(x => new { x.FeatId, x.Edition });
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
