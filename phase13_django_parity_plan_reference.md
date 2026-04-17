# Phase 13 Django Parity Plan Reference

## Objective
Maintain a comparable Django build as an aside while .NET 10 API + React remains primary.

## Parity scope (must-match v1 capabilities)
1. Character wizard start/steps/finalize/copy-to-ruleset
2. Mixed-rules resolution endpoint behavior
3. Custom origin/species guided vs fully-custom validation
4. Item effect and inventory/attunement state updates
5. Check/save/attack calculation endpoints
6. Character management and revision history
7. Local auth stub behavior

## Suggested Django structure
1. `apps/auth_stub/`
2. `apps/rules/`
3. `apps/characters/`
4. `apps/items/`
5. `apps/mechanics/`

## API compatibility strategy
1. Mirror route paths and request/response JSON contracts from the .NET API.
2. Keep shared fixture payloads for contract tests across both stacks.
3. Mark intentional contract differences explicitly in a parity exceptions list.

## Execution order
1. Scaffold Django project and app modules.
2. Implement read-only parity endpoints first (health, guidance, resolve).
3. Implement mutable flows (wizard, inventory state, history).
4. Run contract tests against both APIs with same fixtures.

## Parity acceptance checkpoint
- Django parity can be considered v1-complete when shared fixture-based contract tests pass for all listed parity scope endpoints.
