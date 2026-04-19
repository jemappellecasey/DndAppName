using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DndApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CleanupJunkCatalogRows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Removes obvious OCR stubs (e.g. "Page 12") and empty titles from edition
            // catalog tables. Each DELETE is correlated and skips rows still referenced
            // from character sheets (ModuleId / SpellModuleId / ItemDefinitionId).
            // Subclasses are removed before classes in case a junk row ever had children.

            migrationBuilder.Sql(
                """
                DELETE FROM catalog_subclass_2014
                WHERE (
                    LOWER(Name) LIKE 'page %' OR LOWER(Name) LIKE 'chapter %' OR TRIM(Name) = ''
                    OR LOWER(Slug) LIKE 'page-%' OR LOWER(Slug) LIKE 'chapter-%'
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_selected_module m
                    WHERE m.ModuleId = catalog_subclass_2014.Id OR m.ModuleId = catalog_subclass_2014.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_class_level c
                    WHERE c.ClassModuleId = catalog_subclass_2014.Id OR c.ClassModuleId = catalog_subclass_2014.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_sheet s
                    WHERE s.ClassModuleId = catalog_subclass_2014.Id OR s.ClassModuleId = catalog_subclass_2014.LegacyRuleModuleId
                );
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM catalog_subclass_2024
                WHERE (
                    LOWER(Name) LIKE 'page %' OR LOWER(Name) LIKE 'chapter %' OR TRIM(Name) = ''
                    OR LOWER(Slug) LIKE 'page-%' OR LOWER(Slug) LIKE 'chapter-%'
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_selected_module m
                    WHERE m.ModuleId = catalog_subclass_2024.Id OR m.ModuleId = catalog_subclass_2024.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_class_level c
                    WHERE c.ClassModuleId = catalog_subclass_2024.Id OR c.ClassModuleId = catalog_subclass_2024.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_sheet s
                    WHERE s.ClassModuleId = catalog_subclass_2024.Id OR s.ClassModuleId = catalog_subclass_2024.LegacyRuleModuleId
                );
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM catalog_subclass_2014
                WHERE ParentClassId IN (
                    SELECT c.Id FROM catalog_class_2014 c
                    WHERE (
                        LOWER(c.Name) LIKE 'page %' OR LOWER(c.Name) LIKE 'chapter %' OR TRIM(c.Name) = ''
                        OR LOWER(c.Slug) LIKE 'page-%' OR LOWER(c.Slug) LIKE 'chapter-%'
                    )
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_selected_module m
                    WHERE m.ModuleId = catalog_subclass_2014.Id OR m.ModuleId = catalog_subclass_2014.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_class_level c
                    WHERE c.ClassModuleId = catalog_subclass_2014.Id OR c.ClassModuleId = catalog_subclass_2014.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_sheet s
                    WHERE s.ClassModuleId = catalog_subclass_2014.Id OR s.ClassModuleId = catalog_subclass_2014.LegacyRuleModuleId
                );
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM catalog_subclass_2024
                WHERE ParentClassId IN (
                    SELECT c.Id FROM catalog_class_2024 c
                    WHERE (
                        LOWER(c.Name) LIKE 'page %' OR LOWER(c.Name) LIKE 'chapter %' OR TRIM(c.Name) = ''
                        OR LOWER(c.Slug) LIKE 'page-%' OR LOWER(c.Slug) LIKE 'chapter-%'
                    )
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_selected_module m
                    WHERE m.ModuleId = catalog_subclass_2024.Id OR m.ModuleId = catalog_subclass_2024.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_class_level c
                    WHERE c.ClassModuleId = catalog_subclass_2024.Id OR c.ClassModuleId = catalog_subclass_2024.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_sheet s
                    WHERE s.ClassModuleId = catalog_subclass_2024.Id OR s.ClassModuleId = catalog_subclass_2024.LegacyRuleModuleId
                );
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM catalog_class_2014
                WHERE (
                    LOWER(Name) LIKE 'page %' OR LOWER(Name) LIKE 'chapter %' OR TRIM(Name) = ''
                    OR LOWER(Slug) LIKE 'page-%' OR LOWER(Slug) LIKE 'chapter-%'
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_selected_module m
                    WHERE m.ModuleId = catalog_class_2014.Id OR m.ModuleId = catalog_class_2014.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_class_level c
                    WHERE c.ClassModuleId = catalog_class_2014.Id OR c.ClassModuleId = catalog_class_2014.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_sheet s
                    WHERE s.ClassModuleId = catalog_class_2014.Id OR s.ClassModuleId = catalog_class_2014.LegacyRuleModuleId
                );
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM catalog_class_2024
                WHERE (
                    LOWER(Name) LIKE 'page %' OR LOWER(Name) LIKE 'chapter %' OR TRIM(Name) = ''
                    OR LOWER(Slug) LIKE 'page-%' OR LOWER(Slug) LIKE 'chapter-%'
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_selected_module m
                    WHERE m.ModuleId = catalog_class_2024.Id OR m.ModuleId = catalog_class_2024.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_class_level c
                    WHERE c.ClassModuleId = catalog_class_2024.Id OR c.ClassModuleId = catalog_class_2024.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_sheet s
                    WHERE s.ClassModuleId = catalog_class_2024.Id OR s.ClassModuleId = catalog_class_2024.LegacyRuleModuleId
                );
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM catalog_race_2014
                WHERE (
                    LOWER(Name) LIKE 'page %' OR LOWER(Name) LIKE 'chapter %' OR TRIM(Name) = ''
                    OR LOWER(Slug) LIKE 'page-%' OR LOWER(Slug) LIKE 'chapter-%'
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_selected_module m
                    WHERE m.ModuleId = catalog_race_2014.Id OR m.ModuleId = catalog_race_2014.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_class_level c
                    WHERE c.ClassModuleId = catalog_race_2014.Id OR c.ClassModuleId = catalog_race_2014.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_sheet s
                    WHERE s.ClassModuleId = catalog_race_2014.Id OR s.ClassModuleId = catalog_race_2014.LegacyRuleModuleId
                );
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM catalog_species_2024
                WHERE (
                    LOWER(Name) LIKE 'page %' OR LOWER(Name) LIKE 'chapter %' OR TRIM(Name) = ''
                    OR LOWER(Slug) LIKE 'page-%' OR LOWER(Slug) LIKE 'chapter-%'
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_selected_module m
                    WHERE m.ModuleId = catalog_species_2024.Id OR m.ModuleId = catalog_species_2024.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_class_level c
                    WHERE c.ClassModuleId = catalog_species_2024.Id OR c.ClassModuleId = catalog_species_2024.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_sheet s
                    WHERE s.ClassModuleId = catalog_species_2024.Id OR s.ClassModuleId = catalog_species_2024.LegacyRuleModuleId
                );
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM catalog_background_2014
                WHERE (
                    LOWER(Name) LIKE 'page %' OR LOWER(Name) LIKE 'chapter %' OR TRIM(Name) = ''
                    OR LOWER(Slug) LIKE 'page-%' OR LOWER(Slug) LIKE 'chapter-%'
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_selected_module m
                    WHERE m.ModuleId = catalog_background_2014.Id OR m.ModuleId = catalog_background_2014.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_class_level c
                    WHERE c.ClassModuleId = catalog_background_2014.Id OR c.ClassModuleId = catalog_background_2014.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_sheet s
                    WHERE s.ClassModuleId = catalog_background_2014.Id OR s.ClassModuleId = catalog_background_2014.LegacyRuleModuleId
                );
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM catalog_background_2024
                WHERE (
                    LOWER(Name) LIKE 'page %' OR LOWER(Name) LIKE 'chapter %' OR TRIM(Name) = ''
                    OR LOWER(Slug) LIKE 'page-%' OR LOWER(Slug) LIKE 'chapter-%'
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_selected_module m
                    WHERE m.ModuleId = catalog_background_2024.Id OR m.ModuleId = catalog_background_2024.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_class_level c
                    WHERE c.ClassModuleId = catalog_background_2024.Id OR c.ClassModuleId = catalog_background_2024.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_sheet s
                    WHERE s.ClassModuleId = catalog_background_2024.Id OR s.ClassModuleId = catalog_background_2024.LegacyRuleModuleId
                );
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM catalog_feat_2014
                WHERE (
                    LOWER(Name) LIKE 'page %' OR LOWER(Name) LIKE 'chapter %' OR TRIM(Name) = ''
                    OR LOWER(Slug) LIKE 'page-%' OR LOWER(Slug) LIKE 'chapter-%'
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_selected_module m
                    WHERE m.ModuleId = catalog_feat_2014.Id OR m.ModuleId = catalog_feat_2014.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_class_level c
                    WHERE c.ClassModuleId = catalog_feat_2014.Id OR c.ClassModuleId = catalog_feat_2014.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_sheet s
                    WHERE s.ClassModuleId = catalog_feat_2014.Id OR s.ClassModuleId = catalog_feat_2014.LegacyRuleModuleId
                );
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM catalog_feat_2024
                WHERE (
                    LOWER(Name) LIKE 'page %' OR LOWER(Name) LIKE 'chapter %' OR TRIM(Name) = ''
                    OR LOWER(Slug) LIKE 'page-%' OR LOWER(Slug) LIKE 'chapter-%'
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_selected_module m
                    WHERE m.ModuleId = catalog_feat_2024.Id OR m.ModuleId = catalog_feat_2024.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_class_level c
                    WHERE c.ClassModuleId = catalog_feat_2024.Id OR c.ClassModuleId = catalog_feat_2024.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_sheet s
                    WHERE s.ClassModuleId = catalog_feat_2024.Id OR s.ClassModuleId = catalog_feat_2024.LegacyRuleModuleId
                );
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM catalog_item_2014
                WHERE (
                    LOWER(Name) LIKE 'page %' OR LOWER(Name) LIKE 'chapter %' OR TRIM(Name) = ''
                    OR LOWER(Slug) LIKE 'page-%' OR LOWER(Slug) LIKE 'chapter-%'
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_inventory_item i
                    WHERE i.ItemDefinitionId = catalog_item_2014.LegacyItemDefinitionId AND IFNULL(TRIM(catalog_item_2014.LegacyItemDefinitionId), '') <> ''
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_selected_module m
                    WHERE m.ModuleId = catalog_item_2014.Id OR m.ModuleId = catalog_item_2014.LegacyRuleModuleId
                );
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM catalog_item_2024
                WHERE (
                    LOWER(Name) LIKE 'page %' OR LOWER(Name) LIKE 'chapter %' OR TRIM(Name) = ''
                    OR LOWER(Slug) LIKE 'page-%' OR LOWER(Slug) LIKE 'chapter-%'
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_inventory_item i
                    WHERE i.ItemDefinitionId = catalog_item_2024.LegacyItemDefinitionId AND IFNULL(TRIM(catalog_item_2024.LegacyItemDefinitionId), '') <> ''
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_selected_module m
                    WHERE m.ModuleId = catalog_item_2024.Id OR m.ModuleId = catalog_item_2024.LegacyRuleModuleId
                );
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM catalog_spell
                WHERE (
                    LOWER(Name) LIKE 'page %' OR LOWER(Name) LIKE 'chapter %' OR TRIM(Name) = ''
                    OR LOWER(Slug) LIKE 'page-%' OR LOWER(Slug) LIKE 'chapter-%'
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_spell_entry e
                    WHERE e.SpellModuleId = catalog_spell.Id OR e.SpellModuleId = catalog_spell.LegacyRuleModuleId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM character_selected_module m
                    WHERE m.ModuleId = catalog_spell.Id OR m.ModuleId = catalog_spell.LegacyRuleModuleId
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data-only migration; removed rows cannot be restored here.
        }
    }
}
