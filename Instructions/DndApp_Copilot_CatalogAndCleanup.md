# DndApp — Copilot handoff: catalog DB, migrations, cleanup

Pass this file to Copilot (or `@` it in Cursor) when working on **catalog data**, **EF migrations**, **ingestion**, or **SQLite cleanup** for the DndApp project.

**Workspace root (typical):**  
`C:\Users\casey\Documents\OneDrive\MyStuff\CopilotTesting`

---

## 1. What the catalog tables are for

Edition-specific player options and reference data live in `catalog_*` tables (SQLite). They are **normalized views** of content that often originates in `rule_module` plus `content_source` / `source_*` from the ingestion pipeline.

| Concept | SQLite tables |
|--------|----------------|
| Classes (2014 / 2024) | `catalog_class_2014`, `catalog_class_2024` |
| Subclasses | `catalog_subclass_2014`, `catalog_subclass_2024` (`ParentClassId` → parent class table) |
| Races (2014) | `catalog_race_2014` |
| Species (2024) | `catalog_species_2024` |
| Backgrounds | `catalog_background_2014`, `catalog_background_2024` |
| Feats | `catalog_feat_2014`, `catalog_feat_2024` |
| Items | `catalog_item_2014`, `catalog_item_2024` (`LegacyItemDefinitionId` → `item_definition.Id`) |
| Spells (both editions in one table) | `catalog_spell` (`EditionYear` = 2014 or 2024; `LegacyRuleModuleId` → `rule_module`) |

Shared columns pattern: `Id`, `ContentSourceId`, `LegacyRuleModuleId`, `Slug`, `Name`, plus edition-specific JSON payloads. See `src/DndApp.Api/Data/EditionCatalogEntities.cs` and `AppDbContext.cs` for exact shapes.

**Raw ingested modules** live in `rule_module` (and related `rule_variant`, `source_section`, `source_block`). Catalog rows are **not** a full copy of the PHB/DMG Markdown files in the repo root (`DnDPHB2014.md`, etc.) — those files are often **page-based with OCR noise**, so they are a weak direct source for machine-perfect spell lists.

---

## 2. Migrations already tied to catalog / class / spell quality

- **`20260419010751_AddClassSubclassCatalogTablesAndSpellQuality`** — Creates `catalog_class_*` and `catalog_subclass_*`, seeds them from `rule_module` + joins, and **updates** `catalog_spell` with `SourceSectionId`, `IngestionConfidence`, and `StatBlockJson` derived from `EditionPayloadJson`.
- **`CleanupJunkCatalogRows`** — File:  
  `src/DndApp.Api/Data/Migrations/20260419011649_CleanupJunkCatalogRows.cs`  
  **Data-only** migration: `DELETE`s obvious junk rows (e.g. names/slugs like “Page …”, “Chapter …”, empty titles) from all relevant `catalog_*` tables.  
  **Order:** removes matching subclasses first, then subclasses whose **parent class** is junk (FK safety), then other catalog tables.  
  **`Down`:** empty — deleted rows are **not** restored.

---

## 3. Cleanup rules (do not break characters)

The cleanup migration only deletes rows that:

1. Match **junk patterns** (page/chapter stubs, empty `Name`, bad slugs — see migration SQL), **and**
2. Are **not** referenced from:
   - `character_selected_module` (`ModuleId` vs catalog `Id` or `LegacyRuleModuleId`)
   - `character_class_level` / `character_sheet` (`ClassModuleId`)
   - `character_spell_entry` (`SpellModuleId` vs spell `Id` or `LegacyRuleModuleId`)
   - `character_inventory_item` (`ItemDefinitionId` vs `LegacyItemDefinitionId` for items)

**Do not** add blind `DELETE`s that ignore these references.

---

## 4. SQL instructions file (preview + documentation)

**Path:**  
`src/DndApp.Api/Data/Sql/CleanupIncorrectCatalogData.sql`

Contains: table mapping notes, example **preview `SELECT`s**, and warnings about optional manual cleanup of `rule_module`, `source_*`, and `item_definition`. The **authoritative DELETE text** for the automated cleanup is the C# migration above (keep them in sync if you change logic).

---

## 5. PHB/DMG Markdown and “all spells”

- Repo root PHB/DMG `.md` files are **not** a reliable single source for **complete, correct spell text** without a dedicated parser or **curated JSON** (e.g. SRD or licensed data).
- **Legal:** Avoid bulk-copying full non-SRD spell text into the database unless the project has rights to that material.
- Sensible follow-ups: extend **`tools/DndApp.ContentIngestion`**, add a **seed JSON** + migration `INSERT` into `catalog_spell`, or improve `rule_module` quality upstream.

---

## 6. What Copilot should do when asked to “finish” catalog work

1. Prefer **EF migrations** in `src/DndApp.Api/Data/Migrations/` that match existing naming and use `migrationBuilder.Sql()` for bulk data when appropriate.
2. Keep **`AppDbContextModelSnapshot.cs`** consistent with `dotnet ef migrations add` (do not hand-edit snapshot unless you know the project convention).
3. After schema/data changes, **`dotnet build`** the API project; run tests if catalog or character code paths change.
4. **Map new content** to the correct `catalog_*` table and wire `LegacyRuleModuleId` / `ContentSourceId` consistently with existing rows.
5. Any new cleanup: **mirror** the guard pattern in `CleanupJunkCatalogRows` so live characters are not orphaned.

---

## 7. App startup

`Program.cs` calls `db.Database.Migrate()` — new migrations apply on run when using the dev SQLite database.

---

*Generated for handoff to Copilot; update this file when migrations or table names change.*
