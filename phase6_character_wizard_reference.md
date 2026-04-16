# Phase 6 Character Wizard Reference

## Implemented endpoints
1. `POST /wizard/characters/start`
2. `GET /wizard/characters/{characterId}`
3. `POST /wizard/characters/{characterId}/steps`
4. `POST /wizard/characters/{characterId}/finalize`
5. `POST /characters/{characterId}/copy-to-ruleset`

## Behavior implemented
1. In-memory draft storage for v1.
2. Wizard step submissions are validated against selected rules mode.
3. Incompatible source selections are blocked at step submission.
4. Base ruleset is treated as locked after creation.
5. Ruleset migration is implemented as copy-to-ruleset with warnings.
