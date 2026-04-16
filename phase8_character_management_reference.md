# Phase 8 Character Management Reference

## Implemented endpoints
1. `GET /characters?includeArchived={bool}`
2. `GET /characters/{characterId}`
3. `PATCH /characters/{characterId}`
4. `POST /characters/{characterId}/archive`
5. `POST /characters/{characterId}/duplicate`

## Behavior implemented
1. Character records are created when wizard drafts are finalized.
2. Archive is soft-delete only.
3. Duplicate creates a new active character summary record.
4. List endpoint can include or exclude archived records.
