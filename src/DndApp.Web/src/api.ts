import type {
  AdvantageState,
  CharacterHistoryEntry,
  CharacterSummary,
  CharacterWizardResult,
  LocalSession,
  RuleModuleSelection,
  RuleSystemMode,
} from './types'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080'

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...(init?.headers ?? {}),
    },
    ...init,
  })

  if (!response.ok) {
    const text = await response.text()
    throw new Error(text || `Request failed: ${response.status}`)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export function health() {
  return request<{ status: string; name?: string }>('/health')
}

export function loginLocal(userName: string) {
  return request<LocalSession>('/auth/local/login', {
    method: 'POST',
    body: JSON.stringify({ userName }),
  })
}

export function getCharacters(includeArchived = true) {
  return request<CharacterSummary[]>(`/characters?includeArchived=${includeArchived}`)
}

export function archiveCharacter(characterId: string) {
  return request<CharacterSummary>(`/characters/${characterId}/archive`, { method: 'POST' })
}

export function duplicateCharacter(characterId: string) {
  return request<CharacterSummary>(`/characters/${characterId}/duplicate`, {
    method: 'POST',
    body: JSON.stringify({ nameSuffix: 'Copy' }),
  })
}

export function getCharacterHistory(characterId: string) {
  return request<CharacterHistoryEntry[]>(`/characters/${characterId}/history`)
}

export function startWizard(input: {
  sessionToken: string
  characterName: string
  baseRuleSystem: RuleSystemMode
  mixedModeEnabled: boolean
  overlaySources: string[]
}) {
  return request<CharacterWizardResult>('/wizard/characters/start', {
    method: 'POST',
    body: JSON.stringify({
      sessionToken: input.sessionToken,
      characterName: input.characterName,
      rulesProfile: {
        baseRuleSystem: input.baseRuleSystem,
        mixedModeEnabled: input.mixedModeEnabled,
        overlaySources: input.overlaySources,
      },
    }),
  })
}

export function submitWizardStep(
  characterId: string,
  stepName: string,
  selections: RuleModuleSelection[],
) {
  return request<CharacterWizardResult>(`/wizard/characters/${characterId}/steps`, {
    method: 'POST',
    body: JSON.stringify({ stepName, selections }),
  })
}

export function finalizeWizard(characterId: string) {
  return request<CharacterWizardResult>(`/wizard/characters/${characterId}/finalize`, {
    method: 'POST',
    body: JSON.stringify({ explicitOverridesBySlot: {} }),
  })
}

export function copyToRuleset(characterId: string, targetRuleSystem: RuleSystemMode) {
  return request<CharacterWizardResult>(`/characters/${characterId}/copy-to-ruleset`, {
    method: 'POST',
    body: JSON.stringify({
      targetRuleSystem,
      carrySelectionsForward: true,
      targetOverlaySources: [],
    }),
  })
}

export function previewOrigin(payload: {
  name: string
  mode: 'GuidedCustom' | 'FullyCustom'
}) {
  return request('/custom/builders/origin/preview', {
    method: 'POST',
    body: JSON.stringify({
      name: payload.name,
      mode: payload.mode,
      abilityBonuses: [
        { ability: 'Wisdom', bonus: 2 },
        { ability: 'Dexterity', bonus: 1 },
      ],
      skillProficiencies: ['Perception', 'Stealth'],
      featureNotes: ['Trail sense'],
    }),
  })
}

export function previewSpecies(payload: {
  name: string
  mode: 'GuidedCustom' | 'FullyCustom'
}) {
  return request('/custom/builders/species/preview', {
    method: 'POST',
    body: JSON.stringify({
      name: payload.name,
      mode: payload.mode,
      size: 'Medium',
      walkingSpeed: 30,
      traits: ['Darkvision', 'Keen Senses'],
      languages: ['Common'],
    }),
  })
}

export function updateInventoryItemState(characterId: string) {
  return request(`/characters/${characterId}/inventory/update-item-state`, {
    method: 'POST',
    body: JSON.stringify({
      baseStats: {
        armorClass: 15,
        moveSpeed: 30,
        savingThrows: { Dexterity: 2 },
        abilityChecks: { Stealth: 3 },
        availableSpells: ['Mage Hand'],
      },
      itemId: 'cloak-1',
      isEquipped: true,
      isAttuned: true,
      items: [
        {
          itemId: 'cloak-1',
          itemName: 'Cloak of Protection',
          requiresAttunement: true,
          isEquipped: true,
          isAttuned: false,
          effects: [{ type: 'AcBonus', target: null, numericValue: 1, grantedSpell: null, description: '+1 AC' }],
        },
      ],
    }),
  })
}

export function computeCheck(characterId: string, advantageState: AdvantageState) {
  return request(`/characters/${characterId}/compute/check`, {
    method: 'POST',
    body: JSON.stringify({
      skillName: 'Stealth',
      abilityModifier: 3,
      proficiencyBonus: 2,
      isProficient: true,
      hasExpertise: true,
      additionalModifier: 0,
      advantageState,
      rollDice: true,
    }),
  })
}
