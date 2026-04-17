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
   - The default dev launch profile listens on `http://localhost:5080`
   - Test:
     ```powershell
     Invoke-WebRequest http://localhost:5080/health
     ```
   - Root endpoint is also available at:
     - `GET http://localhost:5080/`

## Frontend launch (React)
1. Start API in one terminal:
   ```powershell
   dotnet run --project src\DndApp.Api\DndApp.Api.csproj
   ```
2. Start web app in another terminal:
   ```powershell
   Set-Location src\DndApp.Web
   npm install
   npm run dev
   ```
3. Open:
   - `http://localhost:5173`

The web app defaults to `http://localhost:5080` for API calls. Override with `VITE_API_BASE_URL` if needed.

### Troubleshooting (local run)
1. If `dotnet build` fails with `DndApp.Api.exe ... file is being used by another process`, stop the running API (`Ctrl+C` in the terminal where `dotnet run` is active), then build again.
2. `GET /` and `GET /health` should return `200`. If they do, the API is running correctly.

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
12. `GET /characters/{characterId}/history`

### Local auth stub
1. `POST /auth/local/login`
2. `GET /auth/local/me?sessionToken={token}`

### Custom content
1. `POST /characters/{characterId}/custom/origin`
2. `POST /characters/{characterId}/custom/species`
3. `GET /custom/builders/origin/guidance`
4. `GET /custom/builders/species/guidance`
5. `POST /custom/builders/origin/preview`
6. `POST /custom/builders/species/preview`

### Items and calculations
1. `POST /characters/{characterId}/items/apply-effects`
2. `GET /inventory/attunement-guidance`
3. `POST /characters/{characterId}/inventory/update-item-state`
4. `POST /characters/{characterId}/compute/check`
5. `POST /characters/{characterId}/compute/save`
6. `POST /characters/{characterId}/compute/attack`

## Branching convention used
A dedicated branch is created after each completed phase (phase-0, phase-1, etc.) and pushed to GitHub for review.

## Note on wizard start payload
`POST /wizard/characters/start` expects a `sessionToken` from local login in addition to character name and rules profile.

## Container launch (deployment baseline)
> Docker commands require Docker Desktop (or Docker Engine) to be running.
> If you see `open //./pipe/dockerDesktopLinuxEngine: The system cannot find the file specified`, start Docker Desktop first.

1. Build image:
   ```powershell
   docker build -t dndappname-api .
   ```
2. Run container:
   ```powershell
   docker run --rm -p 8080:8080 dndappname-api
   ```
3. Test container health:
   ```powershell
   Invoke-WebRequest http://localhost:8080/health
   ```
