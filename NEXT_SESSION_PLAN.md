# Next Session Plan - Spell Selection & Remaining Features

## Session Status
- **Completed**: Phase 1-3 + Phase 4 (starting gear/gold) = **8 of 17 tasks done**
- **Tests**: All 24 passing ✅
- **Builds**: Frontend & API successful ✅
- **Branch**: `phase-21-db-core-normalization`
- **Last Commit**: "Phase 4: Implement starting gear/gold calculation"

---

## NEXT: Phase 4 - Spell Selection (Item #12)

### Why This is Important
Current spell UI is extremely basic - just adds first available spell. User requirements:
1. Show spells organized by **class/feature that grants them**
2. Calculate **spell preparation count** based on class + ability mods (different for 2014/2024)
3. List **automatically-granted spells** at top (cantrips, racial spells, etc.)
4. Allow selection from **all available spells** for that source

### Implementation Approach

#### PART 1: Backend (CharacterProgressionService)
**File**: `src\DndApp.Api\Characters\CharacterProgressionService.cs`
**Method to Update**: `GetRecommendedSpellsAsync()` (currently returns empty)

Steps:
1. **Fetch character build** - Get class, level, ability scores from CharacterSheets
2. **Calculate spell slots** - Use class rules to determine how many spells can be prepared:
   - **Wizard** (2014 & 2024): `level + INT modifier` (min 1)
   - **Cleric** (2014 & 2024): `level + WIS modifier` (min 1)
   - **Bard** (2014 & 2024): `(level/2 rounded up) + CHA modifier` (min 1)
   - **Druid** (2014 & 2024): `level + WIS modifier` (min 1)
   - **Paladin** (2014 & 2024): `(level - 2) / 2 rounded up, but min 1 if level >= 5`
   - **Ranger** (2014 & 2024): `(level - 1) / 2 rounded up, but min 1 if level >= 5`
   - **Sorcerer** (2014): `(level + 1) / 2 rounded up` known, but all can be cast
   - **Sorcerer** (2024): Similar but verify exact rules
   - **Warlock** (2014 & 2024): Fixed slots, different calculation - **check if even uses preparation**
   - **Artificer** (2024 only): `level + INT modifier` (min 1)

3. **Get automatically-granted spells** - Query database for:
   - Cantrips from class/race (look for spells marked "level 0" in spell classes)
   - Racial spells (e.g., Tiefling = Hellish Rebuke at 3rd level)
   - Class-granted spells (e.g., some Warlock invocations grant spells)
   
   **Note**: May need to add database fields to mark spells as "automatic" or parse from descriptions

4. **Get available spells** - Filter from `ModuleCatalog` where:
   - `moduleType == "spell"`
   - `spellClasses` contains the character's class
   - Order by spell level/name

5. **Return structured data** - Modify `RecommendedSpellsResult` to include:
   - Prep count per class
   - Automatic spells grouped by source
   - Selectable spells grouped by source
   - Advisory message with rules reference

#### PART 2: Frontend (App.tsx)
**File**: `src\DndApp.Web\src\App.tsx`
**Current Spell UI**: Lines ~3286-3380

**Changes Needed**:
1. **Add spell source tracking** - New state:
   ```typescript
   const [spellSources, setSpellSources] = useState<SpellSourceGroup[]>([])
   ```

2. **Load prep info** - Add useEffect to call `/characters/{id}/spells/recommended`:
   ```typescript
   const loadSpellPrepInfo = async () => {
     const result = await fetch(`/characters/${characterId}/spells/recommended?classModuleId=${classId}&classLevel=${level}`)
     setSpellSources(result.data)
   }
   ```

3. **Refactor UI** - Replace simple "Add spell" button with:
   - For each spell source (e.g., "Wizard Spells", "Artificer Spells"):
     - Show prep count: "You can prepare 5 spells"
     - List automatic spells in a read-only section
     - Checkboxes for selectable spells (with count feedback)
     - "Selected: 3 / 5" indicator
   - Save button saves all selected spells as `spellEntries`

**UI Structure**:
```
Spells
├─ Wizard Spells (Prepare 5 / 5 max)
│  ├─ Automatic:
│  │  ☑ Mage Hand Legerdemain
│  │  ☑ Minor Illusion
│  └─ Selectable (3 chosen):
│     ☐ Magic Missile
│     ☐ Sleep
│     ☐ Identify
│     ☐ Mage Armor
│     [+5 more...]
├─ Artificer Spells (Prepare 3 / 3 max)
│  ├─ Automatic:
│  │  ☑ Mending
│  └─ Selectable (2 chosen):
│     ☐ Identify
│     ☐ Thunderwave
```

