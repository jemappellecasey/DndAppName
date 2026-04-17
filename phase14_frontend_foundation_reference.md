# Phase 14 Frontend Foundation Reference

## Implemented
1. React + TypeScript frontend scaffolded at `src\DndApp.Web`.
2. API client layer added (`src\DndApp.Web\src\api.ts`).
3. Shared frontend types added (`src\DndApp.Web\src\types.ts`).
4. Working app shell implemented for:
   - local session start
   - character wizard draft/finalize
   - character library actions
   - custom builder previews
   - inventory state update
   - roll check execution
5. API CORS policy added for local frontend dev (`localhost:5173`, `localhost:4173`).

## Next frontend slices
1. Break `App.tsx` into routed pages/components.
2. Replace hardcoded sample payloads with form-driven structured inputs.
3. Add persistent client state and request caching.
