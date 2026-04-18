# DndAppName

DndAppName is a D&D character creation and management app focused on 2014/2024 rules support, mixed-rules compatibility, explainable calculations, custom lineage/origin creation, and item/attunement-aware character math.
Repository folder names may vary (for example `CopilotTesting`), while the solution/project naming remains `DndAppName`.

## Current capabilities
1. .NET 10 API + React frontend foundation.
2. Rulebook markdown ingestion to versioned JSON artifacts.
3. Character wizard flow with mixed-rules resolution and copy-to-ruleset support.
4. Persisted wizard drafts, character library summaries, and character history (survive API restarts).
5. Persistent character build model (class, build method, ability scores, skill proficiencies).
6. Build persistence now stores multiclass levels, selected lineage/background modules, and per-skill training tiers (`None`/`Proficient`/`Expertise`).
7. Persistent inventory model with equip/attune/unattune and attunement-cap enforcement.
8. Persisted calculations for checks/saves/attacks and derived stats.
9. Custom origin/species validation and preview endpoints.
10. Frontend character sheet sections for persisted vitals, spells, and resources.

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
   # Run from repository root:
   Set-Location src\DndApp.Web
   npm install
   npm run dev
   ```
   If you are already in `src\DndApp.Web`, run only:
   ```powershell
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
2. After login, the app routes to **Your characters** (`/characters`) and shows active characters only.
3. Use **Archived characters** (`/characters/archived`) to restore archived entries or permanently delete them (explicit confirmation required before delete).
4. Use **Create new character** (`/characters/new`) to start the creation flow.
5. In **Character build setup**, set the character name (optional), rules mode, primary class level, and ability-score method:
    - **Point buy** (27-point budget),
    - **Manual** score entry, or
    - **Roll** (4d6 drop lowest) with drag/drop plus touch-friendly tap assignment from roll pool into each ability slot. Optional **Reroll 1s once** is available.
6. **Total character level** is derived from class levels (primary + multiclass entries), and proficiency bonus is auto-calculated from total level using `(level - 1) // 4 + 2`.
7. Mixed mode now uses a **multi-select overlay source picker** instead of comma-separated text, and catalog module loading uses the currently selected overlay sources only.
8. New-character setup now includes primary class, optional multiclass entries (primary class required), race/species, and background/origin selections.
9. Race/species and background/origin ability bonuses (when available from catalog payload) are applied into total ability scores.
10. In **Wizard + persistent build**, click **Start Wizard Draft**, choose a main class (from the selected base ruleset), optionally choose a subclass (mixed mode shows cross-version subclasses), apply selections, then click **Save build to persistent model**.
11. Click **Finalize** to create the character record (wizard path) and use persisted character ID in the library.
12. In **Skills menu and persisted checks**, each skill has a `None/Proficient/Expertise` dropdown with live proficiency/expertise slot counters (negative values indicate over-allocation), plus persisted advantage/disadvantage check rolling; saved builds now round-trip these training tiers.
13. In **Inventory from database (persisted)**, add item definitions from catalog, set quantity, and manage equip/attune/unattune/remove states. Inventory rows show value, weight, attunement, equip status, and quantity.
14. **Attacks** auto-builds from equipped weapon-style inventory entries (deduped by weapon details) and shows to-hit bonus, damage expression, and advantage/disadvantage rolling.
15. In **Character sheet: vitals, spells, resources**, edit and save persisted HP/speed/AC values, spell entries, and resource pools.
16. In **Custom builder previews**, click preview buttons to test guided custom payload validation.

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

Validate normalized catalog coverage (classes, species/races, backgrounds/origins, spells, items) for both rulesets:
```powershell
dotnet run --project tools\DndApp.ContentIngestion\DndApp.ContentIngestion.csproj -- --validate-catalog
```

Normalization now classifies modules into expandable categories (`class`, `subclass`, `race`, `background`, `feat`, `spell`, `item`, `section`) and creates item definitions for detected item modules.
It also enriches class/background module payloads with proficiency metadata (`fixedSkillProficiencies`, `skillChoices`, `skillChoiceCount`, `expertiseChoiceCount`) used by the frontend skill planner.

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
- Provider-switch strategy: keep all schema changes via EF migrations in `src\DndApp.Api\Data\Migrations`, then generate/apply provider-specific migrations when enabling PostgreSQL in configuration.

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
13. `POST /characters/{characterId}/restore`
14. `DELETE /characters/{characterId}` (permanent delete)
15. `POST /characters/{characterId}/duplicate`
16. `GET /characters/{characterId}/history`
17. `POST /admin/characters/reconcile` (repairs missing persisted wizard records/drafts from existing character sheets)

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
5. Build payload now supports persisted multiclass levels, selected modules (race/background/origin/species), and skill training tiers.
6. Build endpoints now require `X-Session-Token` and enforce owner access.

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
5. Derived stats now include persisted vitals (HP values) and class-derived save proficiency context.
6. Persisted compute endpoints now require `X-Session-Token` and enforce owner access.

### Persisted spells, resources, and vitals
1. `GET /characters/{characterId}/spells`
2. `PUT /characters/{characterId}/spells`
3. `GET /characters/{characterId}/resources`
4. `PUT /characters/{characterId}/resources`
5. `GET /characters/{characterId}/vitals`
6. `PUT /characters/{characterId}/vitals`

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
