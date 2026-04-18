using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSplitEditionCatalogTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalog_background_2014",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ContentSourceId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    LegacyRuleModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    SkillProficienciesJson = table.Column<string>(type: "TEXT", nullable: false),
                    ToolProficienciesJson = table.Column<string>(type: "TEXT", nullable: false),
                    LanguageChoicesJson = table.Column<string>(type: "TEXT", nullable: false),
                    EquipmentJson = table.Column<string>(type: "TEXT", nullable: false),
                    EditionPayloadJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_background_2014", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_background_2024",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ContentSourceId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    LegacyRuleModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    SkillProficienciesJson = table.Column<string>(type: "TEXT", nullable: false),
                    ToolProficienciesJson = table.Column<string>(type: "TEXT", nullable: false),
                    LanguageChoicesJson = table.Column<string>(type: "TEXT", nullable: false),
                    GrantedFeatSlug = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    EditionPayloadJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_background_2024", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_feat_2014",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ContentSourceId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    LegacyRuleModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PrerequisitesJson = table.Column<string>(type: "TEXT", nullable: false),
                    EditionPayloadJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_feat_2014", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_feat_2024",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ContentSourceId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    LegacyRuleModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    PrerequisitesJson = table.Column<string>(type: "TEXT", nullable: false),
                    EditionPayloadJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_feat_2024", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_item_2014",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ContentSourceId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    LegacyRuleModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    LegacyItemDefinitionId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    ItemType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Rarity = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    RequiresAttunement = table.Column<bool>(type: "INTEGER", nullable: false),
                    GoldValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsWeapon = table.Column<bool>(type: "INTEGER", nullable: false),
                    DamageDice = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    WeaponAbility = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    AttackBonus = table.Column<int>(type: "INTEGER", nullable: false),
                    DamageBonus = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    EditionPayloadJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_item_2014", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_item_2024",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ContentSourceId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    LegacyRuleModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    LegacyItemDefinitionId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    ItemType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Rarity = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    RequiresAttunement = table.Column<bool>(type: "INTEGER", nullable: false),
                    GoldValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsWeapon = table.Column<bool>(type: "INTEGER", nullable: false),
                    DamageDice = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    WeaponAbility = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    AttackBonus = table.Column<int>(type: "INTEGER", nullable: false),
                    DamageBonus = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    EditionPayloadJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_item_2024", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_race_2014",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ContentSourceId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    LegacyRuleModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    IsSubrace = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParentRaceSlug = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    AbilityBonusesJson = table.Column<string>(type: "TEXT", nullable: false),
                    LanguagesJson = table.Column<string>(type: "TEXT", nullable: false),
                    TraitsJson = table.Column<string>(type: "TEXT", nullable: false),
                    EditionPayloadJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_race_2014", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_species_2024",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ContentSourceId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    LegacyRuleModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    AbilityBonusesJson = table.Column<string>(type: "TEXT", nullable: false),
                    LanguagesJson = table.Column<string>(type: "TEXT", nullable: false),
                    TraitsJson = table.Column<string>(type: "TEXT", nullable: false),
                    EditionPayloadJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_species_2024", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_spell_2014",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ContentSourceId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    LegacyRuleModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    School = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CastingTime = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    RangeText = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Duration = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Ritual = table.Column<bool>(type: "INTEGER", nullable: false),
                    Concentration = table.Column<bool>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    EditionPayloadJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_spell_2014", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_spell_2024",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ContentSourceId = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    LegacyRuleModuleId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    School = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CastingTime = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    RangeText = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Duration = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Ritual = table.Column<bool>(type: "INTEGER", nullable: false),
                    Concentration = table.Column<bool>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    EditionPayloadJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalog_spell_2024", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_background_2014_ContentSourceId_Slug",
                table: "catalog_background_2014",
                columns: new[] { "ContentSourceId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_background_2014_LegacyRuleModuleId",
                table: "catalog_background_2014",
                column: "LegacyRuleModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_background_2024_ContentSourceId_Slug",
                table: "catalog_background_2024",
                columns: new[] { "ContentSourceId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_background_2024_LegacyRuleModuleId",
                table: "catalog_background_2024",
                column: "LegacyRuleModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_feat_2014_ContentSourceId_Slug",
                table: "catalog_feat_2014",
                columns: new[] { "ContentSourceId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_feat_2014_LegacyRuleModuleId",
                table: "catalog_feat_2014",
                column: "LegacyRuleModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_feat_2024_ContentSourceId_Slug",
                table: "catalog_feat_2024",
                columns: new[] { "ContentSourceId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_feat_2024_LegacyRuleModuleId",
                table: "catalog_feat_2024",
                column: "LegacyRuleModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_item_2014_ContentSourceId_Slug",
                table: "catalog_item_2014",
                columns: new[] { "ContentSourceId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_item_2014_LegacyItemDefinitionId",
                table: "catalog_item_2014",
                column: "LegacyItemDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_item_2014_LegacyRuleModuleId",
                table: "catalog_item_2014",
                column: "LegacyRuleModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_item_2024_ContentSourceId_Slug",
                table: "catalog_item_2024",
                columns: new[] { "ContentSourceId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_item_2024_LegacyItemDefinitionId",
                table: "catalog_item_2024",
                column: "LegacyItemDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_item_2024_LegacyRuleModuleId",
                table: "catalog_item_2024",
                column: "LegacyRuleModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_race_2014_ContentSourceId_Slug",
                table: "catalog_race_2014",
                columns: new[] { "ContentSourceId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_race_2014_LegacyRuleModuleId",
                table: "catalog_race_2014",
                column: "LegacyRuleModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_species_2024_ContentSourceId_Slug",
                table: "catalog_species_2024",
                columns: new[] { "ContentSourceId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_species_2024_LegacyRuleModuleId",
                table: "catalog_species_2024",
                column: "LegacyRuleModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_spell_2014_ContentSourceId_Slug",
                table: "catalog_spell_2014",
                columns: new[] { "ContentSourceId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_spell_2014_LegacyRuleModuleId",
                table: "catalog_spell_2014",
                column: "LegacyRuleModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_spell_2014_Level_Name",
                table: "catalog_spell_2014",
                columns: new[] { "Level", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_spell_2024_ContentSourceId_Slug",
                table: "catalog_spell_2024",
                columns: new[] { "ContentSourceId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalog_spell_2024_LegacyRuleModuleId",
                table: "catalog_spell_2024",
                column: "LegacyRuleModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_catalog_spell_2024_Level_Name",
                table: "catalog_spell_2024",
                columns: new[] { "Level", "Name" });

            migrationBuilder.Sql(
                """
                INSERT INTO catalog_race_2014 (
                    Id, ContentSourceId, LegacyRuleModuleId, Slug, Name, IsSubrace, ParentRaceSlug,
                    Description, AbilityBonusesJson, LanguagesJson, TraitsJson, EditionPayloadJson
                )
                SELECT
                    m.Id,
                    m.ContentSourceId,
                    m.Id,
                    m.Slug,
                    m.DisplayName,
                    CASE WHEN lower(m.ModuleType) = 'subrace' THEN 1 ELSE 0 END,
                    COALESCE(json_extract(rv.PayloadJson, '$.parentRaceSlug'), ''),
                    COALESCE(json_extract(rv.PayloadJson, '$.description'), m.DisplayName),
                    COALESCE(json_extract(rv.PayloadJson, '$.abilityBonuses'), '{}'),
                    COALESCE(json_extract(rv.PayloadJson, '$.languages'), '[]'),
                    COALESCE(json_extract(rv.PayloadJson, '$.traits'), '[]'),
                    COALESCE(rv.PayloadJson, '{}')
                FROM rule_module m
                JOIN content_source s ON s.Id = m.ContentSourceId
                LEFT JOIN rule_variant rv ON rv.RuleModuleId = m.Id
                WHERE s.RuleSystemId = 'rules-2014'
                  AND lower(m.ModuleType) IN ('race', 'subrace');
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO catalog_species_2024 (
                    Id, ContentSourceId, LegacyRuleModuleId, Slug, Name,
                    Description, AbilityBonusesJson, LanguagesJson, TraitsJson, EditionPayloadJson
                )
                SELECT
                    m.Id,
                    m.ContentSourceId,
                    m.Id,
                    m.Slug,
                    m.DisplayName,
                    COALESCE(json_extract(rv.PayloadJson, '$.description'), m.DisplayName),
                    COALESCE(json_extract(rv.PayloadJson, '$.abilityBonuses'), '{}'),
                    COALESCE(json_extract(rv.PayloadJson, '$.languages'), '[]'),
                    COALESCE(json_extract(rv.PayloadJson, '$.traits'), '[]'),
                    COALESCE(rv.PayloadJson, '{}')
                FROM rule_module m
                JOIN content_source s ON s.Id = m.ContentSourceId
                LEFT JOIN rule_variant rv ON rv.RuleModuleId = m.Id
                WHERE s.RuleSystemId = 'rules-2024'
                  AND lower(m.ModuleType) IN ('species', 'race');
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO catalog_background_2014 (
                    Id, ContentSourceId, LegacyRuleModuleId, Slug, Name,
                    Description, SkillProficienciesJson, ToolProficienciesJson, LanguageChoicesJson, EquipmentJson, EditionPayloadJson
                )
                SELECT
                    m.Id,
                    m.ContentSourceId,
                    m.Id,
                    m.Slug,
                    m.DisplayName,
                    COALESCE(json_extract(rv.PayloadJson, '$.description'), m.DisplayName),
                    COALESCE(json_extract(rv.PayloadJson, '$.fixedSkillProficiencies'), '[]'),
                    COALESCE(json_extract(rv.PayloadJson, '$.fixedToolProficiencies'), '[]'),
                    COALESCE(json_extract(rv.PayloadJson, '$.languageChoices'), '[]'),
                    COALESCE(json_extract(rv.PayloadJson, '$.equipment'), '[]'),
                    COALESCE(rv.PayloadJson, '{}')
                FROM rule_module m
                JOIN content_source s ON s.Id = m.ContentSourceId
                LEFT JOIN rule_variant rv ON rv.RuleModuleId = m.Id
                WHERE s.RuleSystemId = 'rules-2014'
                  AND lower(m.ModuleType) IN ('background', 'origin');
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO catalog_background_2024 (
                    Id, ContentSourceId, LegacyRuleModuleId, Slug, Name,
                    Description, SkillProficienciesJson, ToolProficienciesJson, LanguageChoicesJson, GrantedFeatSlug, EditionPayloadJson
                )
                SELECT
                    m.Id,
                    m.ContentSourceId,
                    m.Id,
                    m.Slug,
                    m.DisplayName,
                    COALESCE(json_extract(rv.PayloadJson, '$.description'), m.DisplayName),
                    COALESCE(json_extract(rv.PayloadJson, '$.fixedSkillProficiencies'), '[]'),
                    COALESCE(json_extract(rv.PayloadJson, '$.fixedToolProficiencies'), '[]'),
                    COALESCE(json_extract(rv.PayloadJson, '$.languageChoices'), '[]'),
                    COALESCE(json_extract(rv.PayloadJson, '$.grantedFeatSlug'), ''),
                    COALESCE(rv.PayloadJson, '{}')
                FROM rule_module m
                JOIN content_source s ON s.Id = m.ContentSourceId
                LEFT JOIN rule_variant rv ON rv.RuleModuleId = m.Id
                WHERE s.RuleSystemId = 'rules-2024'
                  AND lower(m.ModuleType) IN ('background', 'origin');
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO catalog_feat_2014 (
                    Id, ContentSourceId, LegacyRuleModuleId, Slug, Name, Description, PrerequisitesJson, EditionPayloadJson
                )
                SELECT
                    m.Id,
                    m.ContentSourceId,
                    m.Id,
                    m.Slug,
                    m.DisplayName,
                    COALESCE(json_extract(rv.PayloadJson, '$.description'), m.DisplayName),
                    COALESCE(json_extract(rv.PayloadJson, '$.prerequisites'), '{}'),
                    COALESCE(rv.PayloadJson, '{}')
                FROM rule_module m
                JOIN content_source s ON s.Id = m.ContentSourceId
                LEFT JOIN rule_variant rv ON rv.RuleModuleId = m.Id
                WHERE s.RuleSystemId = 'rules-2014'
                  AND lower(m.ModuleType) = 'feat';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO catalog_feat_2024 (
                    Id, ContentSourceId, LegacyRuleModuleId, Slug, Name, Description, Category, PrerequisitesJson, EditionPayloadJson
                )
                SELECT
                    m.Id,
                    m.ContentSourceId,
                    m.Id,
                    m.Slug,
                    m.DisplayName,
                    COALESCE(json_extract(rv.PayloadJson, '$.description'), m.DisplayName),
                    COALESCE(json_extract(rv.PayloadJson, '$.category'), ''),
                    COALESCE(json_extract(rv.PayloadJson, '$.prerequisites'), '{}'),
                    COALESCE(rv.PayloadJson, '{}')
                FROM rule_module m
                JOIN content_source s ON s.Id = m.ContentSourceId
                LEFT JOIN rule_variant rv ON rv.RuleModuleId = m.Id
                WHERE s.RuleSystemId = 'rules-2024'
                  AND lower(m.ModuleType) = 'feat';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO catalog_spell_2014 (
                    Id, ContentSourceId, LegacyRuleModuleId, Slug, Name,
                    Level, School, CastingTime, RangeText, Duration, Ritual, Concentration,
                    Description, EditionPayloadJson
                )
                SELECT
                    m.Id,
                    m.ContentSourceId,
                    m.Id,
                    m.Slug,
                    m.DisplayName,
                    COALESCE(CAST(json_extract(rv.PayloadJson, '$.level') AS INTEGER), 0),
                    COALESCE(json_extract(rv.PayloadJson, '$.school'), ''),
                    COALESCE(json_extract(rv.PayloadJson, '$.castingTime'), ''),
                    COALESCE(json_extract(rv.PayloadJson, '$.range'), ''),
                    COALESCE(json_extract(rv.PayloadJson, '$.duration'), ''),
                    CASE WHEN COALESCE(CAST(json_extract(rv.PayloadJson, '$.ritual') AS INTEGER), 0) = 1 THEN 1 ELSE 0 END,
                    CASE WHEN COALESCE(CAST(json_extract(rv.PayloadJson, '$.concentration') AS INTEGER), 0) = 1 THEN 1 ELSE 0 END,
                    COALESCE(json_extract(rv.PayloadJson, '$.description'), m.DisplayName),
                    COALESCE(rv.PayloadJson, '{}')
                FROM rule_module m
                JOIN content_source s ON s.Id = m.ContentSourceId
                LEFT JOIN rule_variant rv ON rv.RuleModuleId = m.Id
                WHERE s.RuleSystemId = 'rules-2014'
                  AND lower(m.ModuleType) = 'spell';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO catalog_spell_2024 (
                    Id, ContentSourceId, LegacyRuleModuleId, Slug, Name,
                    Level, School, CastingTime, RangeText, Duration, Ritual, Concentration,
                    Description, EditionPayloadJson
                )
                SELECT
                    m.Id,
                    m.ContentSourceId,
                    m.Id,
                    m.Slug,
                    m.DisplayName,
                    COALESCE(CAST(json_extract(rv.PayloadJson, '$.level') AS INTEGER), 0),
                    COALESCE(json_extract(rv.PayloadJson, '$.school'), ''),
                    COALESCE(json_extract(rv.PayloadJson, '$.castingTime'), ''),
                    COALESCE(json_extract(rv.PayloadJson, '$.range'), ''),
                    COALESCE(json_extract(rv.PayloadJson, '$.duration'), ''),
                    CASE WHEN COALESCE(CAST(json_extract(rv.PayloadJson, '$.ritual') AS INTEGER), 0) = 1 THEN 1 ELSE 0 END,
                    CASE WHEN COALESCE(CAST(json_extract(rv.PayloadJson, '$.concentration') AS INTEGER), 0) = 1 THEN 1 ELSE 0 END,
                    COALESCE(json_extract(rv.PayloadJson, '$.description'), m.DisplayName),
                    COALESCE(rv.PayloadJson, '{}')
                FROM rule_module m
                JOIN content_source s ON s.Id = m.ContentSourceId
                LEFT JOIN rule_variant rv ON rv.RuleModuleId = m.Id
                WHERE s.RuleSystemId = 'rules-2024'
                  AND lower(m.ModuleType) = 'spell';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO catalog_item_2014 (
                    Id, ContentSourceId, LegacyRuleModuleId, LegacyItemDefinitionId, Slug, Name,
                    ItemType, Rarity, RequiresAttunement, GoldValue, Weight, IsWeapon,
                    DamageDice, WeaponAbility, AttackBonus, DamageBonus, Description, EditionPayloadJson
                )
                SELECT
                    i.Id,
                    m.ContentSourceId,
                    m.Id,
                    i.Id,
                    m.Slug,
                    m.DisplayName,
                    i.ItemType,
                    i.Rarity,
                    i.RequiresAttunement,
                    i.GoldValue,
                    i.Weight,
                    i.IsWeapon,
                    i.DamageDice,
                    i.WeaponAbility,
                    i.AttackBonus,
                    i.DamageBonus,
                    m.DisplayName,
                    COALESCE(rv.PayloadJson, '{}')
                FROM item_definition i
                JOIN rule_module m ON m.Id = i.RuleModuleId
                JOIN content_source s ON s.Id = m.ContentSourceId
                LEFT JOIN rule_variant rv ON rv.RuleModuleId = m.Id
                WHERE s.RuleSystemId = 'rules-2014';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO catalog_item_2024 (
                    Id, ContentSourceId, LegacyRuleModuleId, LegacyItemDefinitionId, Slug, Name,
                    ItemType, Rarity, RequiresAttunement, GoldValue, Weight, IsWeapon,
                    DamageDice, WeaponAbility, AttackBonus, DamageBonus, Description, EditionPayloadJson
                )
                SELECT
                    i.Id,
                    m.ContentSourceId,
                    m.Id,
                    i.Id,
                    m.Slug,
                    m.DisplayName,
                    i.ItemType,
                    i.Rarity,
                    i.RequiresAttunement,
                    i.GoldValue,
                    i.Weight,
                    i.IsWeapon,
                    i.DamageDice,
                    i.WeaponAbility,
                    i.AttackBonus,
                    i.DamageBonus,
                    m.DisplayName,
                    COALESCE(rv.PayloadJson, '{}')
                FROM item_definition i
                JOIN rule_module m ON m.Id = i.RuleModuleId
                JOIN content_source s ON s.Id = m.ContentSourceId
                LEFT JOIN rule_variant rv ON rv.RuleModuleId = m.Id
                WHERE s.RuleSystemId = 'rules-2024';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalog_background_2014");

            migrationBuilder.DropTable(
                name: "catalog_background_2024");

            migrationBuilder.DropTable(
                name: "catalog_feat_2014");

            migrationBuilder.DropTable(
                name: "catalog_feat_2024");

            migrationBuilder.DropTable(
                name: "catalog_item_2014");

            migrationBuilder.DropTable(
                name: "catalog_item_2024");

            migrationBuilder.DropTable(
                name: "catalog_race_2014");

            migrationBuilder.DropTable(
                name: "catalog_species_2024");

            migrationBuilder.DropTable(
                name: "catalog_spell_2014");

            migrationBuilder.DropTable(
                name: "catalog_spell_2024");
        }
    }
}
