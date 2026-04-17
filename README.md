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

### Frontend quality commands
Run these from `src\DndApp.Web`:
```powershell
npm run lint
npm run build
```

## Frontend quick walkthrough (first-time user)
1. In the **Local session** card, enter a username and click **Start Session**.
2. In **Character build setup**, set the character name, rules mode, and ability-score method:
   - **Point buy** (27-point budget),
   - **Manual** score entry, or
   - **Roll** (4d6 drop lowest).
3. In **Character wizard and class module**, click **Start Wizard Draft**, pick a class from DB-backed class catalog, then click **Apply selected class to wizard**.
4. Click **Finalize** to create the character record.
5. In **Skills menu and check rolling**, choose any skill, set advantage/expertise, and roll checks from the full skill list.
6. In **Inventory from database**, add item definitions from the DB catalog, then equip/attune/unattune/remove items. The UI sends current inventory state into `update-item-state` to compute derived effects.
7. In **Character library**, click **Refresh** and select your character. Use **History**, **Duplicate**, or **Archive** as needed.
8. In **Custom builder previews**, click preview buttons to test guided custom payload validation.

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
- `Database:Provider` currently supports `sqlite` for this phase.
- PostgreSQL connection string placeholders are present for planned provider switch work.

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

### Persistent character build (phase 2)
1. `GET /characters/{characterId}/build`
2. `PUT /characters/{characterId}/build`
3. `PATCH /characters/{characterId}/build`
4. `DELETE /characters/{characterId}/build`

### Content catalogs (database-backed)
1. `GET /catalog/classes?ruleSystem={Rules2014|Rules2024}`
2. `GET /catalog/items`

## Documentation update policy
For this repository, **README.md must be updated whenever behavior, setup steps, or user workflows change**. Treat README updates as part of done criteria for every future feature phase.

## Branching convention used
A dedicated branch is created after each completed phase (phase-0, phase-1, etc.) and pushed to GitHub for review.

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
