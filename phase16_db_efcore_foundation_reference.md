# Phase 16 EF Core Foundation Reference

## Implemented
1. Added EF Core packages for core, design-time tooling, and SQLite provider.
2. Added `AppDbContext` and design-time factory.
3. Added database options/config model with provider and connection strings.
4. Wired `AppDbContext` into API startup for SQLite provider.
5. Added DB connectivity endpoint: `GET /db/health`.

## Notes
1. This phase supports SQLite provider execution.
2. PostgreSQL connection placeholders are configured and provider switch work is planned in follow-up phases.
