# Phase 7 Mixed-Mode Resolution Reference

## Implemented endpoint
- `POST /rules/resolve-mixed`

## Resolution policy implemented
1. Base-rules precedence when no explicit override is supplied.
2. Explicit slot override support (`ExplicitOverridesBySlot`).
3. Conflict warnings emitted for auto-resolved or override-resolved slots.
4. Mixed-mode overlay source validation.
5. Compatibility validation for base ruleset.

## Output
1. Resolved winner per slot
2. Full candidates per slot
3. Resolution reason
4. Warnings and errors

## Example request payload
```json
{
  "baseRuleSystem": "Rules2014",
  "mixedModeEnabled": true,
  "overlaySources": ["PHB2024"],
  "selections": [],
  "explicitOverridesBySlot": {}
}
```
