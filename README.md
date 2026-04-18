# DndAppName

DndAppName is a D&D character creation and management app focused on 2014/2024 rules support, mixed-rules compatibility, explainable calculations, custom lineage/origin creation, and item/attunement-aware character math.

## Current capabilities
1. .NET 10 API + React frontend foundation.
2. Rulebook markdown ingestion to versioned JSON artifacts.
3. Character wizard flow with mixed-rules resolution and copy-to-ruleset support.
4. Persistent character build model (class, build method, ability scores, skill proficiencies).
5. Persistent inventory model with equip/attune/unattune and attunement-cap enforcement.
6. Persisted calculations for checks/saves/attacks and derived stats.
7. Custom origin/species validation and preview endpoints.

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

### Frontend quality commands
Run these from `src\DndApp.Web`:
```powershell
npm run lint
npm run build
```

## Frontend quick walkthrough (first-time user)
1. The landing page shows **Login** and **About DndAppName**. Sign in with username + password (or register a new local account).
2. After login, **Your characters** displays only characters owned by the signed-in user, with ruleset badges.
3. In **Character build setup**, set the character name, rules mode, character level, and ability-score method:
    - **Point buy** (27-point budget),
    - **Manual** score entry, or
    - **Roll** (4d6 drop lowest) with drag/drop assignment from roll pool into each ability slot.
4. **Proficiency bonus** is auto-calculated from level using `(level - 1) // 4 + 2`.
5. Mixed mode now uses a **multi-select overlay source picker** instead of comma-separated text.
6. New-character setup now includes primary class, optional multiclass entries (primary class required), race/species, and background/origin selections.
7. Race/species and background/origin ability bonuses (when available from catalog payload) are applied into total ability scores.
8. In **Wizard + persistent build**, click **Start Wizard Draft**, apply class/race/background selections, then click **Save build to persistent model**.
9. Click **Finalize** to create the character record (wizard path) and use persisted character ID in the library.
10. In **Skills menu and persisted checks**, choose any skill, set advantage/expertise, and roll checks via persisted-computation endpoint(s).
11. In **Inventory from database (persisted)**, add item definitions from catalog, then equip/attune/unattune/remove through persisted inventory endpoints.
12. In **Custom builder previews**, click preview buttons to test guided custom payload validation.

### Troubleshooting (local run)
1. If `dotnet build` fails with `DndApp.Api.exe ... file is being used by another process`, stop the running API (`Ctrl+C` in the terminal where `dotnet run` is active), then build again.
2. `GET /` and `GET /health` should return `200`. If they do, the API is running correctly.

## Run ingestion pipeline
Generate versioned JSON artifacts from PHB/DMG markdown sources:

```powershell
dotnet run --project tools\DndApp.ContentIngestion\DndApp.ContentIngestion.csproj
```

Import generated section artifacts into SQLite raw tables:
```powershell
dotnet run --project tools\DndApp.ContentIngestion\DndApp.ContentIngestion.csproj -- --import-db
```

Normalize imported raw sections into core domain entities (`rule_system`, `content_source`, `rule_module`, `rule_variant`):
```powershell
dotnet run --project tools\DndApp.ContentIngestion\DndApp.ContentIngestion.csproj -- --normalize-db
```

Normalization now classifies modules into expandable categories (`class`, `subclass`, `race`, `background`, `feat`, `spell`, `item`, `section`) and creates item definitions for detected item modules.

Outputs are written under:
- `data\ingested\phb2014\v1\sections.json`
- `data\ingested\phb2024\v1\sections.json`
- `data\ingested\dmg2014\v1\sections.json`
- `data\ingested\dmg2024\v1\sections.json`

## Database foundation (SQLite now)
1. The API is configured for SQLite by default in `appsettings*.json`.
2. Database health endpoint:
   - `GET http://localhost:5080/db/health`
