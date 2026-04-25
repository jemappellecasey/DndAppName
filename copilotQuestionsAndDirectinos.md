# Phase 1 Fixes - COMPLETED ✅
- ✅ Removed proficiency bonus display from character build setup
- ✅ Added stat entry limit validation: -10,000 to 10,000 with "Ambitions... but no." message
- ✅ Verified currency consolidation endpoint and handler (working)
- ✅ Verified skill proficiency selection UI (working)

# Phase 2 Fixes - COMPLETED ✅
- ✅ Fixed primary class selection display: now shows just "Artificer" not "Artificer Alchemist"
- ✅ Race/Species bonus calculation: verified backend returns abilityBonuses, frontend applies them correctly
- ✅ Background/Origin bonus calculation: verified backend returns abilityBonuses, frontend applies them correctly

# Phase 3 Fixes - COMPLETED ✅
- ✅ Removed "view activity" button from active characters list
- ✅ Fixed HP rolling: Level 1 always uses max hit die value; now displays per-level roll breakdown when rolled

# Phase 4 - IN PROGRESS

## COMPLETED ✅
- ✅ **#8 Starting gear/gold calculation**: 
  - Implemented CLASS_STARTING_GOLD lookup (Monk: 25gp, Fighter/Rogue/Bard: 75gp, Cleric/Druid/Wizard: 50gp)
  - Auto-populates when selecting "gold-only" mode
  - Updates when primary class changes
  - Shows display text: "Starting gold for [Class]: X gp"

## IN PROGRESS 🔄
- ⏳ **#12 Spell Selection** (NEXT PRIORITY):
  - See `NEXT_SESSION_PLAN.md` for detailed implementation guide
  - Requires:
    1. Backend: Implement `GetRecommendedSpellsAsync()` with spell prep calculations by class + ability modifiers
    2. Frontend: Refactor spell UI to show sections by class/feature with prep counts and auto-granted spells
    3. Supports 2014 & 2024 rules with different preparation formulas per class
  - Key files: `CharacterProgressionService.cs`, `src/DndApp.Web/src/App.tsx` (lines 3286+)

# Remaining items for Phase 4-5:

7. After selecting a race/species/background/origin/class/subclass from a drop down a "show info" button should pop up that displays the details for that specific thing.

9. some of the entries for backgrounds should not be there. Please fix the imports and database so they are not included. Do you need help identifying which ones do not belong?

13. proficiencies/expertise available for skill setup is not being calculated correctly, this needs to be fully implemented, make any changes that need to be made.

14. Selecting items should display their value in gp if applicable. These also need a "details" button when selected so the user can see the details of the item.

15. Adding items to persisted inventory isn't working, this needs to be fixed.

16. A user should be able to click and drag to rearrange their active characters. Newly created characters should go on the top of the list of active characters. Archived characters should be ordered by when they were archived.
