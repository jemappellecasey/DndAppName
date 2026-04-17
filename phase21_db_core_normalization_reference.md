# Phase 21 Core Entity Normalization Reference

## Implemented
1. Extended `DndApp.ContentIngestion` with a new `--normalize-db` mode.
2. Added deterministic normalization pass from raw section tables into:
   - `rule_system`
   - `content_source`
   - `rule_module`
   - `rule_variant`
3. Added seeding of rules systems and content sources for 2014/2024 PHB/DMG datasets.
4. Added per-section module/variant payload generation with source span metadata and compatibility tags.

## Command
```powershell
dotnet run --project tools\DndApp.ContentIngestion\DndApp.ContentIngestion.csproj -- --normalize-db
```
