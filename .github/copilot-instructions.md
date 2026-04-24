# Copilot instructions for DndAppName

## Build, test, and lint commands

Run from repository root unless noted otherwise.

| Task | Command |
| --- | --- |
| Build all .NET projects | `dotnet build DndAppName.slnx -v minimal` |
| Run API tests | `dotnet test tests\DndApp.Api.Tests\DndApp.Api.Tests.csproj` |
| Run a single API test | `dotnet test tests\DndApp.Api.Tests\DndApp.Api.Tests.csproj --filter "FullyQualifiedName~CharacterWizardServiceTests.StartDraft_AllowsEmptyName_UsesDefault"` |
| Start API | `dotnet run --project src\DndApp.Api\DndApp.Api.csproj` |
| Frontend install (from `src\DndApp.Web`) | `npm install` |
| Frontend dev server (from `src\DndApp.Web`) | `npm run dev` |
| Frontend lint (from `src\DndApp.Web`) | `npm run lint` |
| Frontend production build (from `src\DndApp.Web`) | `npm run build` |
| Ingest markdown to JSON artifacts | `dotnet run --project tools\DndApp.ContentIngestion\DndApp.ContentIngestion.csproj` |
| Import ingestion artifacts to DB | `dotnet run --project tools\DndApp.ContentIngestion\DndApp.ContentIngestion.csproj -- --import-db` |
| Normalize imported content to catalog/domain tables | `dotnet run --project tools\DndApp.ContentIngestion\DndApp.ContentIngestion.csproj -- --normalize-db` |
| Validate normalized catalog coverage | `dotnet run --project tools\DndApp.ContentIngestion\DndApp.ContentIngestion.csproj -- --validate-catalog` |

## High-level architecture

- **Three-project solution:** `src\DndApp.Api` (.NET minimal API + EF Core), `tests\DndApp.Api.Tests` (xUnit), and `tools\DndApp.ContentIngestion` (markdown-to-catalog ingestion pipeline).
- **API composition is centralized in `Program.cs`:** DI registration, CORS, startup migration, and all HTTP endpoints live in one file. Domain behavior is delegated to services in feature folders (`Characters`, `Auth`, `Items`, `Mechanics`, `MixedRules`, `CustomContent`).
- **Persistence is SQLite-first with EF migrations:** `AppDbContext` maps both legacy normalized content tables (`rule_module`, `rule_variant`, etc.) and split-edition catalog tables (`catalog_*_2014`, `catalog_*_2024`, plus unified `catalog_spell`), plus character/auth/ops tables.
- **Content pipeline is staged:** markdown books (`DnDPHB*.md`, `DnDDMG*.md`) -> JSON under `data\ingested\...\v1\sections.json` -> optional DB import/normalization/validation via ingestion tool flags.
- **Frontend is a single React app with typed API boundary:** `src\DndApp.Web\src\App.tsx` holds most app state/workflow; `src\DndApp.Web\src\api.ts` centralizes fetch and auth header handling; `src\DndApp.Web\src\types.ts` mirrors backend payload contracts.

## Key repository conventions

- **Enums are serialized as strings end-to-end.** The API config uses `JsonStringEnumConverter`, and frontend types use string unions (`'Rules2014' | 'Rules2024'`, etc.). Keep enum payloads string-valued in JSON.
- **Character-scoped persisted endpoints require session ownership checks.** Use `X-Session-Token` (or `sessionToken` query fallback where supported) and follow the `EndpointAuth.AuthorizeCharacterOwnerAsync(...)` pattern before accessing persisted character data.
- **Catalog reads follow typed-table-first, legacy-fallback behavior.** For split-edition entities (classes/subclasses/items/races/species/backgrounds/feats), query edition-specific catalog tables first, then fall back to legacy `rule_module` sources when needed.
- **Backend tests use real EF Core + SQLite in-memory**, typically opening a `SqliteConnection("Data Source=:memory:")`, `UseSqlite(connection)`, then `EnsureCreatedAsync()` in test fixtures.
- **README is part of done criteria for behavior/setup/workflow changes.** Keep `README.md` in sync with implementation changes.
- **After each completed phase, commit and push.** Once a phase is implemented and validated, save, commit, and update GitHub before moving to the next phase.

## MCP server configuration (repository-local)

- **Playwright MCP is configured in `.vscode\mcp.json`** for agentic browser workflows in this project.
- Server config uses:
  - command: `npx`
  - args: `-y @playwright/mcp@latest`
