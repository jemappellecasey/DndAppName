export type RuleSystemMode = 'Rules2014' | 'Rules2024'
export type AdvantageState = 'None' | 'Advantage' | 'Disadvantage'
export type AbilityName = 'Strength' | 'Dexterity' | 'Constitution' | 'Intelligence' | 'Wisdom' | 'Charisma'
export type BuildMethod = 'PointBuy' | 'Manual' | 'Roll'
export type SkillName =
  | 'Acrobatics'
  | 'Animal Handling'
  | 'Arcana'
  | 'Athletics'
  | 'Deception'
  | 'History'
  | 'Insight'
  | 'Intimidation'
  | 'Investigation'
  | 'Medicine'
  | 'Nature'
  | 'Perception'
  | 'Performance'
  | 'Persuasion'
  | 'Religion'
  | 'Sleight of Hand'
  | 'Stealth'
  | 'Survival'

export interface LocalSession {
  sessionToken: string
  userId: string
  userName: string
  createdAtUtc: string
  expiresAtUtc: string
}

export interface CharacterSummary {
  characterId: string
  characterName: string
  baseRuleSystem: RuleSystemMode
  mixedModeEnabled: boolean
  isArchived: boolean
  createdAtUtc: string
  updatedAtUtc: string
}

export interface RuleModuleSelection {
  slot: string
  moduleId: string
  sourceCode: string
  compatible2014: boolean
  compatible2024: boolean
}

export interface CharacterWizardDraft {
  characterId: string
  ownerUserId: string
  characterName: string
  rulesProfile: {
    baseRuleSystem: RuleSystemMode
    mixedModeEnabled: boolean
    overlaySources: string[]
  }
  isFinalized: boolean
  warnings: string[]
}

export interface CharacterWizardResult {
  isSuccess: boolean
  draft: CharacterWizardDraft | null
  errors: string[]
  warnings: string[]
}

export interface CharacterHistoryEntry {
  timestampUtc: string
  action: string
  actorUserId: string
  details: string
}

export interface ValidationResult {
  isValid: boolean
  errors: string[]
  warnings: string[]
}

export interface ClassCatalogItem {
  moduleId: string
  className: string
  sourceCode: string
  versionTag: string
}

export interface ContentSourceCatalogItem {
  sourceCode: string
  sourceName: string
  ruleSystem: RuleSystemMode
}

export interface ModuleCatalogItem {
  moduleId: string
  moduleType: string
  displayName: string
  sourceCode: string
  versionTag: string
  abilityBonuses: Record<string, number>
  fixedSkillProficiencies: SkillName[]
  skillChoices: SkillName[]
  skillChoiceCount: number
  expertiseChoiceCount: number
  fixedToolProficiencies: string[]
  toolChoices: string[]
  toolChoiceCount: number
  fixedLanguages: string[]
  languageChoices: string[]
  languageChoiceCount: number
  minLevelRequirement: number
  abilityScoreRequirements: Record<string, number>
  spellClasses: string[]
  walkingSpeed: number | null
  parentClassModuleId: string | null
  subclassFeatureStartLevel: number | null
}

export interface ItemCatalogEffect {
  effectId: string
  effectType: string
  effectPayloadJson: string
  conditionJson: string
}

export interface ItemCatalogItem {
  itemId: string
  itemName: string
  sourceCode: string
  itemType: string
  rarity: string
  description?: string
  requiresAttunement: boolean
  goldValue: number
  weight: number
  isWeapon: boolean
  damageDice: string
  weaponAbility: string
  attackBonus: number
  damageBonus: number
  effects: ItemCatalogEffect[]
}

export interface ItemEffectInput {
  type: 'AcBonus' | 'MoveSpeedBonus' | 'SavingThrowBonus' | 'AbilityCheckBonus' | 'GrantSpell'
  target: string | null
  numericValue: number
  grantedSpell: string | null
  description: string
}

export interface CharacterItemState {
  itemId: string
  itemName: string
  requiresAttunement: boolean
  isEquipped: boolean
  isAttuned: boolean
  effects: ItemEffectInput[]
}

export interface BaseStatsPayload {
  armorClass: number
  moveSpeed: number
  savingThrows: Record<string, number>
  abilityChecks: Record<string, number>
  availableSpells: string[]
}

export interface UpdateInventoryItemStatePayload {
  baseStats: BaseStatsPayload
  items: CharacterItemState[]
  itemId: string
  isEquipped?: boolean
  isAttuned?: boolean
}

export interface InventoryStateUpdateResult {
  items: CharacterItemState[]
  pipelineResult: {
    derivedStats: {
      armorClass: number
      moveSpeed: number
      savingThrows: Record<string, number>
      abilityChecks: Record<string, number>
      availableSpells: string[]
    }
    breakdown: Array<{
      itemName: string
      effectType: string
      target: string
      value: number
      description: string
    }>
  }
  activeAttunementCount: number
  attunementCap: number
  warnings: string[]
  errors: string[]
}

