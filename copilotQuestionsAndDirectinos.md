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

# Remaining items for Phase 4-5:

7. After selecting a race/species/background/origin/class/subclass from a drop down a "show info" button should pop up that displays the details for that specific thing.

8. Starting gear/gold is not being calculated, it needs to be.

9. some of the entries for backgrounds should not be there. Please fix the imports and database so they are not included. Do you need help identifying which ones do not belong?

12. the add spells part of character creation isn't working, please fully implement that part of the project. Do you need more direction on this?

13. proficiencies/expertise available for skill setup is not being calculated correctly, this needs to be fully implemented, make any changes that need to be made.

14. Selecting items should display their value in gp if applicable. These also need a "details" button when selected so the user can see the details of the item.

15. Adding items to persisted inventory isn't working, this needs to be fixed.

16. A user should be able to click and drag to rearrange their active characters. Newly created characters should go on the top of the list of active characters. Archived characters should be ordered by when they were archived.
