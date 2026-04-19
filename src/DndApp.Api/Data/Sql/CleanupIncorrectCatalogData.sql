-- CleanupIncorrectCatalogData.sql
-- -----------------------------------------------------------------------------
-- Purpose: Identify and remove ingested catalog rows that are clearly not real
-- game entities (OCR page headers, empty titles, etc.).
--
-- Content mapping (PHB/DMG markdown -> SQLite tables):
--   rule_module + source_*     : raw ingested modules (spell, class, item, ...)
--   catalog_class_2014 / _2024 : normalized player classes per edition
--   catalog_subclass_2014/_2024: subclasses; ParentClassId -> catalog_class_*
--   catalog_race_2014          : 2014 races/subraces
--   catalog_species_2024       : 2024 species
--   catalog_background_2014/_2024
--   catalog_feat_2014 / _2024
--   catalog_item_2014 / _2024  : LegacyItemDefinitionId -> item_definition.Id
--   catalog_spell              : merged 2014+2024; LegacyRuleModuleId -> rule_module;
--                                optional SourceSectionId / IngestionConfidence
--
-- Character guards: before DELETE, run the SELECT preview blocks below. Deletes
-- are skipped in application code when ModuleId / SpellModuleId / ItemDefinitionId
-- still references a row (see migration CleanupJunkCatalogRows).
--
-- Backup: copy the SQLite file (e.g. dndapp-dev.sqlite) before running DELETEs.
-- -----------------------------------------------------------------------------

-- === Preview: rows that look like page/chapter stubs (adjust patterns as needed) ===

-- SELECT 'catalog_spell' AS tbl, Id, Name, Slug, IngestionConfidence FROM catalog_spell
-- WHERE (LOWER(Name) LIKE 'page %' OR LOWER(Name) LIKE 'chapter %' OR TRIM(Name) = ''
--        OR LOWER(Slug) LIKE 'page-%' OR LOWER(Slug) LIKE 'chapter-%');

-- SELECT 'catalog_class_2014' AS tbl, Id, Name FROM catalog_class_2014
-- WHERE (LOWER(Name) LIKE 'page %' OR TRIM(Name) = '');

-- (Repeat for catalog_class_2024, catalog_subclass_*, catalog_race_2014,
--  catalog_species_2024, catalog_background_*, catalog_feat_*, catalog_item_*)

-- === Optional: find catalog_spell rows with no source section (review before delete) ===
-- SELECT Id, Name, IngestionConfidence, SourceSectionId FROM catalog_spell
-- WHERE (SourceSectionId IS NULL OR SourceSectionId = '');

-- -----------------------------------------------------------------------------
-- DELETE statements: verbatim copy of Up() in
--   Data/Migrations/20260419011649_CleanupJunkCatalogRows.cs
-- Run SELECT previews first; keep a DB backup. Down() does not restore rows.
-- -----------------------------------------------------------------------------

-- -----------------------------------------------------------------------------
-- Optional manual cleanup (NOT in EF migration — review FKs first):
--
-- rule_module: modules that duplicate or mis-label content should be fixed at
--   source re-ingestion; deleting rule_module cascades to rule_variant and can
--   break catalog.LegacyRuleModuleId pointers.
--
-- source_section / source_block: remove only after confirming no rule_module
--   PayloadJson still references the section id.
--
-- item_definition: deleting rows that are still referenced by
--   character_inventory_item.ItemDefinitionId will fail or orphan the column;
--   update inventory rows first.
-- -----------------------------------------------------------------------------
