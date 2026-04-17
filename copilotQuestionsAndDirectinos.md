# Copilot Questions and Directions (Completed)

## 1. Direction: Rolling stats should be drag-and-drop
**Status:** Implemented.

**Current behavior:** Roll mode generates a roll pool (4d6 drop lowest), each rolled value is draggable, and each ability slot is a drop target. Assigned rolls can be cleared/reassigned, and saving is blocked until all six values are assigned.

---

## 2. Direction: Build out login for multiple users
**Status:** Implemented for local multi-user sessions.

**Current behavior:**
- `POST /auth/local/register` and `POST /auth/local/login` use persistent user accounts in SQLite.
- Passwords are hashed (PBKDF2) before storage.
- Sessions are persisted and validated from `X-Session-Token`.
- Persisted build/inventory/persisted-computation routes enforce character ownership.

---

## 3. Explain each input in "2. Character build setup"
1. **Character name**: Display name saved to character records/build.
2. **Rules mode** (`Rules2024` / `Rules2014`): Selects ruleset context and class compatibility validation.
3. **Build method** (`PointBuy` / `Manual` / `Roll`): Determines how ability scores are entered/generated.
4. **Mixed mode**: Enables cross-source overlays for wizard flow.
5. **Overlay sources**: Comma-separated source codes used when mixed mode is enabled.
6. **Proficiency bonus**: Base proficiency used in persisted check/save/attack computations.
7. **Ability score controls**:
   - **Point buy**: +/- with 27-point budget checks.
   - **Manual**: Direct numeric entry.
   - **Roll**: 4d6 drop-lowest generation with drag/drop assignment to abilities.

---

## 4. Question: Where are SQLite DB files located?
Configured in API settings:
- `src\DndApp.Api\appsettings.Development.json` → `Data Source=dndapp-dev.sqlite`
- `src\DndApp.Api\appsettings.json` → `Data Source=dndapp.sqlite`

In local development, the active file is typically:
- `src\DndApp.Api\dndapp-dev.sqlite`

---

## 5. Question: Is Docker still necessary / what is it doing?
Docker is **optional** for local development, but useful for:
1. Running API in a consistent containerized environment.
2. Testing deployment-like behavior (port binding, runtime image, publish output).
3. Sharing/reproducing runtime setup without local SDK parity issues.

What the Dockerfile does:
1. Uses `dotnet/sdk:10.0` to restore + publish.
2. Copies published API output into `dotnet/aspnet:10.0`.
3. Exposes port `8080` and runs `dotnet DndApp.Api.dll`.

---

## 6. Backend error resolution: `SQLite Error 1: no such table: character_sheet`
### Root cause
The DB file existed but schema migrations for newer tables (including `character_sheet`) had not been applied.

### Resolution applied
Startup now automatically applies EF Core migrations:
- `src\DndApp.Api\Program.cs` now runs `db.Database.Migrate()` during app startup.

### Manual fallback command
If needed, run:
```powershell
dotnet ef database update --project src\DndApp.Api\DndApp.Api.csproj --startup-project src\DndApp.Api\DndApp.Api.csproj
```

---

## 7. Docker build error explanation
Error seen:
`failed to prepare extraction snapshot ... parent snapshot ... does not exist`

### What it means
This is a Docker Desktop/buildkit layer-cache corruption/state issue, not an application code compile/publish error.

### Fix steps
1. Restart Docker Desktop.
2. Rebuild without cache:
   ```powershell
   docker build --no-cache -t dndappname-api .
   ```
3. If still failing, clean builder cache:
   ```powershell
   docker builder prune -af
   ```
4. Retry build.

---

## Error context cleanup
Raw repeated terminal stack traces and build logs were removed from this file as requested.
