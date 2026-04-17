# Phase 20 Raw Section Import Reference

## Implemented
1. Extended `DndApp.ContentIngestion` tool with `--import-db` mode.
2. Added database import pipeline to load `sections.json` artifacts into:
   - `source_book`
   - `source_chapter`
   - `source_section`
   - `source_block`
3. Added ingestion run/report tracking and review queue creation for low-confidence records.

## Command
```powershell
dotnet run --project tools\DndApp.ContentIngestion\DndApp.ContentIngestion.csproj -- --import-db
```
