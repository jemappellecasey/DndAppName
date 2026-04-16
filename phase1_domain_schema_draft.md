# Phase 1 Domain Schema Draft

## Confirmed decisions used in this draft
1. Primary stack: **.NET 10 API + React**
2. Mixed precedence: **base rules win unless explicitly overridden**
3. Residual conflicts: **warn and auto-apply selected override**
4. Custom origin/species: **allowed in all modes; tag as nonstandard**
5. Ruleset change policy: **base ruleset locked after creation; allow export/copy to another ruleset with migration warnings**

## Domain model (v1)
1. `RuleSystem`
   - `Id`, `Name` (`2014`, `2024`), `IsActive`
2. `ContentSource`
   - `Id`, `Code` (`PHB2014`, `PHB2024`, `DMG2014`, `DMG2024`), `RuleSystemId`
3. `RuleModule`
   - `Id`, `Type` (`Class`, `Species`, `Origin`, `Feat`, `Spell`, `Weapon`, `Item`), `SourceId`, `Slug`, `Version`
4. `RuleVariant`
   - `Id`, `RuleModuleId`, `RuleSystemId`, `PayloadJson`, `CompatibilityTags`
5. `Prerequisite`
   - `Id`, `RuleModuleId`, `PredicateJson`
6. `Constraint`
   - `Id`, `Scope`, `ConstraintJson`

## Character aggregate
1. `Character`
   - `Id`, `UserId`, `Name`, `Level`, `BaseRuleSystemId`, `IsArchived`
2. `CharacterRulesProfile`
   - `CharacterId`, `OverlaySourcesJson`, `ExplicitOverridesJson`, `ConflictPolicy`
3. `CharacterSelection`
   - selected class/species/origin/feats/spells/equipment
4. `CharacterCustomization`
   - links to ad-hoc custom origin/species definitions and overrides

## Item and attunement model
1. `ItemDefinition`
   - `Id`, `Name`, `ItemType`, `Rarity`, `RequiresAttunement`, `ChargesModelJson`
2. `ItemEffect`
   - `Id`, `ItemId`, `EffectType`, `EffectPayloadJson`, `ConditionJson`
3. `CharacterItemState`
   - `CharacterId`, `ItemId`, `IsOwned`, `IsEquipped`, `IsAttuned`, `IsActive`

## Custom content model
1. `CustomOriginTemplate`
   - `Id`, `OwnerUserId`, `Name`, `IsGuided`, `DefinitionJson`
2. `CustomSpeciesTemplate`
   - `Id`, `OwnerUserId`, `Name`, `IsGuided`, `DefinitionJson`
3. `CustomContentTag`
   - `TargetType`, `TargetId`, `Tag` (`guided-custom`, `fully-custom`)

## Computation and provenance
1. `ModifierEntry`
   - source, stacking group, signed value, condition, priority
2. `ComputedSnapshot`
   - final derived stats and roll profiles
3. `ComputationProvenance`
   - winning source and overridden sources for each computed field

## API boundary (phase 1 contracts)
1. `POST /characters`
2. `POST /characters/{id}/copy-to-ruleset`
3. `POST /characters/{id}/compute/snapshot`
4. `POST /characters/{id}/compute/check`
5. `POST /characters/{id}/compute/save`
6. `POST /characters/{id}/compute/attack`
7. `POST /characters/{id}/items/{itemId}/equip`
8. `POST /characters/{id}/items/{itemId}/attune`
9. `POST /characters/{id}/custom/origin`
10. `POST /characters/{id}/custom/species`
