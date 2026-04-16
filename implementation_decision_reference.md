# Implementation Decision Reference

## Confirmed decisions
1. **Primary stack:** .NET 10 API + React.
2. **Secondary track:** Keep a comparable Django duplicate build as an aside.
3. **Core requirement:** Mixed-rules support across 2014/2024 with explicit handling for cross-edition content.
4. **Roll mechanics:** Must support advantage/disadvantage, including when granted automatically by item abilities.

## Open decision gates (mixed-rules strategy)
1. **Precedence policy:** In mixed mode, should base-rules content win by default unless the user explicitly overrides?
2. **Conflict behavior:** When two sources define conflicting mechanics, should the app block, warn, or auto-resolve?
3. **Character lock policy:** After a character is created in a mixed mode, can the base mode be changed later?
4. **Audit transparency:** Should every computed stat/roll show source provenance by default, or only on demand?
5. **Custom content compatibility:** Can fully custom origin/species content be used in all rule modes without restriction?

## Phase 0 start notes
- Start with .NET 10 API + React architecture baseline.
- Maintain a Django parity backlog so features can be replicated in an aside build.
- Resolve mixed-rules policy decisions before implementing core resolver logic.