3. EF Core migrations are enabled through `AppDbContext` + design-time factory.
4. Create/apply migrations:
   ```powershell
   dotnet ef migrations add <MigrationName> --project src\DndApp.Api\DndApp.Api.csproj --startup-project src\DndApp.Api\DndApp.Api.csproj --output-dir Data\Migrations
   dotnet ef database update --project src\DndApp.Api\DndApp.Api.csproj --startup-project src\DndApp.Api\DndApp.Api.csproj
   ```

### Database provider config
- `Database:Provider` currently supports `sqlite`.
- PostgreSQL connection string placeholders are present for future provider-switch work.

## Key API endpoints (current)
### Character wizard and rules
1. `POST /wizard/characters/start`
2. `GET /wizard/characters/{characterId}`
3. `POST /wizard/characters/{characterId}/steps`
4. `POST /wizard/characters/{characterId}/finalize`
5. `POST /characters/{characterId}/copy-to-ruleset`
6. `POST /rules/resolve-mixed`
7. `GET /characters`
8. `GET /characters?mine=true` (requires `X-Session-Token`; returns only current user's characters)
9. `GET /catalog/content-sources?ruleSystem={Rules2014|Rules2024}` (overlay-source options from opposite ruleset)
10. `GET /characters/{characterId}`
11. `PATCH /characters/{characterId}`
12. `POST /characters/{characterId}/archive`
13. `POST /characters/{characterId}/duplicate`
14. `GET /characters/{characterId}/history`

### Local auth
1. `POST /auth/local/register`
2. `POST /auth/local/login`
3. `GET /auth/local/me` (reads `X-Session-Token` header)

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

### Persistent character build
1. `GET /characters/{characterId}/build`
2. `PUT /characters/{characterId}/build`
3. `PATCH /characters/{characterId}/build`
4. `DELETE /characters/{characterId}/build`
5. Build endpoints now require `X-Session-Token` and enforce owner access.

### Persistent inventory
1. `GET /characters/{characterId}/inventory`
2. `POST /characters/{characterId}/inventory/items`
3. `PATCH /characters/{characterId}/inventory/items/{inventoryItemId}`
4. `DELETE /characters/{characterId}/inventory/items/{inventoryItemId}`
5. Inventory endpoints now require `X-Session-Token` and enforce owner access.

### Persisted calculations
1. `GET /characters/{characterId}/compute/derived-stats`
2. `POST /characters/{characterId}/compute/check/persisted`
3. `POST /characters/{characterId}/compute/save/persisted`
4. `POST /characters/{characterId}/compute/attack/persisted`
5. Persisted compute endpoints now require `X-Session-Token` and enforce owner access.

### Rule validation
1. `PUT /characters/{characterId}/build` and `PATCH /characters/{characterId}/build` now validate class-module compatibility and prerequisite predicates from `prerequisite`.
2. `POST /characters/{characterId}/inventory/items` now validates item source compatibility and prerequisite predicates before persisting.

### Frontend integration
The React app now uses persisted build/inventory/computation endpoints for core workflows instead of local simulation payloads.

### Content catalogs (database-backed)
1. `GET /catalog/classes?ruleSystem={Rules2014|Rules2024}`
2. `GET /catalog/items`
3. `GET /catalog/content-sources?ruleSystem={Rules2014|Rules2024}`
4. `GET /catalog/modules?baseRuleSystem={Rules2014|Rules2024}&mixedMode={true|false}&overlaySources=...&moduleTypes=...`

## Documentation update policy
For this repository, **README.md must be updated whenever behavior, setup steps, or user workflows change**. Treat README updates as part of done criteria for every future feature phase.

## Note on wizard start payload
`POST /wizard/characters/start` expects a `sessionToken` from local login in addition to character name and rules profile.

### Enum payload note
Enum fields are expected as strings in JSON (for example `Rules2024`, `Rules2014`, `GuidedCustom`, `FullyCustom`, `None`, `Advantage`, `Disadvantage`).

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
