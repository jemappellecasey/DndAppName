# Phase 1 Fixes - COMPLETED ✅
- ✅ Removed proficiency bonus display from character build setup
- ✅ Added stat entry limit validation: -10,000 to 10,000 with "Ambitions... but no." message
- ✅ Verified currency consolidation endpoint and handler (working)
- ✅ Verified skill proficiency selection UI (working)

# Remaining items for Phase 2-5:

4. Primary class selection needs to be just Artificer, Barbarian, Bard, etc. Not "artificer alchemist" that "alchemist" should be on the subclass dropdown if the person has enough levels entered in a class for a subclass.

5. Race/Species bonuses are not being calculated, they need to be.

6. Background/Origin bonuses are not being calculated, they need to be.

7. After selecting a race/species/background/origin/class/subclass from a drop down a "show info" button should pop up that displays the details for that specific thing.

8. Starting gear/gold is not being calculated, it needs to be.

9. some of the entries for backgrounds should not be there. Please fix the imports and database so they are not included. Do you need help identifying which ones do not belong?

10. when rolling for hp, remember from the rulebook that level 1 is always the max value of their hit die.

11. when rolling for hp, display the roll for each level on character creation.

12. the add spells part of character creation isn't working, please fully implement that part of the project. Do you need more direction on this?

13. proficiencies/expertise available for skill setup is not being calculated correctly, this needs to be fully implemented, make any changes that need to be made.

14. Selecting items should display their value in gp if applicable. These also need a "details" button when selected so the user can see the details of the item.

15. Adding items to persisted inventory isn't working, this needs to be fixed.

16. A user should be able to click and drag to rearrange their active characters. Newly created characters should go on the top of the list of active characters. Archived characters should be ordered by when they were archived.

17. the "view activity" button isn't needed for active characters. Please remove that button.
