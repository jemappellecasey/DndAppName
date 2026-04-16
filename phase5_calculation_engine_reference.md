# Phase 5 Calculation Engine Reference

## Decision applied
- Endpoints return **modifier math** and can also return **optional random rolls**.

## Implemented endpoints
1. `POST /characters/{characterId}/compute/check`
2. `POST /characters/{characterId}/compute/save`
3. `POST /characters/{characterId}/compute/attack`

## Supported mechanics (v1)
1. Proficiency and expertise handling for checks
2. Proficient/non-proficient saving throws
3. Weapon attack modifier calculation
4. Advantage/disadvantage roll selection
5. Damage roll parsing for `NdM` formats (example: `1d8`)

## Response behavior
- If `RollDice=false`, endpoints return computed modifiers and no random roll totals.
- If `RollDice=true`, endpoints return roll details and final totals.
