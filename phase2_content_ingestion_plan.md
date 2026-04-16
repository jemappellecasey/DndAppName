# Phase 2 Content Ingestion Plan

## Confirmed ingestion decisions
1. Build this phase first.
2. Generate **versioned JSON artifacts in repo first** (not DB-only direct writes).
3. Maintain manual review for low-confidence OCR-derived records.

## Ingestion pipeline (v1)
1. **Extract**
   - Inputs: `DnDPHB2014.md`, `DnDPHB2024.md`, `DnDDMG2014.md`, `DNDDMG2024.md`
   - Parse structural anchors (chapter/section/table/list blocks).
2. **Normalize**
   - Map records into canonical module shapes:
     - origins/backgrounds
     - species/races
     - classes/subclasses/features
     - feats
     - weapons/equipment
     - items + item effects (including attunement requirements)
3. **Score confidence**
   - Attach confidence score to each parsed record.
   - Route low-confidence records to review queue.
4. **Review**
   - Manual correction file for parser misses and OCR-noise remediation.
5. **Publish artifacts**
   - Write versioned JSON under `data\ingested\<source>\<version>\`.
   - Keep source-provenance pointers for each record.

## Proposed artifact layout
1. `data\ingested\phb2014\v1\*.json`
2. `data\ingested\phb2024\v1\*.json`
3. `data\ingested\dmg2014\v1\*.json`
4. `data\ingested\dmg2024\v1\*.json`
5. `data\review\pending\*.json`
6. `data\review\resolved\*.json`

## Minimal schemas to emit first
1. `rule_module.json`
2. `rule_variant.json`
3. `item_definition.json`
4. `item_effect.json`
5. `custom_guidance_constraints.json`

## Exit criteria for phase completion
1. All four source books produce versioned JSON artifacts.
2. Confidence scoring and review queue are operational.
3. Item effects and attunement metadata are included in normalized output.
4. Output is sufficient to feed phase-3 calculation engine inputs.