### Testing Checklist
- [ ] Build passes (npm run build + dotnet build)
- [ ] All 24 tests still pass
- [ ] Spell prep counts correct for each class
- [ ] Can select/deselect spells
- [ ] Spells persist when saved
- [ ] Automatic spells shown at top
- [ ] Respects 2014 vs 2024 rules

### Database Check
- [ ] Verify spell modules have correct `spellClasses` field
- [ ] Check if need to add "isAutomatic" or "spellLevel" fields for cantrips
- [ ] Confirm racial spell grants exist in database

---

## REMAINING TASKS (Priority Order)

### Phase 4 Continued:
- [ ] **#12 Spell Selection** ← NEXT (this document)
- [ ] **#14 Item Details & Value** - Show GP value, add info modal
- [ ] **#7 Show Info Button** - Design reusable modal for catalog items

### Phase 5:
- [ ] **#13 Skill Proficiencies** - Calculate available proficiencies by class/background
- [ ] **#15 Persisted Inventory** - Fix item addition to inventory
- [ ] **#6 Spell Prep Count Display** - (Related to #12, may be combined)
- [ ] **#9 Background Data Quality** - Identify and exclude invalid backgrounds
- [ ] **#16 Drag-Reorder Characters** - Implement character reordering, date sorting

---

## Key Files Reference

### Backend
- `src/DndApp.Api/Program.cs` - Spell endpoints (around line 1035-1045)
- `src/DndApp.Api/Characters/CharacterProgressionService.cs` - Spell logic
- `src/DndApp.Api/Characters/CharacterProgressionModels.cs` - Data models
- `src/DndApp.Api/Mechanics/CatalogCacheService.cs` - Cached spell data

### Frontend
- `src/DndApp.Web/src/App.tsx` - Spell UI (lines 3286+)
- `src/DndApp.Web/src/types.ts` - TypeScript models
- `src/DndApp.Web/src/api.ts` - Fetch functions

### Data
- D&D 5e spell prep rules: `DnDPHB2014.md`, `DnDPHB2024.md`
- Spell data: `/data/ingested/phb2014/v1/sections.json`, `/data/ingested/phb2024/v1/sections.json`

---

## Commands to Know

```bash
# Build & test
npm run build --prefix src\DndApp.Web
dotnet build DndAppName.slnx -v minimal
dotnet test tests\DndApp.Api.Tests\DndApp.Api.Tests.csproj

# Run API
dotnet run --project src\DndApp.Api\DndApp.Api.csproj

# Commit when done
git add -A
git commit -m "Phase 4 Part 2: Implement spell selection with prep counts"
git push

# Check status
git status
git log --oneline -5
```

---

## Architecture Notes

**Caching Strategy**: Spell data is cached in `CatalogCacheService` on startup. Use this for read-heavy operations.

**DI Pattern**: Scoped services (like `CharacterProgressionService`) can access `AppDbContext`. Singletons must use `IServiceProvider.CreateScope()`.

**Enum Handling**: All enums serialized as strings (JSON `JsonStringEnumConverter`). Frontend uses string unions.

**Character Build Flow**: 
1. User fills out wizard → creates draft
2. Draft finalized → character sheet created
3. Character sheet → fetch related data (spells, inventory, currency, etc.)

---

## Notes for Session Pickup

1. **Spell preparation is class-specific** - Wizard/Cleric/etc all have different formulas
2. **2014 vs 2024 rules differ** - Need to branch logic by `baseRuleSystem`
3. **Cantrips vs Prepared Spells** - Some classes know all cantrips but prepare known spells
4. **Multiclassing complexity** - Current design assumes single class; full multiclass support would be future work
5. **Database may not have all spell metadata** - May need to hardcode some automatic spell grants
6. **Test coverage** - Add test for spell prep calculation to `CharacterProgressionService` tests

---

## Success Criteria

When complete:
- ✅ Spell prep counts display correctly based on class + modifiers
- ✅ Automatic spells shown at top of each class section
- ✅ User can select/deselect from available spells
- ✅ Selection respects prep count limit with visual feedback
- ✅ Spells persist when character saved
- ✅ All tests passing
- ✅ Rules-compliant for both 2014 and 2024 editions
