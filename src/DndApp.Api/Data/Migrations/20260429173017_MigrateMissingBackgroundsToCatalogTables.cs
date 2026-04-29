using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MigrateMissingBackgroundsToCatalogTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration identifies backgrounds in the legacy rule_module table that are not yet
            // present in the split edition catalog tables (catalog_background_2014, catalog_background_2024).
            // It then inserts those missing backgrounds into the appropriate split table based on their
            // RuleSystemId, ensuring 100% catalog coverage.

            migrationBuilder.Sql(
                """
                -- Insert missing backgrounds from rule_module into catalog_background_2014
                -- These are backgrounds that exist in legacy tables but weren't yet migrated to split tables
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
                  AND lower(m.ModuleType) IN ('background', 'origin')
                  AND m.Id NOT IN (SELECT LegacyRuleModuleId FROM catalog_background_2014)
                """);

            migrationBuilder.Sql(
                """
                -- Insert missing backgrounds from rule_module into catalog_background_2024
                -- These are backgrounds that exist in legacy tables but weren't yet migrated to split tables
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
                  AND lower(m.ModuleType) IN ('background', 'origin')
                  AND m.Id NOT IN (SELECT LegacyRuleModuleId FROM catalog_background_2024)
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This migration only inserts rows; removal would require identifying which rows
            // were added by this migration vs. the initial population. Since this is a data
            // migration that ensures completeness, Down() is not implemented.
        }
    }
}
