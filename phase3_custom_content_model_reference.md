# Phase 3 Custom Content Model Reference

## Decision applied
- Guided custom mode: **hard-block invalid submissions**.
- Fully custom mode: **allow submission with warnings**.

## Implemented API endpoints
1. `POST /characters/{characterId}/custom/origin`
2. `POST /characters/{characterId}/custom/species`

## Implemented validation profile
1. Guided origin checks:
   - 2-3 ability bonus entries
   - total bonus sum 2-3
   - exactly 2 skill proficiencies
   - at least one feature note
2. Guided species checks:
   - walking speed 25-40
   - 1-6 traits
   - size required
   - at least one language
3. Fully custom:
   - warnings only (no hard block)

## Tags used
- `guided-custom`
- `fully-custom`
