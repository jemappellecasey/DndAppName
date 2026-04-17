# Phase 15 Database Roadmap (SQLite now, PostgreSQL later)

## Objective
Create a production-grade data layer that can ingest and serve full PHB/DMG content reliably, using SQLite for current development and a PostgreSQL-ready architecture for deployment.

## Confirmed decisions
1. EF Core migrations and provider switching from day one.
2. Store both raw source text and normalized gameplay entities.
3. Keep full raw text local/dev-only by default.

## Implementation breakdown
1. **EF Core foundation**
   - Add `DbContext`, configuration, and migration project wiring.
   - Configure SQLite connection string in development.
   - Add PostgreSQL provider package/config placeholders.
2. **Schema creation**
   - Raw source tables: books, chapters, sections, source_blocks.
   - Normalized rule tables: rule systems, modules, variants, prerequisites, item effects, etc.
   - Operational tables: ingestion_run, review_queue, corrections, import_reports.
3. **Import pipeline persistence**
   - Persist `data\ingested\*\v1\sections.json` into raw tables.
   - Normalize raw rows into domain entities.
   - Track confidence, unresolved rows, and correction lifecycle.
4. **Data integrity and quality controls**
   - Uniqueness constraints for canonical slugs/source-version pairs.
   - Referential integrity for rule dependencies and effects.
   - Repeatable import checksums and run metadata.
5. **PostgreSQL cutover readiness**
   - Keep SQL/provider-neutral EF patterns.
   - Validate migration generation for both providers.
   - Add environment toggle for provider at startup.

## Data model groups (minimum)
1. `rule_system`, `content_source`, `rule_module`, `rule_variant`
2. `class_feature`, `origin_background`, `species_race`, `feat`, `weapon_equipment`
3. `item_definition`, `item_effect`, `prerequisite`, `constraint`
4. `source_section`, `source_block`, `ingestion_run`, `review_queue`, `correction_override`

## Import flow
1. Run ingestion tool to produce JSON artifacts.
2. Run DB import command: raw load -> normalize -> review queue.
3. Apply correction overrides and rerun normalize stage.
4. Produce import summary report.

## Acceptance criteria
1. Fresh SQLite database can be created from migrations.
2. Full PHB/DMG section corpus imports into raw layer.
3. Core normalized entities are populated and queryable by API.
4. Low-confidence records are visible in review queue.
5. Provider switch to PostgreSQL requires config change + migrations, not schema redesign.
