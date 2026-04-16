# DndAppName

DndAppName is a D&D character creation and management app focused on 2014/2024 rules support, mixed-rules compatibility, explainable calculations, custom lineage/origin creation, and item/attunement-aware character math.

## Current v1 scope
1. .NET 10 API foundation.
2. Rulebook markdown ingestion to versioned JSON artifacts.
3. Character wizard draft flow with rules-mode constraints.
4. Mixed-rules conflict resolution with base-rules precedence and explicit overrides.
5. Custom origin/species endpoints (guided + fully custom).
6. Item effect pipeline (equipped/attuned activation).
7. Roll calculation endpoints for checks, saves, and attacks with advantage/disadvantage and optional random rolling.

## Prerequisites
1. .NET SDK 10.x
2. PowerShell (Windows)

## Quick launch
1. Restore and build:
   ```powershell
   dotnet build DndAppName.slnx -v minimal
   ```
2. Run API:
   ```powershell
   dotnet run --project src\DndApp.Api\DndApp.Api.csproj
   ```
3. Health check:
   - `GET http://localhost:5000/health` (or the port shown in console output)

## Run ingestion pipeline
Generate versioned JSON artifacts from PHB/DMG markdown sources:

```powershell
dotnet run --project tools\DndApp.ContentIngestion\DndApp.ContentIngestion.csproj
```

Outputs are written under:
- `data\ingested\phb2014\v1\sections.json`
- `data\ingested\phb2024\v1\sections.json`
- `data\ingested\dmg2014\v1\sections.json`
- `data\ingested\dmg2024\v1\sections.json`

## Key API endpoints (current)
### Character wizard and rules
1. `POST /wizard/characters/start`
2. `GET /wizard/characters/{characterId}`
3. `POST /wizard/characters/{characterId}/steps`
4. `POST /wizard/characters/{characterId}/finalize`
5. `POST /characters/{characterId}/copy-to-ruleset`
6. `POST /rules/resolve-mixed`
7. `GET /characters`
8. `GET /characters/{characterId}`
9. `PATCH /characters/{characterId}`
10. `POST /characters/{characterId}/archive`
11. `POST /characters/{characterId}/duplicate`

### Custom content
1. `POST /characters/{characterId}/custom/origin`
2. `POST /characters/{characterId}/custom/species`
3. `GET /custom/builders/origin/guidance`
4. `GET /custom/builders/species/guidance`
5. `POST /custom/builders/origin/preview`
6. `POST /custom/builders/species/preview`

### Items and calculations
1. `POST /characters/{characterId}/items/apply-effects`
2. `POST /characters/{characterId}/compute/check`
3. `POST /characters/{characterId}/compute/save`
4. `POST /characters/{characterId}/compute/attack`

## Branching convention used
A dedicated branch is created after each completed phase (phase-0, phase-1, etc.) and pushed to GitHub for review.
