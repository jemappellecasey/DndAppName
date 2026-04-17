export type RuleSystemMode = 'Rules2014' | 'Rules2024'
export type AdvantageState = 'None' | 'Advantage' | 'Disadvantage'

export interface LocalSession {
  sessionToken: string
  userId: string
  userName: string
  createdAtUtc: string
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
