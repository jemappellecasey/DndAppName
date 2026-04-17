# Phase 19 Ingestion Ops Schema Reference

## Implemented tables
1. `ingestion_run`
2. `review_queue`
3. `correction_override`
4. `import_report`

## Scope covered
1. Ingestion run tracking with source/version/status/checksum metadata.
2. Review queue for low-confidence/unresolved records.
3. Correction override persistence with actor/timestamp audit.
4. Import reporting table for deterministic run summaries.
