# Phase 4 Item Effect Pipeline Reference

## Implemented endpoint
- `POST /characters/{characterId}/items/apply-effects`

## Activation rule applied
- Item effects apply only when:
  1. Item is equipped, and
  2. If item requires attunement, it is attuned

## Supported effect categories (v1)
1. AC bonus
2. Move speed bonus
3. Saving throw bonus (targeted)
4. Ability check bonus (targeted)
5. Granted spell

## Response includes
1. Derived stats after active item effects
2. Effect breakdown entries for transparency and debugging