export interface ComputeCheckPayload {
  skillName: SkillName
  abilityModifier: number
  proficiencyBonus: number
  isProficient: boolean
  hasExpertise: boolean
  additionalModifier: number
  advantageState: AdvantageState
  rollDice: boolean
}

export interface CharacterBuildData {
  characterId: string
  characterName: string
  baseRuleSystem: RuleSystemMode
  buildMethod: BuildMethod
  classModuleId: string
  className: string
  level: number
  proficiencyBonus: number
  abilityScores: Record<AbilityName, number>
  skillTrainingBySkill: Record<SkillName, 'None' | 'Proficient' | 'Expertise'>
  proficientSkills: SkillName[]
  saveProficiencies: AbilityName[]
  classLevels: Array<{
    classModuleId: string
    className: string
    level: number
    sortOrder: number
  }>
  selectedModules: Array<{
    slot: string
    moduleId: string
    displayName: string
    sourceCode: string
  }>
  createdAtUtc: string
  updatedAtUtc: string
}

export interface UpsertCharacterBuildPayload {
  characterName: string
  baseRuleSystem: RuleSystemMode
  buildMethod: BuildMethod
  classModuleId: string
  className: string
  level: number
  proficiencyBonus: number
  abilityScores: Record<AbilityName, number>
  proficientSkills: SkillName[]
  skillTrainingBySkill: Record<SkillName, 'None' | 'Proficient' | 'Expertise'>
  classLevels: Array<{
    classModuleId: string
    className: string
    level: number
    sortOrder: number
  }>
  selectedModules: Array<{
    slot: string
    moduleId: string
    displayName: string
    sourceCode: string
  }>
}

export interface CharacterInventoryItemData {
  inventoryItemId: string
  itemDefinitionId: string
  itemName: string
  itemType: string
  goldValue: number
  weight: number
  quantity: number
  requiresAttunement: boolean
  isEquipped: boolean
  isAttuned: boolean
  isWeapon: boolean
  damageDice: string
  weaponAbility: string
  attackBonus: number
  damageBonus: number
}

export interface CharacterInventoryState {
  characterId: string
  items: CharacterInventoryItemData[]
  pipelineResult: InventoryStateUpdateResult['pipelineResult']
  activeAttunementCount: number
  attunementCap: number
  warnings: string[]
  errors: string[]
}

export interface PersistedComputeCheckPayload {
  skillName: SkillName
  advantageState: AdvantageState
  rollDice: boolean
  additionalModifier: number
  hasExpertise: boolean
}

export interface CharacterSpellEntryData {
  spellModuleId: string
  spellName: string
  preparationMode: string
}

export interface CharacterSpellsData {
  characterId: string
  entries: CharacterSpellEntryData[]
}

export interface SpellSourceGroup {
  sourceName: string
  prepareCount: number
  automaticSpells: CharacterSpellEntryData[]
  selectableSpells: CharacterSpellEntryData[]
}

export interface RecommendedSpellsResult {
  characterId: string
  classModuleId: string
  classLevel: number
  spellSources: SpellSourceGroup[]
  advisoryMessage: string
  dataGap: string | null
}

export interface CharacterResourcePoolData {
  resourceKey: string
  currentValue: number
  maxValue: number
  metadataJson: string
}

export interface CharacterResourcesData {
  characterId: string
  resources: CharacterResourcePoolData[]
}

export interface CharacterCurrencyData {
  characterId: string
  cp: number
  sp: number
  ep: number
  gp: number
  pp: number
}

export interface CharacterVitalsData {
  characterId: string
  maxHitPoints: number
  currentHitPoints: number
  tempHitPoints: number
  baseMoveSpeed: number
  baseArmorClass: number
  updatedAtUtc: string
}

export interface AdvancedRulesSnapshotResponse {
  characterId: string
  extraAttackStacks: boolean
  armorClassResolution: string
  pactMagicTrackedSeparately: boolean
  dataGaps: string[]
}

export interface CharacterExperienceResponse {
  characterId: string
  currentLevel: number
  totalExperience: number
  experienceForNextLevel: number
  experienceTowardNextLevel: number
  abilityScoreImprovementsUsed: number
  availableAbilityScoreImprovements: number
  lastLevelUpAtUtc: string | null
  updatedAtUtc: string
}

export interface LevelUpGrantResponse {
  level: number
  experienceRequired: number
  grantsAbilityScoreImprovement: boolean
  grantsFeatOption: boolean
  leveledUpAtUtc: string
}

export interface LevelUpNotificationResponse {
  characterId: string
  oldLevel: number
  newLevel: number
  levelsGained: number
  levelUps: LevelUpGrantResponse[]
}

export interface AwardExperiencePayload {
  experienceAmount: number
}

export interface LevelUpChoice {
  id: string
  level: number
  choiceType: 'ASI' | 'Feat'
  chosenAbility?: string
  chosenFeatId?: string
  isConfirmed: boolean
  createdAt: string
}

export interface SaveLevelUpChoicePayload {
  level: number
  choiceType: 'ASI' | 'Feat'
  chosenAbility?: string
  chosenFeatId?: string
}

export interface ConfirmLevelUpChoicesPayload {
  level: number
}
