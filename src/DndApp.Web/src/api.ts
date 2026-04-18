import type {
  CharacterBuildData,
  CharacterHistoryEntry,
  CharacterInventoryState,
  CharacterSummary,
  CharacterWizardResult,
  ClassCatalogItem,
  ContentSourceCatalogItem,
  ItemCatalogItem,
  LocalSession,
  ModuleCatalogItem,
  PersistedComputeCheckPayload,
  RuleModuleSelection,
  RuleSystemMode,
  UpsertCharacterBuildPayload,
} from './types'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080'
let sessionToken: string | null = null

export function setSessionToken(token: string | null) {
  sessionToken = token
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...(sessionToken ? { 'X-Session-Token': sessionToken } : {}),
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

export function registerLocal(userName: string, password: string) {
  return request<LocalSession>('/auth/local/register', {
    method: 'POST',
    body: JSON.stringify({ userName, password }),
  })
}

export function loginLocal(userName: string, password: string) {
  return request<LocalSession>('/auth/local/login', {
    method: 'POST',
    body: JSON.stringify({ userName, password }),
  })
}

export function getLocalMe() {
  return request<LocalSession>('/auth/local/me')
}

export function getCharacters(includeArchived = true, mine = false) {
  return request<CharacterSummary[]>(`/characters?includeArchived=${includeArchived}&mine=${mine}`)
}

export function getClassCatalog(ruleSystem: RuleSystemMode) {
  return request<ClassCatalogItem[]>(`/catalog/classes?ruleSystem=${ruleSystem}`)
}

export function getContentSources(ruleSystem: RuleSystemMode) {
  return request<ContentSourceCatalogItem[]>(`/catalog/content-sources?ruleSystem=${ruleSystem}`)
}

export function getModuleCatalog(input: {
  baseRuleSystem: RuleSystemMode
  mixedMode: boolean
  overlaySources: string[]
  moduleTypes: string[]
}) {
  const query = new URLSearchParams()
  query.set('baseRuleSystem', input.baseRuleSystem)
  query.set('mixedMode', String(input.mixedMode))
  for (const source of input.overlaySources) {
    query.append('overlaySources', source)
  }
  for (const moduleType of input.moduleTypes) {
    query.append('moduleTypes', moduleType)
  }

  return request<ModuleCatalogItem[]>(`/catalog/modules?${query.toString()}`)
}

export function getItemCatalog() {
  return request<ItemCatalogItem[]>('/catalog/items')
}

export function getAttunementGuidance() {
  return request<{ attunementCap: number; rules: string[] }>('/inventory/attunement-guidance')
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

export function getCharacterBuild(characterId: string) {
  return request<CharacterBuildData>(`/characters/${characterId}/build`)
}

export function upsertCharacterBuild(characterId: string, payload: UpsertCharacterBuildPayload) {
  return request<CharacterBuildData>(`/characters/${characterId}/build`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function getCharacterInventory(characterId: string) {
  return request<CharacterInventoryState>(`/characters/${characterId}/inventory`)
}

export function addInventoryItem(characterId: string, itemDefinitionId: string) {
  return request<CharacterInventoryState>(`/characters/${characterId}/inventory/items`, {
    method: 'POST',
    body: JSON.stringify({ itemDefinitionId }),
  })
}

export function patchInventoryItem(characterId: string, inventoryItemId: string, payload: { isEquipped?: boolean; isAttuned?: boolean }) {
  return request<CharacterInventoryState>(`/characters/${characterId}/inventory/items/${inventoryItemId}`, {
    method: 'PATCH',
    body: JSON.stringify(payload),
  })
}

export function removeInventoryItem(characterId: string, inventoryItemId: string) {
  return request<void>(`/characters/${characterId}/inventory/items/${inventoryItemId}`, {
    method: 'DELETE',
  })
}

export function computePersistedCheck(characterId: string, payload: PersistedComputeCheckPayload) {
  return request(`/characters/${characterId}/compute/check/persisted`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}
