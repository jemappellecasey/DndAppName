# Phase 10 Auth and History Reference

## Implemented endpoints
1. `POST /auth/local/login`
2. `GET /auth/local/me`
3. `GET /characters/{characterId}/history`

## Behavior implemented
1. Local auth stub issues in-memory session tokens.
2. Wizard creation requires a session token.
3. Character revision history tracks draft start, finalize, update, archive, duplicate, and ruleset-copy events.
