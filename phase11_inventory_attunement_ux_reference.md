# Phase 11 Inventory Attunement UX Reference

## Implemented endpoints
1. `GET /inventory/attunement-guidance`
2. `POST /characters/{characterId}/inventory/update-item-state`

## Behavior implemented
1. Attunement cap enforced at 3 active attuned items.
2. Attunement requires the item to be equipped.
3. Non-attunement items cannot be marked attuned.
4. State update response returns recalculated derived stats and effect breakdown.

## Example state update payload
```json
{
  "baseStats": { "armorClass": 15, "moveSpeed": 30, "savingThrows": {}, "abilityChecks": {}, "availableSpells": [] },
  "items": [],
  "itemId": "cloak-1",
  "isEquipped": true,
  "isAttuned": true
}
```
