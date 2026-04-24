import { useEffect, useMemo, useState } from 'react'
import './App.css'
import {
  addInventoryItem,
  archiveCharacter,
  consolidateCharacterCurrency,
  convertCharacterCurrency,
  copyToRuleset,
  deleteCharacter,
  duplicateCharacter,
  finalizeWizard,
  getArchivedCharacters,
  getCharacterBuild,
  getCharacterHistory,
  getCharacterInventory,
  getCharacterCurrency,
  getCharacterResources,
  getCharacterSpells,
  getCharacterVitals,
  getCharacters,
  getClassCatalog,
  getContentSources,
  getRecommendedSpells,
  getModuleCatalog,
  getItemCatalog,
  health,
  loginLocal,
  registerLocal,
  patchInventoryItem,
  purchaseFromCharacterCurrency,
  removeInventoryItem,
  restoreCharacter,
  setSessionToken,
  startWizard,
  submitWizardStep,
  upsertCharacterResources,
  upsertCharacterCurrency,
  upsertCharacterBuild,
  upsertCharacterSpells,
  upsertCharacterVitals,
} from './api'
import type {
  AbilityName,
  BuildMethod,
  CharacterBuildData,
  CharacterHistoryEntry,
  CharacterInventoryState,
  CharacterCurrencyData,
  CharacterResourcePoolData,
  CharacterSpellEntryData,
  CharacterSummary,
  CharacterVitalsData,
  CharacterWizardResult,
  ClassCatalogItem,
  ItemCatalogItem,
  LocalSession,
  ModuleCatalogItem,
  RuleSystemMode,
  SkillName,
  UpsertCharacterBuildPayload,
} from './types'
import CharactersPage from './pages/CharactersPage'
import ArchivedCharactersPage from './pages/ArchivedCharactersPage'
import SettingsPage from './pages/SettingsPage'

const ABILITIES: AbilityName[] = ['Strength', 'Dexterity', 'Constitution', 'Intelligence', 'Wisdom', 'Charisma']

const ALL_SKILLS: SkillName[] = [
  'Acrobatics',
  'Animal Handling',
  'Arcana',
  'Athletics',
  'Deception',
  'History',
  'Insight',
  'Intimidation',
  'Investigation',
  'Medicine',
  'Nature',
  'Perception',
  'Performance',
  'Persuasion',
  'Religion',
  'Sleight of Hand',
  'Stealth',
  'Survival',
]

const SKILL_ABILITY: Record<SkillName, AbilityName> = {
  Acrobatics: 'Dexterity',
  'Animal Handling': 'Wisdom',
  Arcana: 'Intelligence',
  Athletics: 'Strength',
  Deception: 'Charisma',
  History: 'Intelligence',
  Insight: 'Wisdom',
  Intimidation: 'Charisma',
  Investigation: 'Intelligence',
  Medicine: 'Wisdom',
  Nature: 'Intelligence',
  Perception: 'Wisdom',
  Performance: 'Charisma',
  Persuasion: 'Charisma',
  Religion: 'Intelligence',
  'Sleight of Hand': 'Dexterity',
  Stealth: 'Dexterity',
  Survival: 'Wisdom',
}

type SkillTrainingLevel = 'None' | 'Proficient' | 'Expertise'

const DEFAULT_SCORES: Record<AbilityName, number> = {
  Strength: 8,
  Dexterity: 8,
  Constitution: 8,
  Intelligence: 8,
  Wisdom: 8,
  Charisma: 8,
}

const POINT_BUY_COST: Record<number, number> = { 8: 0, 9: 1, 10: 2, 11: 3, 12: 4, 13: 5, 14: 7, 15: 9 }
const DEFAULT_VITALS: Omit<CharacterVitalsData, 'characterId' | 'updatedAtUtc'> = {
  maxHitPoints: 1,
  currentHitPoints: 1,
  tempHitPoints: 0,
  baseMoveSpeed: 30,
  baseArmorClass: 10,
}
const KNOWN_CLASSES = new Set([
  'Artificer',
  'Barbarian',
  'Bard',
  'Cleric',
  'Druid',
  'Fighter',
  'Monk',
  'Paladin',
  'Ranger',
  'Rogue',
  'Sorcerer',
  'Warlock',
  'Wizard',
])
const KNOWN_RACES = new Set([
  'Aasimar',
  'Dragonborn',
  'Dwarf',
  'Elf',
  'Gnome',
  'Goliath',
  'Halfling',
  'Human',
  'Orc',
  'Tiefling',
  'Half-Elf',
  'Half-Orc',
])
const CURRENCY_RESOURCE_KEYS = new Set(['cp', 'sp', 'ep', 'gp', 'pp'])
const THEME_STORAGE_KEY = 'dndapp-theme'
const DEFAULT_THEME = 'pulse'
const CLASS_SPELLCASTING_ABILITY: Partial<Record<string, AbilityName>> = {
  artificer: 'Intelligence',
  bard: 'Charisma',
  cleric: 'Wisdom',
  druid: 'Wisdom',
  paladin: 'Charisma',
  ranger: 'Wisdom',
  sorcerer: 'Charisma',
  warlock: 'Charisma',
  wizard: 'Intelligence',
}
const FULL_CASTER_SLOTS_BY_LEVEL: ReadonlyArray<ReadonlyArray<number>> = [
  [],
  [2, 0, 0, 0, 0, 0, 0, 0, 0],
  [3, 0, 0, 0, 0, 0, 0, 0, 0],
  [4, 2, 0, 0, 0, 0, 0, 0, 0],
  [4, 3, 0, 0, 0, 0, 0, 0, 0],
  [4, 3, 2, 0, 0, 0, 0, 0, 0],
  [4, 3, 3, 0, 0, 0, 0, 0, 0],
  [4, 3, 3, 1, 0, 0, 0, 0, 0],
  [4, 3, 3, 2, 0, 0, 0, 0, 0],
  [4, 3, 3, 3, 1, 0, 0, 0, 0],
  [4, 3, 3, 3, 2, 0, 0, 0, 0],
  [4, 3, 3, 3, 2, 1, 0, 0, 0],
  [4, 3, 3, 3, 2, 1, 0, 0, 0],
  [4, 3, 3, 3, 2, 1, 1, 0, 0],
  [4, 3, 3, 3, 2, 1, 1, 0, 0],
  [4, 3, 3, 3, 2, 1, 1, 1, 0],
  [4, 3, 3, 3, 2, 1, 1, 1, 0],
  [4, 3, 3, 3, 2, 1, 1, 1, 1],
  [4, 3, 3, 3, 3, 1, 1, 1, 1],
  [4, 3, 3, 3, 3, 2, 1, 1, 1],
  [4, 3, 3, 3, 3, 2, 2, 1, 1],
]
const HALF_CASTER_SLOTS_BY_LEVEL: ReadonlyArray<ReadonlyArray<number>> = [
  [],
  [0, 0, 0, 0, 0],
  [2, 0, 0, 0, 0],
  [3, 0, 0, 0, 0],
  [3, 0, 0, 0, 0],
  [4, 2, 0, 0, 0],
  [4, 2, 0, 0, 0],
  [4, 3, 0, 0, 0],
  [4, 3, 0, 0, 0],
  [4, 3, 2, 0, 0],
  [4, 3, 2, 0, 0],
  [4, 3, 3, 0, 0],
  [4, 3, 3, 0, 0],
  [4, 3, 3, 1, 0],
  [4, 3, 3, 1, 0],
  [4, 3, 3, 2, 0],
  [4, 3, 3, 2, 0],
  [4, 3, 3, 3, 1],
  [4, 3, 3, 3, 1],
  [4, 3, 3, 3, 2],
  [4, 3, 3, 3, 2],
]
const ARTIFICER_SLOTS_BY_LEVEL: ReadonlyArray<ReadonlyArray<number>> = [
  [],
  [2, 0, 0, 0, 0],
  [2, 0, 0, 0, 0],
  [3, 0, 0, 0, 0],
  [3, 0, 0, 0, 0],
  [4, 2, 0, 0, 0],
  [4, 2, 0, 0, 0],
  [4, 3, 0, 0, 0],
  [4, 3, 0, 0, 0],
  [4, 3, 2, 0, 0],
  [4, 3, 2, 0, 0],
  [4, 3, 3, 0, 0],
  [4, 3, 3, 0, 0],
  [4, 3, 3, 1, 0],
  [4, 3, 3, 1, 0],
  [4, 3, 3, 2, 0],
  [4, 3, 3, 2, 0],
  [4, 3, 3, 3, 1],
  [4, 3, 3, 3, 1],
  [4, 3, 3, 3, 2],
  [4, 3, 3, 3, 2],
]

function abilityModifier(score: number) {
  return Math.floor((score - 10) / 2)
}

function rollAbilityValues(rerollOnes: boolean): number[] {
  return ABILITIES.map(() => {
    const rolls = Array.from({ length: 4 }, () => {
      let die = Math.floor(Math.random() * 6) + 1
      if (rerollOnes && die === 1) {
        die = Math.floor(Math.random() * 6) + 1
      }
      return die
    }).sort((a, b) => a - b)
    return rolls[1] + rolls[2] + rolls[3]
  }).sort((a, b) => b - a)
}

function getDisplayItemName(itemName: string, itemId: string): string {
  if (!itemName || itemName.trim().length === 0 || itemName.startsWith('Page ')) {
    return itemId
  }

  return itemName
}

function rulesetLabel(ruleSystem: RuleSystemMode): string {
  return ruleSystem === 'Rules2024' ? '2024 rules' : '2014 rules'
}

function mixedModeLabel(baseRuleSystem: RuleSystemMode, mixedModeEnabled: boolean): string {
  if (!mixedModeEnabled) {
    return 'Single ruleset'
  }

  return baseRuleSystem === 'Rules2024' ? 'Plus 2014 content' : 'Plus 2024 content'
}

function parseHitDieSides(className: string): number {
  const normalized = className.trim().toLowerCase()
  if (normalized === 'barbarian') return 12
  if (normalized === 'fighter' || normalized === 'paladin' || normalized === 'ranger') return 10
  if (normalized === 'artificer' || normalized === 'bard' || normalized === 'cleric' || normalized === 'druid' || normalized === 'monk' || normalized === 'rogue' || normalized === 'warlock') return 8
  if (normalized === 'sorcerer' || normalized === 'wizard') return 6
  return 8
}

function rollHitPointsForLevel(level: number, hitDieSides: number, generousRolls: boolean): number {
  const boundedLevel = Math.max(1, Math.trunc(level))
  const boundedSides = Math.max(1, Math.trunc(hitDieSides))
  const averageFloor = Math.floor(boundedSides / 2) + 1
  let total = boundedSides
  for (let i = 2; i <= boundedLevel; i += 1) {
    let roll = Math.floor(Math.random() * boundedSides) + 1
    if (generousRolls && roll < averageFloor) {
      roll = averageFloor
    }
    total += roll
  }
  return total
}

function sourceMatchesBaseRules(sourceCode: string, baseRules: RuleSystemMode): boolean {
  if (!sourceCode) {
    return true
  }

  const normalized = sourceCode.toLowerCase()
  return baseRules === 'Rules2014' ? normalized.includes('2014') : normalized.includes('2024')
}

function toSpellSlotResources(slotCounts: ReadonlyArray<number>): CharacterResourcePoolData[] {
  return slotCounts.flatMap((maxValue, index) =>
    maxValue > 0
      ? [
          {
            resourceKey: `spell-slot-${index + 1}`,
            currentValue: maxValue,
            maxValue,
            metadataJson: '{"source":"auto"}',
          },
        ]
      : [],
  )
}

function getDefaultSpellSlotResources(className: string, classLevel: number): CharacterResourcePoolData[] {
  const level = Math.max(1, Math.min(20, Math.trunc(classLevel)))
  const normalized = className.trim().toLowerCase()
  const fullCasterClasses = new Set(['bard', 'cleric', 'druid', 'sorcerer', 'wizard'])
  const halfCasterClasses = new Set(['paladin', 'ranger'])
  if (fullCasterClasses.has(normalized)) {
    return toSpellSlotResources(FULL_CASTER_SLOTS_BY_LEVEL[level] ?? [])
  }
  if (halfCasterClasses.has(normalized)) {
    return toSpellSlotResources(HALF_CASTER_SLOTS_BY_LEVEL[level] ?? [])
  }
  if (normalized === 'artificer') {
    return toSpellSlotResources(ARTIFICER_SLOTS_BY_LEVEL[level] ?? [])
  }
  if (normalized === 'warlock') {
    const pactSlotCount = level >= 17 ? 4 : level >= 11 ? 3 : 2
    const pactSlotLevel = level >= 9 ? 5 : level >= 7 ? 4 : level >= 5 ? 3 : level >= 3 ? 2 : 1
    return [
      {
        resourceKey: `pact-slot-level-${pactSlotLevel}`,
        currentValue: pactSlotCount,
        maxValue: pactSlotCount,
        metadataJson: '{"source":"auto"}',
      },
    ]
  }

  return []
}

function App() {
  const [status, setStatus] = useState('Checking API...')
  const [session, setSession] = useState<LocalSession | null>(null)
  const [loginName, setLoginName] = useState('')
  const [loginPassword, setLoginPassword] = useState('')
  const [isRegisterMode, setIsRegisterMode] = useState(false)
  const [characters, setCharacters] = useState<CharacterSummary[]>([])
  const [archivedCharacters, setArchivedCharacters] = useState<CharacterSummary[]>([])
  const [selectedCharacterId, setSelectedCharacterId] = useState('')
  const [history, setHistory] = useState<CharacterHistoryEntry[]>([])
  const [classSummaryByCharacterId, setClassSummaryByCharacterId] = useState<Record<string, string>>({})
  const [error, setError] = useState('')
  const [currentPath, setCurrentPath] = useState(window.location.pathname || '/login')
  const [themeName, setThemeName] = useState<'pulse' | 'zephyr'>(() => {
    const stored = window.localStorage.getItem(THEME_STORAGE_KEY)
    return stored === 'pulse' || stored === 'zephyr' ? stored : DEFAULT_THEME
  })

  const [wizardName, setWizardName] = useState('')
  const [baseRules, setBaseRules] = useState<RuleSystemMode>('Rules2024')
  const [mixedMode, setMixedMode] = useState(false)
  const [overlaySources, setOverlaySources] = useState<string[]>([])
  const [activeDraft, setActiveDraft] = useState<CharacterWizardResult | null>(null)

  const [classCatalog, setClassCatalog] = useState<ClassCatalogItem[]>([])
  const [moduleCatalog, setModuleCatalog] = useState<ModuleCatalogItem[]>([])
  const [selectedClassModuleId, setSelectedClassModuleId] = useState('')
  const [selectedSubclassModuleId, setSelectedSubclassModuleId] = useState('')
  const [selectedRaceModuleId, setSelectedRaceModuleId] = useState('')
  const [selectedSubraceModuleId, setSelectedSubraceModuleId] = useState('')
  const [selectedBackgroundModuleId, setSelectedBackgroundModuleId] = useState('')
  const [secondaryClassModuleId, setSecondaryClassModuleId] = useState('')
  const [multiClassSelections, setMultiClassSelections] = useState<
    Array<{ moduleId: string; level: number; subclassModuleId?: string }>
  >([])
  const [classCatalogResult, setClassCatalogResult] = useState('')
  const [newCharacterStep, setNewCharacterStep] = useState(1)

  const [buildMethod, setBuildMethod] = useState<BuildMethod>('PointBuy')
  const [rerollOnes, setRerollOnes] = useState(false)
  const [manualScores, setManualScores] = useState<Record<AbilityName, number>>({ ...DEFAULT_SCORES })
  const [pointBuyScores, setPointBuyScores] = useState<Record<AbilityName, number>>({ ...DEFAULT_SCORES })
  const [rolledPool, setRolledPool] = useState<number[]>([])
  const [rollAssignments, setRollAssignments] = useState<Partial<Record<AbilityName, number>>>({})
  const [primaryClassLevel, setPrimaryClassLevel] = useState(1)
  const [skillTrainingBySkill, setSkillTrainingBySkill] = useState<Record<SkillName, SkillTrainingLevel>>(
    () =>
      Object.fromEntries(ALL_SKILLS.map((skill) => [skill, 'None'])) as Record<SkillName, SkillTrainingLevel>,
  )
  const [savedBuild, setSavedBuild] = useState<CharacterBuildData | null>(null)
  const [buildResult, setBuildResult] = useState('')
  const [selectedToolPicks, setSelectedToolPicks] = useState<string[]>([])
  const [selectedLanguagePicks, setSelectedLanguagePicks] = useState<string[]>([])

  const [itemCatalog, setItemCatalog] = useState<ItemCatalogItem[]>([])
  const [selectedCatalogItemId, setSelectedCatalogItemId] = useState('')
  const [selectedCatalogQuantity, setSelectedCatalogQuantity] = useState(1)
  const [purchaseFromCurrencyMode, setPurchaseFromCurrencyMode] = useState(false)
  const [inventoryState, setInventoryState] = useState<CharacterInventoryState | null>(null)
  const [currencyState, setCurrencyState] = useState<CharacterCurrencyData | null>(null)
  const [currencyDraft, setCurrencyDraft] = useState({ cp: 0, sp: 0, ep: 0, gp: 0, pp: 0 })
  const [currencyConvert, setCurrencyConvert] = useState({ fromDenomination: 'gp', toDenomination: 'sp', amount: 1 })
  const [usePlatinumConsolidation, setUsePlatinumConsolidation] = useState(false)
  const [startingEquipmentMode, setStartingEquipmentMode] = useState<'package' | 'gold-only'>('package')

  const [spellEntries, setSpellEntries] = useState<CharacterSpellEntryData[]>([])
  const [recommendedSpellsByClass, setRecommendedSpellsByClass] = useState<Record<string, string>>({})
  const [resourcePools, setResourcePools] = useState<CharacterResourcePoolData[]>([])
  const [vitals, setVitals] = useState<Omit<CharacterVitalsData, 'characterId' | 'updatedAtUtc'>>({
    ...DEFAULT_VITALS,
  })
  const [sheetResult, setSheetResult] = useState('')
  const [characterNotes, setCharacterNotes] = useState('')
  const [viewSkillSort, setViewSkillSort] = useState<'name' | 'ability'>('ability')
  const [maxHpMethod, setMaxHpMethod] = useState<'manual' | 'roll'>('manual')
  const [generousHitPointRolls, setGenerousHitPointRolls] = useState(false)
  const [deathSaveSuccesses, setDeathSaveSuccesses] = useState(0)
  const [deathSaveFailures, setDeathSaveFailures] = useState(0)
  const [viewEditMode, setViewEditMode] = useState(false)
  const [acMode, setAcMode] = useState<'manual' | 'calculated'>('calculated')
  const [defaultBuildSnapshot, setDefaultBuildSnapshot] = useState<CharacterBuildData | null>(null)
  const [defaultVitalsSnapshot, setDefaultVitalsSnapshot] = useState<Omit<CharacterVitalsData, 'characterId' | 'updatedAtUtc'>>({
    ...DEFAULT_VITALS,
  })
  const [defaultResourcesSnapshot, setDefaultResourcesSnapshot] = useState<CharacterResourcePoolData[]>([])

  const currentCharacterId = useMemo(
    () => selectedCharacterId || activeDraft?.draft?.characterId || '',
    [selectedCharacterId, activeDraft?.draft?.characterId],
  )
  const isCharactersRoute = currentPath === '/' || currentPath === '/characters'
  const isArchivedRoute = currentPath === '/characters/archived'
  const isNewCharacterRoute = currentPath.startsWith('/characters/new')
  const isCharacterViewRoute = currentPath.startsWith('/characters/view/')
  const isSettingsRoute = currentPath === '/settings'
  const viewCharacterIdFromPath = useMemo(
    () => (isCharacterViewRoute ? currentPath.replace('/characters/view/', '') : ''),
    [currentPath, isCharacterViewRoute],
  )

  const classOptions = useMemo(() => {
    const allClassOptions = moduleCatalog.filter(
      (x) => x.moduleType.toLowerCase() === 'class' && KNOWN_CLASSES.has(x.displayName),
    )
    const byClassName = new Map<string, ModuleCatalogItem>()
    for (const item of allClassOptions) {
      const key = item.displayName.trim().toLowerCase()
      const current = byClassName.get(key)
      if (!current) {
        byClassName.set(key, item)
        continue
      }

      const currentIsPhb = current.sourceCode.toLowerCase().includes('phb')
      const nextIsPhb = item.sourceCode.toLowerCase().includes('phb')
      if (!currentIsPhb && nextIsPhb) {
        byClassName.set(key, item)
      }
    }

    return Array.from(byClassName.values()).sort((a, b) => a.displayName.localeCompare(b.displayName))
  }, [moduleCatalog])
  const mainClassOptions = useMemo(() => {
    const byClassName = new Map<string, ClassCatalogItem>()
    for (const item of classCatalog) {
      const key = item.className.trim().toLowerCase()
      const current = byClassName.get(key)
      if (!current) {
        byClassName.set(key, item)
        continue
      }

      const currentIsPhb = current.sourceCode.toLowerCase().includes('phb')
      const nextIsPhb = item.sourceCode.toLowerCase().includes('phb')
      if (!currentIsPhb && nextIsPhb) {
        byClassName.set(key, item)
      }
    }

    return Array.from(byClassName.values()).sort((a, b) => a.className.localeCompare(b.className))
  }, [classCatalog])
  const classNameByModuleId = useMemo(() => {
    const map = new Map<string, string>()
    for (const item of mainClassOptions) {
      map.set(item.moduleId.toLowerCase(), item.className)
    }
    for (const item of classOptions) {
      if (!map.has(item.moduleId.toLowerCase())) {
        map.set(item.moduleId.toLowerCase(), item.displayName)
      }
    }
    return map
  }, [classOptions, mainClassOptions])
  const subclassOptions = useMemo(
    () =>
      moduleCatalog.filter((x) => {
        if (x.moduleType.toLowerCase() !== 'subclass') return false
        if (mixedMode) return true
        return baseRules === 'Rules2014'
          ? x.sourceCode.toLowerCase().includes('2014')
          : x.sourceCode.toLowerCase().includes('2024')
      }),
    [baseRules, mixedMode, moduleCatalog],
  )
  const raceOptions = useMemo(
    () =>
      moduleCatalog.filter(
        (x) =>
          (x.moduleType.toLowerCase() === 'race' || x.moduleType.toLowerCase() === 'species') &&
          KNOWN_RACES.has(x.displayName),
      ),
    [moduleCatalog],
  )
  const backgroundOptions = useMemo(
    () =>
      moduleCatalog.filter(
        (x) => x.moduleType.toLowerCase() === 'background' || x.moduleType.toLowerCase() === 'origin',
      ),
    [moduleCatalog],
  )
  const selectedClassOption = useMemo(
    () => classOptions.find((x) => x.moduleId === selectedClassModuleId) ?? null,
    [classOptions, selectedClassModuleId],
  )
  const effectiveSelectedRaceModuleId = useMemo(
    () =>
      selectedRaceModuleId && raceOptions.some((x) => x.moduleId === selectedRaceModuleId)
        ? selectedRaceModuleId
        : (raceOptions[0]?.moduleId ?? ''),
    [selectedRaceModuleId, raceOptions],
  )
  const effectiveSelectedBackgroundModuleId = useMemo(
    () =>
      selectedBackgroundModuleId && backgroundOptions.some((x) => x.moduleId === selectedBackgroundModuleId)
        ? selectedBackgroundModuleId
        : (backgroundOptions[0]?.moduleId ?? ''),
    [selectedBackgroundModuleId, backgroundOptions],
  )
  const selectedBackgroundOption = useMemo(
    () => backgroundOptions.find((x) => x.moduleId === effectiveSelectedBackgroundModuleId) ?? null,
    [backgroundOptions, effectiveSelectedBackgroundModuleId],
  )
  const selectedRaceOption = useMemo(
    () => raceOptions.find((x) => x.moduleId === effectiveSelectedRaceModuleId) ?? null,
    [raceOptions, effectiveSelectedRaceModuleId],
  )
  const subraceOptions = useMemo(
    () =>
      moduleCatalog.filter((x) => {
        if (x.moduleType.toLowerCase() !== 'subrace') return false
        if (!selectedRaceOption) return false
        return x.displayName.toLowerCase().includes(selectedRaceOption.displayName.toLowerCase())
      }),
    [moduleCatalog, selectedRaceOption],
  )
  const effectiveSelectedSubraceModuleId = useMemo(
    () =>
      selectedSubraceModuleId && subraceOptions.some((x) => x.moduleId === selectedSubraceModuleId)
        ? selectedSubraceModuleId
        : '',
    [selectedSubraceModuleId, subraceOptions],
  )

  const rolledScores = useMemo<Record<AbilityName, number>>(
    () =>
      ABILITIES.reduce(
        (acc, ability) => ({ ...acc, [ability]: rollAssignments[ability] ?? DEFAULT_SCORES[ability] }),
        { ...DEFAULT_SCORES },
      ),
    [rollAssignments],
  )

  const isRollAssignmentComplete = useMemo(
    () => ABILITIES.every((ability) => typeof rollAssignments[ability] === 'number') && rolledPool.length === 0,
    [rollAssignments, rolledPool],
  )

  const activeAbilityScores = useMemo<Record<AbilityName, number>>(() => {
    if (buildMethod === 'Manual') return manualScores
    if (buildMethod === 'Roll') return rolledScores
    return pointBuyScores
  }, [buildMethod, manualScores, pointBuyScores, rolledScores])

  const raceBonuses = useMemo(() => {
    const selected = raceOptions.find((x) => x.moduleId === effectiveSelectedRaceModuleId)
    if (selected && mixedMode && !sourceMatchesBaseRules(selected.sourceCode, baseRules)) {
      return {}
    }
    return selected?.abilityBonuses ?? {}
  }, [baseRules, effectiveSelectedRaceModuleId, mixedMode, raceOptions])

  const crossRulesetLineageBonusSuppressed = useMemo(() => {
    const selected = raceOptions.find((x) => x.moduleId === effectiveSelectedRaceModuleId)
    if (!selected || !mixedMode) {
      return false
    }

    return !sourceMatchesBaseRules(selected.sourceCode, baseRules)
  }, [baseRules, effectiveSelectedRaceModuleId, mixedMode, raceOptions])

  const backgroundBonuses = useMemo(() => {
    const selected = backgroundOptions.find((x) => x.moduleId === effectiveSelectedBackgroundModuleId)
    return selected?.abilityBonuses ?? {}
  }, [backgroundOptions, effectiveSelectedBackgroundModuleId])

  const totalAbilityScores = useMemo<Record<AbilityName, number>>(() => {
    const combined = { ...activeAbilityScores }
    for (const ability of ABILITIES) {
      const raceBonus = raceBonuses[ability] ?? 0
      const backgroundBonus = backgroundBonuses[ability] ?? 0
      combined[ability] = Math.max(1, Math.min(30, combined[ability] + raceBonus + backgroundBonus))
    }
    return combined
  }, [activeAbilityScores, raceBonuses, backgroundBonuses])

  const totalCharacterLevel = useMemo(
    () => primaryClassLevel + multiClassSelections.reduce((sum, entry) => sum + entry.level, 0),
    [primaryClassLevel, multiClassSelections],
  )
  const effectiveSelectedClassModuleId = useMemo(
    () =>
      mainClassOptions.some((x) => x.moduleId === selectedClassModuleId)
        ? selectedClassModuleId
        : (mainClassOptions[0]?.moduleId ?? ''),
    [mainClassOptions, selectedClassModuleId],
  )
  const selectedPrimaryClassName = useMemo(() => {
    const classFromCatalog = mainClassOptions.find((x) => x.moduleId === effectiveSelectedClassModuleId)?.className
    const classFromModules = classOptions.find((x) => x.moduleId === effectiveSelectedClassModuleId)?.displayName
    return classFromCatalog ?? classFromModules ?? ''
  }, [classOptions, effectiveSelectedClassModuleId, mainClassOptions])
  const selectedSubclassName = useMemo(
    () => subclassOptions.find((x) => x.moduleId === selectedSubclassModuleId)?.displayName ?? '',
    [selectedSubclassModuleId, subclassOptions],
  )
  const spellOriginNames = useMemo(() => {
    const origins = new Set<string>()
    if (selectedPrimaryClassName) {
      origins.add(selectedPrimaryClassName.toLowerCase())
    }
    for (const multiClass of multiClassSelections) {
      const name = classOptions.find((x) => x.moduleId === multiClass.moduleId)?.displayName
      if (name) {
        origins.add(name.toLowerCase())
      }
    }
    if (selectedSubclassName) {
      origins.add(selectedSubclassName.toLowerCase())
    }
    return origins
  }, [classOptions, multiClassSelections, selectedPrimaryClassName, selectedSubclassName])
  const availableSpellOptions = useMemo(() => {
    return moduleCatalog.filter((item) => {
      if (item.moduleType.toLowerCase() !== 'spell') {
        return false
      }
      if (!selectedPrimaryClassName && spellOriginNames.size === 0) {
        return true
      }
      if (item.spellClasses.length === 0) {
        return false
      }

      return item.spellClasses.some((className) => spellOriginNames.has(className.toLowerCase()))
    })
  }, [moduleCatalog, selectedPrimaryClassName, spellOriginNames])
  const filteredItemCatalog = useMemo(
    () => itemCatalog.filter((item) => mixedMode || sourceMatchesBaseRules(item.sourceCode, baseRules)),
    [baseRules, itemCatalog, mixedMode],
  )
  const effectiveSelectedCatalogItemId = useMemo(
    () =>
      selectedCatalogItemId && filteredItemCatalog.some((x) => x.itemId === selectedCatalogItemId)
        ? selectedCatalogItemId
        : (filteredItemCatalog[0]?.itemId ?? ''),
    [filteredItemCatalog, selectedCatalogItemId],
  )
  const primaryClassSubclassOptions = useMemo(
    () => {
      const selectedClassName = classNameByModuleId.get(effectiveSelectedClassModuleId.toLowerCase())?.toLowerCase() ?? ''
      return subclassOptions.filter((item) => {
        if (!item.parentClassModuleId) {
          return selectedClassName.length > 0 && item.displayName.toLowerCase().includes(selectedClassName)
        }
        if (item.parentClassModuleId.toLowerCase() === effectiveSelectedClassModuleId.toLowerCase()) {
          return true
        }
        const parentClassName = classNameByModuleId.get(item.parentClassModuleId.toLowerCase())?.toLowerCase() ?? ''
        if (selectedClassName && parentClassName && selectedClassName === parentClassName) {
          return true
        }
        return selectedClassName.length > 0 && item.displayName.toLowerCase().includes(selectedClassName)
      })
    },
    [classNameByModuleId, effectiveSelectedClassModuleId, subclassOptions],
  )
  const unlockedPrimarySubclassOptions = useMemo(
    () =>
      primaryClassSubclassOptions.filter((item) => {
        if (item.minLevelRequirement > primaryClassLevel) {
          return false
        }

        for (const [ability, required] of Object.entries(item.abilityScoreRequirements ?? {})) {
          const key = ability as AbilityName
          if ((totalAbilityScores[key] ?? 0) < required) {
            return false
          }
        }

        return true
      }),
    [primaryClassLevel, primaryClassSubclassOptions, totalAbilityScores],
  )
  const effectiveSelectedSubclassModuleId = useMemo(
    () =>
      selectedSubclassModuleId && primaryClassSubclassOptions.some((x) => x.moduleId === selectedSubclassModuleId)
        ? selectedSubclassModuleId
        : '',
    [selectedSubclassModuleId, primaryClassSubclassOptions],
  )

  const pointBuySpent = useMemo(
    () => ABILITIES.reduce((sum, ability) => sum + POINT_BUY_COST[pointBuyScores[ability]], 0),
    [pointBuyScores],
  )

  const proficiencyBonus = useMemo(() => Math.floor((totalCharacterLevel - 1) / 4) + 2, [totalCharacterLevel])

  const autoGrantedSkills = useMemo(
    () =>
      Array.from(
        new Set([
          ...(selectedClassOption?.fixedSkillProficiencies ?? []),
          ...(selectedBackgroundOption?.fixedSkillProficiencies ?? []),
          ...(selectedRaceOption?.fixedSkillProficiencies ?? []),
        ]),
      ) as SkillName[],
    [selectedBackgroundOption, selectedClassOption, selectedRaceOption],
  )
  const effectiveSkillTrainingBySkill = useMemo(() => {
    const next = { ...skillTrainingBySkill }
    for (const skill of autoGrantedSkills) {
      if (next[skill] === 'None') {
        next[skill] = 'Proficient'
      }
    }
    return next
  }, [autoGrantedSkills, skillTrainingBySkill])
  const selectableSkillChoices = useMemo(
    () =>
      Array.from(
        new Set([
          ...(selectedClassOption?.skillChoices ?? []),
          ...(selectedBackgroundOption?.skillChoices ?? []),
          ...(selectedRaceOption?.skillChoices ?? []),
        ]),
      ) as SkillName[],
    [selectedBackgroundOption, selectedClassOption, selectedRaceOption],
  )
  const skillChoiceCount = useMemo(
    () =>
      (selectedClassOption?.skillChoiceCount ?? 0) +
      (selectedBackgroundOption?.skillChoiceCount ?? 0) +
      (selectedRaceOption?.skillChoiceCount ?? 0),
    [selectedBackgroundOption, selectedClassOption, selectedRaceOption],
  )
  const expertiseSlotsAvailable = useMemo(
    () =>
      (selectedClassOption?.expertiseChoiceCount ?? 0) +
      (selectedBackgroundOption?.expertiseChoiceCount ?? 0) +
      (selectedRaceOption?.expertiseChoiceCount ?? 0),
    [selectedBackgroundOption, selectedClassOption, selectedRaceOption],
  )
  const proficiencySlotsAvailable = useMemo(() => autoGrantedSkills.length + skillChoiceCount, [autoGrantedSkills.length, skillChoiceCount])
  const proficiencySlotsUsed = useMemo(
    () => ALL_SKILLS.filter((skill) => effectiveSkillTrainingBySkill[skill] !== 'None').length,
    [effectiveSkillTrainingBySkill],
  )
  const nonAutoSkillPicksUsed = useMemo(
    () =>
      ALL_SKILLS.filter(
        (skill) => effectiveSkillTrainingBySkill[skill] !== 'None' && !autoGrantedSkills.some((x) => x.toLowerCase() === skill.toLowerCase()),
      ).length,
    [autoGrantedSkills, effectiveSkillTrainingBySkill],
  )
  const expertiseSlotsUsed = useMemo(
    () => ALL_SKILLS.filter((skill) => effectiveSkillTrainingBySkill[skill] === 'Expertise').length,
    [effectiveSkillTrainingBySkill],
  )
  const skillSections = useMemo(
    () =>
      [
        {
          key: 'class',
          title: selectedClassOption ? `${selectedClassOption.displayName} skill proficiencies` : 'Class skill proficiencies',
          fixed: selectedClassOption?.fixedSkillProficiencies ?? [],
          choices: selectedClassOption?.skillChoices ?? [],
          choiceCount: selectedClassOption?.skillChoiceCount ?? 0,
        },
        {
          key: 'race',
          title: selectedRaceOption ? `${selectedRaceOption.displayName} skill proficiencies` : 'Race skill proficiencies',
          fixed: selectedRaceOption?.fixedSkillProficiencies ?? [],
          choices: selectedRaceOption?.skillChoices ?? [],
          choiceCount: selectedRaceOption?.skillChoiceCount ?? 0,
        },
        {
          key: 'background',
          title: selectedBackgroundOption
            ? `${selectedBackgroundOption.displayName} skill proficiencies`
            : 'Background skill proficiencies',
          fixed: selectedBackgroundOption?.fixedSkillProficiencies ?? [],
          choices: selectedBackgroundOption?.skillChoices ?? [],
          choiceCount: selectedBackgroundOption?.skillChoiceCount ?? 0,
        },
      ].filter((section) => section.fixed.length > 0 || section.choices.length > 0),
    [selectedBackgroundOption, selectedClassOption, selectedRaceOption],
  )

  const fixedTools = useMemo(
    () =>
      Array.from(
        new Set([
          ...(selectedClassOption?.fixedToolProficiencies ?? []),
          ...(selectedBackgroundOption?.fixedToolProficiencies ?? []),
          ...(selectedRaceOption?.fixedToolProficiencies ?? []),
        ]),
      ),
    [selectedBackgroundOption, selectedClassOption, selectedRaceOption],
  )
  const selectableTools = useMemo(
    () =>
      Array.from(
        new Set([
          ...(selectedClassOption?.toolChoices ?? []),
          ...(selectedBackgroundOption?.toolChoices ?? []),
          ...(selectedRaceOption?.toolChoices ?? []),
        ]),
      ),
    [selectedBackgroundOption, selectedClassOption, selectedRaceOption],
  )
  const toolChoiceCount = useMemo(
    () =>
      (selectedClassOption?.toolChoiceCount ?? 0) +
      (selectedBackgroundOption?.toolChoiceCount ?? 0) +
      (selectedRaceOption?.toolChoiceCount ?? 0),
    [selectedBackgroundOption, selectedClassOption, selectedRaceOption],
  )

  const fixedLanguages = useMemo(
    () =>
      Array.from(
        new Set([
          ...(selectedClassOption?.fixedLanguages ?? []),
          ...(selectedBackgroundOption?.fixedLanguages ?? []),
          ...(selectedRaceOption?.fixedLanguages ?? []),
        ]),
      ),
    [selectedBackgroundOption, selectedClassOption, selectedRaceOption],
  )
  const selectableLanguages = useMemo(
    () =>
      Array.from(
        new Set([
          ...(selectedClassOption?.languageChoices ?? []),
          ...(selectedBackgroundOption?.languageChoices ?? []),
          ...(selectedRaceOption?.languageChoices ?? []),
        ]),
      ),
    [selectedBackgroundOption, selectedClassOption, selectedRaceOption],
  )
  const languageChoiceCount = useMemo(
    () =>
      (selectedClassOption?.languageChoiceCount ?? 0) +
      (selectedBackgroundOption?.languageChoiceCount ?? 0) +
      (selectedRaceOption?.languageChoiceCount ?? 0),
    [selectedBackgroundOption, selectedClassOption, selectedRaceOption],
  )

  const classSections = useMemo(() => {
    const primaryClass = classCatalog.find((x) => x.moduleId === effectiveSelectedClassModuleId)
    const primaryName = primaryClass?.className ?? classOptions.find((x) => x.moduleId === effectiveSelectedClassModuleId)?.displayName
    const primary = effectiveSelectedClassModuleId
      ? [{ moduleId: effectiveSelectedClassModuleId, className: primaryName ?? effectiveSelectedClassModuleId, level: primaryClassLevel }]
      : []

    const secondary = multiClassSelections.map((entry) => {
      const option = classOptions.find((x) => x.moduleId === entry.moduleId)
      return {
        moduleId: entry.moduleId,
        className: option?.displayName ?? entry.moduleId,
        level: entry.level,
      }
    })

    return [...primary, ...secondary]
  }, [classCatalog, classOptions, effectiveSelectedClassModuleId, multiClassSelections, primaryClassLevel])

  useEffect(() => {
    health()
      .then((r) => setStatus(`API online (${r.status})`))
      .catch(() => setStatus('API unreachable (start DndApp.Api on localhost:5080)'))
  }, [])

  useEffect(() => {
    let link = document.getElementById('app-theme') as HTMLLinkElement | null
    if (!link) {
      link = document.createElement('link')
      link.id = 'app-theme'
      link.rel = 'stylesheet'
      document.head.appendChild(link)
    }
    link.href = `/themes/${themeName}.min.css`
    window.localStorage.setItem(THEME_STORAGE_KEY, themeName)
  }, [themeName])

  useEffect(() => {
    const onPopState = () => {
      setCurrentPath(window.location.pathname || '/')
    }

    window.addEventListener('popstate', onPopState)
    return () => window.removeEventListener('popstate', onPopState)
  }, [])

  useEffect(() => {
    if (!session) {
      return
    }

    let cancelled = false

    if (isArchivedRoute) {
      getArchivedCharacters(true)
        .then((list) => {
          if (!cancelled) {
            setArchivedCharacters(list)
          }
        })
        .catch((e) => {
          if (!cancelled) {
            setError(String(e))
          }
        })
      return
    }

    if (isCharactersRoute) {
      getCharacters(false, true)
        .then(async (list) => {
          if (cancelled) {
            return
          }

          setCharacters(list)
          void refreshClassSummaries(list)
          if (!selectedCharacterId && list.length > 0) {
            setSelectedCharacterId(list[0].characterId)
            await loadPersistedCharacterState(list[0].characterId)
          }
        })
        .catch((e) => {
          if (!cancelled) {
            setError(String(e))
          }
        })
    }

    return () => {
      cancelled = true
    }
  }, [isArchivedRoute, isCharactersRoute, selectedCharacterId, session])

  function navigate(path: string) {
    if (window.location.pathname === path) {
      setCurrentPath(path)
      return
    }

    window.history.pushState({}, '', path)
    setCurrentPath(path)
  }

  async function loadCatalogData(ruleSystem: RuleSystemMode) {
    const [classes, items, sources] = await Promise.all([
      getClassCatalog(ruleSystem),
      getItemCatalog(),
      getContentSources(ruleSystem),
    ])
    const validOverlaySources = sources.map((src) => src.sourceCode)
    const effectiveOverlaySources = mixedMode ? validOverlaySources : []
    const modules = await getModuleCatalog({
      baseRuleSystem: ruleSystem,
      mixedMode,
      overlaySources: effectiveOverlaySources,
      moduleTypes: ['class', 'race', 'species', 'background', 'origin', 'subclass', 'feat', 'spell'],
    })
    const filteredRaceOptions = modules.filter(
      (x) =>
        (x.moduleType.toLowerCase() === 'race' || x.moduleType.toLowerCase() === 'species') &&
        KNOWN_RACES.has(x.displayName),
    )
    const filteredBackgroundOptions = modules.filter(
      (x) => x.moduleType.toLowerCase() === 'background' || x.moduleType.toLowerCase() === 'origin',
    )
    setClassCatalog(classes)
    setModuleCatalog(modules)
    setItemCatalog(items)
    setOverlaySources(effectiveOverlaySources)
    if (!selectedClassModuleId && classes.length > 0) {
      setSelectedClassModuleId(classes[0].moduleId)
    }
    const selectedRaceStillVisible = filteredRaceOptions.some((x) => x.moduleId === selectedRaceModuleId)
    if (!selectedRaceStillVisible) {
      const firstRace = filteredRaceOptions[0]
      if (firstRace) {
        setSelectedRaceModuleId(firstRace.moduleId)
        setVitals((prev) => ({
          ...prev,
          baseMoveSpeed: firstRace.walkingSpeed && firstRace.walkingSpeed > 0 ? firstRace.walkingSpeed : 30,
        }))
      }
    }
    const selectedBackgroundStillVisible = filteredBackgroundOptions.some((x) => x.moduleId === selectedBackgroundModuleId)
    if (!selectedBackgroundStillVisible) {
      const firstBackground = filteredBackgroundOptions[0]
      if (firstBackground) {
        setSelectedBackgroundModuleId(firstBackground.moduleId)
      }
    }
    if (!selectedCatalogItemId && items.length > 0) {
      setSelectedCatalogItemId(items[0].itemId)
    }
  }

  async function loadPersistedCharacterState(characterId: string) {
    try {
      const build = await getCharacterBuild(characterId)
      setSavedBuild(build)
      setBuildMethod(build.buildMethod)
      setWizardName(build.characterName)
      setBaseRules(build.baseRuleSystem)
      const summary = characters.find((entry) => entry.characterId === characterId)
      setMixedMode(summary?.mixedModeEnabled ?? false)
      const persistedClassLevels = build.classLevels.length > 0 ? build.classLevels : [{ classModuleId: build.classModuleId, className: build.className, level: build.level, sortOrder: 0 }]
      const primaryPersistedClass = persistedClassLevels[0]
      setSelectedClassModuleId(primaryPersistedClass.classModuleId)
      setPrimaryClassLevel(primaryPersistedClass.level)
      setMultiClassSelections(
        persistedClassLevels.slice(1).map((entry) => ({
          moduleId: entry.classModuleId,
          level: entry.level,
          subclassModuleId:
            build.selectedModules.find((x) => x.slot.toLowerCase() === `subclass:${entry.classModuleId.toLowerCase()}`)
              ?.moduleId ?? '',
        })),
      )
      const subclassModule = build.selectedModules.find((x) => x.slot.toLowerCase() === 'subclass')
      const raceModule = build.selectedModules.find((x) => x.slot.toLowerCase() === 'race' || x.slot.toLowerCase() === 'species')
      const subraceModule = build.selectedModules.find((x) => x.slot.toLowerCase() === 'subrace')
      const backgroundModule = build.selectedModules.find(
        (x) => x.slot.toLowerCase() === 'background' || x.slot.toLowerCase() === 'origin',
      )
      const toolPicks = build.selectedModules
        .filter((x) => x.slot.toLowerCase() === 'tool-proficiency')
        .map((x) => x.moduleId)
      const languagePicks = build.selectedModules
        .filter((x) => x.slot.toLowerCase() === 'language')
        .map((x) => x.moduleId)
      const equipmentMode = build.selectedModules.find((x) => x.slot.toLowerCase() === 'starting-equipment-mode')?.moduleId
      const notesModule = build.selectedModules.find((x) => x.slot.toLowerCase() === 'character-notes')
      const acModeModule = build.selectedModules.find((x) => x.slot.toLowerCase() === 'ac-mode')
      if (subclassModule) setSelectedSubclassModuleId(subclassModule.moduleId)
      if (raceModule) setSelectedRaceModuleId(raceModule.moduleId)
      if (subraceModule) setSelectedSubraceModuleId(subraceModule.moduleId)
      if (backgroundModule) setSelectedBackgroundModuleId(backgroundModule.moduleId)
      setSelectedToolPicks(toolPicks)
      setSelectedLanguagePicks(languagePicks)
      if (equipmentMode === 'gold-only') {
        setStartingEquipmentMode('gold-only')
      } else {
        setStartingEquipmentMode('package')
      }
      setCharacterNotes(notesModule?.displayName ?? '')
      setAcMode(acModeModule?.moduleId === 'manual' ? 'manual' : 'calculated')
      setSkillTrainingBySkill(() => {
        const next = Object.fromEntries(ALL_SKILLS.map((skill) => [skill, 'None'])) as Record<SkillName, SkillTrainingLevel>
        if (build.skillTrainingBySkill) {
          for (const [skill, level] of Object.entries(build.skillTrainingBySkill)) {
            next[skill as SkillName] = level as SkillTrainingLevel
          }
        } else {
          for (const skill of build.proficientSkills) {
            next[skill] = 'Proficient'
          }
        }
        return next
      })
      setManualScores(build.abilityScores)
      setPointBuyScores(build.abilityScores)
      setRollAssignments(build.abilityScores)
      setRolledPool([])
      setBuildResult('Loaded persisted character build.')
      setDefaultBuildSnapshot(build)
    } catch {
      setSavedBuild(null)
      setDefaultBuildSnapshot(null)
      setBuildResult('No persisted build for selected character yet.')
      setSelectedToolPicks([])
      setSelectedLanguagePicks([])
      setSelectedSubraceModuleId('')
      setStartingEquipmentMode('package')
      setCharacterNotes('')
      setAcMode('calculated')
    }

    try {
      const inventory = await getCharacterInventory(characterId)
      setInventoryState(inventory)
    } catch {
      setInventoryState(null)
    }

    try {
      const currency = await getCharacterCurrency(characterId)
      setCurrencyState(currency)
      setCurrencyDraft({ cp: currency.cp, sp: currency.sp, ep: currency.ep, gp: currency.gp, pp: currency.pp })
    } catch {
      setCurrencyState(null)
      setCurrencyDraft({ cp: 0, sp: 0, ep: 0, gp: 0, pp: 0 })
    }

    try {
      const spells = await getCharacterSpells(characterId)
      setSpellEntries(spells.entries)
    } catch {
      setSpellEntries([])
    }

    try {
      const resources = await getCharacterResources(characterId)
      setResourcePools(resources.resources)
      setDefaultResourcesSnapshot(resources.resources)
    } catch {
      setResourcePools([])
      setDefaultResourcesSnapshot([])
    }

    try {
      const nextVitals = await getCharacterVitals(characterId)
      setVitals({
        maxHitPoints: nextVitals.maxHitPoints,
        currentHitPoints: nextVitals.currentHitPoints,
        tempHitPoints: nextVitals.tempHitPoints,
        baseMoveSpeed: nextVitals.baseMoveSpeed,
        baseArmorClass: nextVitals.baseArmorClass,
      })
      setDefaultVitalsSnapshot({
        maxHitPoints: nextVitals.maxHitPoints,
        currentHitPoints: nextVitals.currentHitPoints,
        tempHitPoints: nextVitals.tempHitPoints,
        baseMoveSpeed: nextVitals.baseMoveSpeed,
        baseArmorClass: nextVitals.baseArmorClass,
      })
    } catch {
      setVitals({ ...DEFAULT_VITALS })
      setDefaultVitalsSnapshot({ ...DEFAULT_VITALS })
    }

    setSheetResult('')
    setDeathSaveSuccesses(0)
    setDeathSaveFailures(0)
    setViewEditMode(false)
  }

  async function refreshCharacters() {
    const list = await getCharacters(false, true)
    setCharacters(list)
    await refreshClassSummaries(list)
    if (!selectedCharacterId && list.length > 0) {
      setSelectedCharacterId(list[0].characterId)
      await loadPersistedCharacterState(list[0].characterId)
    }
  }

  async function refreshClassSummaries(list: CharacterSummary[]) {
    const next: Record<string, string> = {}
    await Promise.all(
      list.map(async (character) => {
        try {
          const build = await getCharacterBuild(character.characterId)
          const classSummary = build.classLevels.length > 0
            ? build.classLevels.map((entry) => `${entry.className} ${entry.level}`).join(', ')
            : `${build.className} ${build.level}`
          next[character.characterId] = classSummary
        } catch {
          next[character.characterId] = 'No class build saved'
        }
      }),
    )
    setClassSummaryByCharacterId(next)
  }

  async function refreshArchivedCharacters() {
    const list = await getArchivedCharacters(true)
    setArchivedCharacters(list)
  }

  async function handleSelectCharacter(characterId: string) {
    setSelectedCharacterId(characterId)
    setRecommendedSpellsByClass({})
    await loadPersistedCharacterState(characterId)
  }

  function handlePrimaryClassChange(nextClassModuleId: string) {
    setSelectedClassModuleId(nextClassModuleId)
    setMultiClassSelections((prev) => prev.filter((entry) => entry.moduleId !== nextClassModuleId))
  }

  function handleRaceModuleChange(nextRaceModuleId: string) {
    setSelectedRaceModuleId(nextRaceModuleId)
    const selectedRace = raceOptions.find((item) => item.moduleId === nextRaceModuleId)
    if (!selectedRace) {
      return
    }
    const walkingSpeed = selectedRace.walkingSpeed && selectedRace.walkingSpeed > 0 ? selectedRace.walkingSpeed : 30
    setVitals((prev) => ({ ...prev, baseMoveSpeed: walkingSpeed }))
  }

  async function handleLogin() {
    try {
      setError('')
      const result = isRegisterMode
        ? await registerLocal(loginName, loginPassword)
        : await loginLocal(loginName, loginPassword)
      setSession(result)
      setSessionToken(result.sessionToken)
      await refreshCharacters()
      await refreshArchivedCharacters()
      await loadCatalogData(baseRules)
      navigate('/characters')
    } catch (e) {
      setError(String(e))
    }
  }

  function handleLogout() {
    setSession(null)
    setSessionToken(null)
    setCharacters([])
    setClassSummaryByCharacterId({})
    setSelectedCharacterId('')
    setHistory([])
    setSavedBuild(null)
    setSelectedToolPicks([])
    setSelectedLanguagePicks([])
    setSelectedSubraceModuleId('')
    setInventoryState(null)
    setCurrencyState(null)
    setCurrencyDraft({ cp: 0, sp: 0, ep: 0, gp: 0, pp: 0 })
    setStartingEquipmentMode('package')
    setActiveDraft(null)
    setSpellEntries([])
    setResourcePools([])
    setRecommendedSpellsByClass({})
    setVitals({ ...DEFAULT_VITALS })
    setSheetResult('')
    setCharacterNotes('')
    setViewSkillSort('ability')
    setMaxHpMethod('manual')
    setGenerousHitPointRolls(false)
    setDeathSaveSuccesses(0)
    setDeathSaveFailures(0)
    setViewEditMode(false)
    setAcMode('calculated')
    setDefaultBuildSnapshot(null)
    setDefaultVitalsSnapshot({ ...DEFAULT_VITALS })
    setDefaultResourcesSnapshot([])
    setArchivedCharacters([])
    navigate('/login')
  }

  async function handleBaseRulesChange(ruleSystem: RuleSystemMode) {
    try {
      setBaseRules(ruleSystem)
      await loadCatalogData(ruleSystem)
    } catch (e) {
      setError(String(e))
    }
  }

  function resetCharacterCreationState() {
    setWizardName('')
    setBaseRules('Rules2024')
    setMixedMode(false)
    setOverlaySources([])
    setActiveDraft(null)
    setSelectedCharacterId('')
    setSelectedClassModuleId('')
    setSelectedSubclassModuleId('')
    setSelectedRaceModuleId('')
    setSelectedSubraceModuleId('')
    setSelectedBackgroundModuleId('')
    setSecondaryClassModuleId('')
    setMultiClassSelections([])
    setClassCatalogResult('')
    setNewCharacterStep(1)
    setBuildMethod('PointBuy')
    setRerollOnes(false)
    setManualScores({ ...DEFAULT_SCORES })
    setPointBuyScores({ ...DEFAULT_SCORES })
    setRolledPool([])
    setRollAssignments({})
    setPrimaryClassLevel(1)
    setSkillTrainingBySkill(
      Object.fromEntries(ALL_SKILLS.map((skill) => [skill, 'None'])) as Record<SkillName, SkillTrainingLevel>,
    )
    setSavedBuild(null)
    setBuildResult('')
    setSelectedToolPicks([])
    setSelectedLanguagePicks([])
    setSelectedCatalogItemId('')
    setSelectedCatalogQuantity(1)
    setPurchaseFromCurrencyMode(false)
    setInventoryState(null)
    setCurrencyState(null)
    setCurrencyDraft({ cp: 0, sp: 0, ep: 0, gp: 0, pp: 0 })
    setCurrencyConvert({ fromDenomination: 'gp', toDenomination: 'sp', amount: 1 })
    setUsePlatinumConsolidation(false)
    setStartingEquipmentMode('package')
    setSpellEntries([])
    setRecommendedSpellsByClass({})
    setResourcePools([])
    setVitals({ ...DEFAULT_VITALS })
    setSheetResult('')
    setCharacterNotes('')
    setViewSkillSort('ability')
    setMaxHpMethod('manual')
    setGenerousHitPointRolls(false)
    setDeathSaveSuccesses(0)
    setDeathSaveFailures(0)
    setViewEditMode(false)
    setAcMode('calculated')
    setDefaultBuildSnapshot(null)
    setDefaultVitalsSnapshot({ ...DEFAULT_VITALS })
    setDefaultResourcesSnapshot([])
  }

  async function ensureActiveDraft() {
    if (activeDraft?.draft) {
      return activeDraft
    }
    if (!session) {
      setError('Start a session before creating a character draft.')
      return null
    }

    const result = await startWizard({
      sessionToken: session.sessionToken,
      characterName: wizardName.trim() ? wizardName : null,
      baseRuleSystem: baseRules,
      mixedModeEnabled: mixedMode,
      overlaySources: mixedMode ? overlaySources : [],
    })
    setActiveDraft(result)
    if (result.draft?.characterId) {
      setSelectedCharacterId(result.draft.characterId)
    }
    return result
  }

  function startNewCharacterFlow() {
    resetCharacterCreationState()
    navigate('/characters/new')
  }

  async function handleApplySelectedClassToWizard() {
    if (!effectiveSelectedClassModuleId) return
    const selectedClass = classCatalog.find((x) => x.moduleId === effectiveSelectedClassModuleId)
    const selectedClassModule = classOptions.find((x) => x.moduleId === effectiveSelectedClassModuleId)
    if (!selectedClass && !selectedClassModule) return
    try {
      const draft = await ensureActiveDraft()
      const draftCharacterId = draft?.draft?.characterId
      if (!draftCharacterId) return
      const classSelections = [
        {
          slot: 'class',
          moduleId: effectiveSelectedClassModuleId,
          sourceCode: selectedClassModule?.sourceCode ?? selectedClass?.sourceCode ?? '',
          compatible2014: true,
          compatible2024: true,
        },
        ...multiClassSelections.map((entry) => {
          const option = classOptions.find((x) => x.moduleId === entry.moduleId)
          return {
            slot: 'class',
            moduleId: entry.moduleId,
            sourceCode: option?.sourceCode ?? '',
            compatible2014: true,
            compatible2024: true,
          }
        }),
      ]

      let result = await submitWizardStep(draftCharacterId, 'class', classSelections)

      if (effectiveSelectedRaceModuleId) {
        const race = raceOptions.find((x) => x.moduleId === effectiveSelectedRaceModuleId)
        if (race) {
          result = await submitWizardStep(draftCharacterId, 'race', [
            {
              slot: 'race',
              moduleId: race.moduleId,
              sourceCode: race.sourceCode,
              compatible2014: true,
              compatible2024: true,
            },
          ])
        }
      }

      if (effectiveSelectedSubraceModuleId) {
        const subrace = subraceOptions.find((x) => x.moduleId === effectiveSelectedSubraceModuleId)
        if (subrace) {
          result = await submitWizardStep(draftCharacterId, 'race', [
            {
              slot: 'subrace',
              moduleId: subrace.moduleId,
              sourceCode: subrace.sourceCode,
              compatible2014: true,
              compatible2024: true,
            },
          ])
        }
      }

      if (effectiveSelectedBackgroundModuleId) {
        const background = backgroundOptions.find((x) => x.moduleId === effectiveSelectedBackgroundModuleId)
        if (background) {
          result = await submitWizardStep(draftCharacterId, 'background', [
            {
              slot: 'background',
              moduleId: background.moduleId,
              sourceCode: background.sourceCode,
              compatible2014: true,
              compatible2024: true,
            },
          ])
        }
      }

      if (effectiveSelectedSubclassModuleId) {
        const subclass = primaryClassSubclassOptions.find((x) => x.moduleId === effectiveSelectedSubclassModuleId)
        if (subclass) {
          result = await submitWizardStep(draftCharacterId, 'subclass', [
            {
              slot: 'subclass',
              moduleId: subclass.moduleId,
              sourceCode: subclass.sourceCode,
              compatible2014: true,
              compatible2024: true,
            },
          ])
        }
      }

      setActiveDraft(result)
      setClassCatalogResult(
        `Submitted selections: primary class + ${multiClassSelections.length} multiclass entries, subclass/race/background selections.`,
      )
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleFinalizeWizard() {
    try {
      const draft = await ensureActiveDraft()
      const draftCharacterId = draft?.draft?.characterId
      if (!draftCharacterId) return
      setError('')
      const result = await finalizeWizard(draftCharacterId)
      setActiveDraft(result)
      await refreshCharacters()
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleSaveBuildToDb() {
    let targetCharacterId = currentCharacterId
    if (!targetCharacterId) {
      const draft = await ensureActiveDraft()
      targetCharacterId = draft?.draft?.characterId ?? ''
      if (!targetCharacterId) {
        setError('Select or create a character first.')
        return
      }
    }
    if (buildMethod === 'Roll' && !isRollAssignmentComplete) {
      setError('Assign all rolled values to abilities before saving.')
      return
    }
    const selectedClass = classCatalog.find((x) => x.moduleId === effectiveSelectedClassModuleId)
    const selectedClassFromModules = classOptions.find((x) => x.moduleId === effectiveSelectedClassModuleId)
    const primaryClassName = selectedClass?.className ?? selectedClassFromModules?.displayName
    if (!primaryClassName) {
      setError('Select a class before saving the build.')
      return
    }
    if (totalCharacterLevel > 20) {
      setError('Total character level across all classes must be 20 or lower.')
      return
    }

    try {
      const selectedClassModule = classOptions.find((x) => x.moduleId === effectiveSelectedClassModuleId)
      const secondarySummary = multiClassSelections
        .map((entry) => {
          const option = classOptions.find((x) => x.moduleId === entry.moduleId)
          return option ? `${option.displayName} ${entry.level}` : `${entry.moduleId} ${entry.level}`
        })
        .join(', ')
      const classLevels = [
        {
          classModuleId: effectiveSelectedClassModuleId,
          className: primaryClassName,
          level: primaryClassLevel,
          sortOrder: 0,
        },
        ...multiClassSelections.map((entry, index) => {
          const option = classOptions.find((x) => x.moduleId === entry.moduleId)
          return {
            classModuleId: entry.moduleId,
            className: option?.displayName ?? entry.moduleId,
            level: entry.level,
            sortOrder: index + 1,
          }
        }),
      ]
      const selectedModules = [
        selectedClassModule
          ? {
              slot: 'class',
              moduleId: selectedClassModule.moduleId,
              displayName: selectedClassModule.displayName,
              sourceCode: selectedClassModule.sourceCode,
            }
          : null,
        ...(effectiveSelectedRaceModuleId
          ? [
              (() => {
                const race = raceOptions.find((x) => x.moduleId === effectiveSelectedRaceModuleId)
                return race
                  ? {
                      slot: race.moduleType.toLowerCase() === 'species' ? 'species' : 'race',
                      moduleId: race.moduleId,
                      displayName: race.displayName,
                      sourceCode: race.sourceCode,
                    }
                  : null
              })(),
            ]
          : []),
        ...(effectiveSelectedBackgroundModuleId
          ? [
              (() => {
                const background = backgroundOptions.find((x) => x.moduleId === effectiveSelectedBackgroundModuleId)
                return background
                  ? {
                      slot: background.moduleType.toLowerCase() === 'origin' ? 'origin' : 'background',
                      moduleId: background.moduleId,
                      displayName: background.displayName,
                      sourceCode: background.sourceCode,
                    }
                  : null
              })(),
            ]
          : []),
        ...(effectiveSelectedSubraceModuleId
          ? [
              (() => {
                const subrace = subraceOptions.find((x) => x.moduleId === effectiveSelectedSubraceModuleId)
                return subrace
                  ? {
                      slot: 'subrace',
                      moduleId: subrace.moduleId,
                      displayName: subrace.displayName,
                      sourceCode: subrace.sourceCode,
                    }
                  : null
              })(),
            ]
          : []),
        ...(effectiveSelectedSubclassModuleId
          ? [
              (() => {
                const subclass = primaryClassSubclassOptions.find((x) => x.moduleId === effectiveSelectedSubclassModuleId)
                return subclass
                  ? {
                      slot: 'subclass',
                      moduleId: subclass.moduleId,
                      displayName: subclass.displayName,
                      sourceCode: subclass.sourceCode,
                    }
                  : null
              })(),
            ]
          : []),
        ...multiClassSelections.map((entry) => {
          const subclass = entry.subclassModuleId
            ? getSubclassChoicesForClass(entry.moduleId).find((x) => x.moduleId === entry.subclassModuleId)
            : null
          return subclass
            ? {
                slot: `subclass:${entry.moduleId}`,
                moduleId: subclass.moduleId,
                displayName: subclass.displayName,
                sourceCode: subclass.sourceCode,
              }
            : null
        }),
        ...selectedToolPicks.map((tool) => ({
          slot: 'tool-proficiency',
          moduleId: tool,
          displayName: tool,
          sourceCode: selectedBackgroundOption?.sourceCode ?? selectedClassOption?.sourceCode ?? selectedRaceOption?.sourceCode ?? '',
        })),
        ...selectedLanguagePicks.map((language) => ({
          slot: 'language',
          moduleId: language,
          displayName: language,
          sourceCode: selectedBackgroundOption?.sourceCode ?? selectedClassOption?.sourceCode ?? selectedRaceOption?.sourceCode ?? '',
        })),
        {
          slot: 'starting-equipment-mode',
          moduleId: startingEquipmentMode,
          displayName: startingEquipmentMode === 'gold-only' ? 'Starting gold' : 'Equipment package',
          sourceCode: selectedClassOption?.sourceCode ?? '',
        },
        {
          slot: 'ac-mode',
          moduleId: acMode,
          displayName: acMode === 'manual' ? 'Manual armor class' : 'Calculated armor class',
          sourceCode: 'user',
        },
        ...(characterNotes.trim().length > 0
          ? [
              {
                slot: 'character-notes',
                moduleId: 'notes',
                displayName: characterNotes.trim(),
                sourceCode: 'user',
              },
            ]
          : []),
      ].filter((x): x is { slot: string; moduleId: string; displayName: string; sourceCode: string } => x !== null)

      const saved = await upsertCharacterBuild(targetCharacterId, {
        characterName: wizardName,
        baseRuleSystem: baseRules,
        buildMethod,
        classModuleId: effectiveSelectedClassModuleId,
        className: secondarySummary ? `${primaryClassName} (Primary); ${secondarySummary}` : primaryClassName,
        level: totalCharacterLevel,
        proficiencyBonus,
        abilityScores: totalAbilityScores,
        proficientSkills: ALL_SKILLS.filter((skill) => effectiveSkillTrainingBySkill[skill] !== 'None'),
        skillTrainingBySkill: effectiveSkillTrainingBySkill,
        classLevels,
        selectedModules,
      })
      setSavedBuild(saved)
      setBuildResult(JSON.stringify(saved, null, 2))
      const inventory = await getCharacterInventory(targetCharacterId).catch(() => null)
      if (inventory) {
        setInventoryState(inventory)
      }
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleArchive(characterId: string) {
    await archiveCharacter(characterId)
    await refreshCharacters()
    await refreshArchivedCharacters()
  }

  async function handleRestore(characterId: string) {
    await restoreCharacter(characterId)
    await refreshCharacters()
    await refreshArchivedCharacters()
  }

  async function handleDelete(characterId: string) {
    const confirmed = window.confirm('Permanently delete this character? This cannot be undone.')
    if (!confirmed) {
      return
    }

    await deleteCharacter(characterId)
    if (selectedCharacterId === characterId) {
      setSelectedCharacterId('')
      setHistory([])
      setSavedBuild(null)
      setInventoryState(null)
      setActiveDraft(null)
      setSpellEntries([])
      setResourcePools([])
      setVitals({ ...DEFAULT_VITALS })
      setSheetResult('')
    }
    await refreshCharacters()
    await refreshArchivedCharacters()
  }

  async function handleDuplicate(characterId: string) {
    await duplicateCharacter(characterId)
    await refreshCharacters()
  }

  async function handleLoadHistory(characterId: string) {
    const entries = await getCharacterHistory(characterId)
    setHistory(entries)
  }

  async function handleCopyRuleset() {
    if (!selectedCharacterId) return
    await copyToRuleset(selectedCharacterId, baseRules === 'Rules2024' ? 'Rules2014' : 'Rules2024')
    await refreshCharacters()
  }

  function addMultiClassSelection() {
    if (!secondaryClassModuleId) return
    const secondaryClassName = classNameByModuleId.get(secondaryClassModuleId.toLowerCase())?.toLowerCase() ?? ''
    const primaryClassName = classNameByModuleId.get(effectiveSelectedClassModuleId.toLowerCase())?.toLowerCase() ?? ''
    if (secondaryClassModuleId === effectiveSelectedClassModuleId) return
    if (secondaryClassName && primaryClassName && secondaryClassName === primaryClassName) return
    if (multiClassSelections.some((x) => x.moduleId === secondaryClassModuleId)) return
    if (
      secondaryClassName &&
      multiClassSelections.some((entry) => {
        const entryClassName = classNameByModuleId.get(entry.moduleId.toLowerCase())?.toLowerCase() ?? ''
        return entryClassName.length > 0 && entryClassName === secondaryClassName
      })
    ) {
      return
    }
    setMultiClassSelections((prev) => [...prev, { moduleId: secondaryClassModuleId, level: 1, subclassModuleId: '' }])
  }

  function getSubclassChoicesForClass(classModuleId: string) {
    const normalizedClassId = classModuleId.toLowerCase()
    const selectedClassName = classNameByModuleId.get(normalizedClassId)?.toLowerCase() ?? ''
    return subclassOptions.filter((item) => {
      if (!item.parentClassModuleId) {
        return selectedClassName.length > 0 && item.displayName.toLowerCase().includes(selectedClassName)
      }
      if (item.parentClassModuleId.toLowerCase() === normalizedClassId) {
        return true
      }
      const parentClassName = classNameByModuleId.get(item.parentClassModuleId.toLowerCase())?.toLowerCase() ?? ''
      if (selectedClassName && parentClassName && selectedClassName === parentClassName) {
        return true
      }
      return selectedClassName.length > 0 && item.displayName.toLowerCase().includes(selectedClassName)
    })
  }

  function getUnlockedSubclassChoicesForClass(classModuleId: string, classLevel: number) {
    return getSubclassChoicesForClass(classModuleId).filter(
      (item) => moduleCompatibilityIssues(item, classLevel).length === 0,
    )
  }

  function removeMultiClassSelection(moduleId: string) {
    setMultiClassSelections((prev) => prev.filter((x) => x.moduleId !== moduleId))
  }

  function setMultiClassLevel(moduleId: string, level: number) {
    const bounded = Math.max(1, Math.min(20, level))
    setMultiClassSelections((prev) =>
      prev.map((x) => {
        if (x.moduleId !== moduleId) {
          return x
        }
        const unlocked = getUnlockedSubclassChoicesForClass(moduleId, bounded)
        return {
          ...x,
          level: bounded,
          subclassModuleId: x.subclassModuleId && unlocked.some((item) => item.moduleId === x.subclassModuleId) ? x.subclassModuleId : '',
        }
      }),
    )
  }

  function setMultiClassSubclass(moduleId: string, subclassModuleId: string) {
    const unlocked = getUnlockedSubclassChoicesForClass(
      moduleId,
      multiClassSelections.find((x) => x.moduleId === moduleId)?.level ?? 1,
    )
    if (subclassModuleId && !unlocked.some((item) => item.moduleId === subclassModuleId)) {
      return
    }
    setMultiClassSelections((prev) =>
      prev.map((x) => (x.moduleId === moduleId ? { ...x, subclassModuleId } : x)),
    )
  }

  function moveNewCharacterStep(delta: number) {
    setNewCharacterStep((prev) => Math.max(1, Math.min(4, prev + delta)))
  }

  async function handleViewCharacter(characterId: string) {
    await handleSelectCharacter(characterId)
    navigate(`/characters/view/${characterId}`)
  }

  function buildNotesUpsertPayload(build: CharacterBuildData, notes: string): UpsertCharacterBuildPayload {
    const noteText = notes.trim()
    const withoutNotes = build.selectedModules.filter((module) => module.slot.toLowerCase() !== 'character-notes')
    const selectedModules = noteText.length > 0
      ? [
          ...withoutNotes,
          {
            slot: 'character-notes',
            moduleId: 'notes',
            displayName: noteText,
            sourceCode: 'user',
          },
        ]
      : withoutNotes

    return {
      characterName: build.characterName,
      baseRuleSystem: build.baseRuleSystem,
      buildMethod: build.buildMethod,
      classModuleId: build.classModuleId,
      className: build.className,
      level: build.level,
      proficiencyBonus: build.proficiencyBonus,
      abilityScores: build.abilityScores,
      proficientSkills: build.proficientSkills,
      skillTrainingBySkill: build.skillTrainingBySkill,
      classLevels: build.classLevels,
      selectedModules,
    }
  }

  async function handleSaveCharacterNotes() {
    if (!currentCharacterId || !savedBuild) {
      return
    }

    try {
      const saved = await upsertCharacterBuild(currentCharacterId, buildNotesUpsertPayload(savedBuild, characterNotes))
      setSavedBuild(saved)
      setSheetResult('Saved notes.')
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleSaveViewEdits() {
    if (!currentCharacterId || !savedBuild || isViewingArchivedCharacter) {
      return
    }

    try {
      const withoutNotesOrAcMode = savedBuild.selectedModules.filter((module) => {
        const slot = module.slot.toLowerCase()
        return slot !== 'character-notes' && slot !== 'ac-mode'
      })
      const nextSelectedModules = [
        ...withoutNotesOrAcMode,
        {
          slot: 'ac-mode',
          moduleId: acMode,
          displayName: acMode === 'manual' ? 'Manual armor class' : 'Calculated armor class',
          sourceCode: 'user',
        },
        ...(characterNotes.trim().length > 0
          ? [
              {
                slot: 'character-notes',
                moduleId: 'notes',
                displayName: characterNotes.trim(),
                sourceCode: 'user',
              },
            ]
          : []),
      ]
      const nextBuild = await upsertCharacterBuild(currentCharacterId, {
        characterName: savedBuild.characterName,
        baseRuleSystem: savedBuild.baseRuleSystem,
        buildMethod: savedBuild.buildMethod,
        classModuleId: savedBuild.classModuleId,
        className: savedBuild.className,
        level: savedBuild.level,
        proficiencyBonus: savedBuild.proficiencyBonus,
        abilityScores: totalAbilityScores,
        proficientSkills: ALL_SKILLS.filter((skill) => effectiveSkillTrainingBySkill[skill] !== 'None'),
        skillTrainingBySkill: effectiveSkillTrainingBySkill,
        classLevels: savedBuild.classLevels,
        selectedModules: nextSelectedModules,
      })
      const nextVitals = await upsertCharacterVitals(currentCharacterId, vitals)
      const nextResources = await upsertCharacterResources(currentCharacterId, resourcePools)
      setSavedBuild(nextBuild)
      setVitals({
        maxHitPoints: nextVitals.maxHitPoints,
        currentHitPoints: nextVitals.currentHitPoints,
        tempHitPoints: nextVitals.tempHitPoints,
        baseMoveSpeed: nextVitals.baseMoveSpeed,
        baseArmorClass: nextVitals.baseArmorClass,
      })
      setResourcePools(nextResources.resources)
      setSheetResult('Saved character edits.')
      setViewEditMode(false)
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleResetViewDefaults() {
    const defaultScores = defaultBuildSnapshot?.abilityScores ?? { ...DEFAULT_SCORES }
    setManualScores(defaultScores)
    setPointBuyScores(
      ABILITIES.reduce(
        (acc, ability) => ({ ...acc, [ability]: Math.max(8, Math.min(15, defaultScores[ability])) }),
        { ...DEFAULT_SCORES },
      ),
    )
    setRollAssignments(defaultScores)
    setRolledPool([])
    setSkillTrainingBySkill(
      (defaultBuildSnapshot?.skillTrainingBySkill as Record<SkillName, SkillTrainingLevel>) ??
      (Object.fromEntries(ALL_SKILLS.map((skill) => [skill, 'None'])) as Record<SkillName, SkillTrainingLevel>),
    )
    setVitals({ ...defaultVitalsSnapshot })
    setResourcePools(defaultResourcesSnapshot.map((resource) => ({ ...resource })))
    setAcMode('calculated')
    setSheetResult('Reset editable fields to creation defaults. Save to persist.')
  }

  async function handleSubmitCharacter() {
    let targetCharacterId = currentCharacterId
    if (!targetCharacterId) {
      const draft = await ensureActiveDraft()
      targetCharacterId = draft?.draft?.characterId ?? ''
    }
    if (!targetCharacterId) {
      setError('Unable to create character draft.')
      return
    }

    await handleSaveBuildToDb()
    await handleApplySelectedClassToWizard()
    await handleFinalizeWizard()
    await upsertCharacterVitals(targetCharacterId, {
      maxHitPoints: vitals.maxHitPoints,
      currentHitPoints: Math.max(vitals.currentHitPoints, vitals.maxHitPoints),
      tempHitPoints: vitals.tempHitPoints,
      baseMoveSpeed: vitals.baseMoveSpeed,
      baseArmorClass: vitals.baseArmorClass,
    })
    await upsertCharacterSpells(targetCharacterId, spellEntries)
    const hasSpellSlotResources = resourcePools.some((resource) =>
      resource.resourceKey.toLowerCase().includes('slot'),
    )
    const autoSpellSlotResources = hasSpellSlotResources
      ? []
      : getDefaultSpellSlotResources(selectedPrimaryClassName, primaryClassLevel)
    const resourcesToSave =
      autoSpellSlotResources.length > 0
        ? [...resourcePools, ...autoSpellSlotResources]
        : resourcePools
    await upsertCharacterResources(targetCharacterId, resourcesToSave)
    await upsertCharacterCurrency(targetCharacterId, currencyDraft)
    await refreshCharacters()
    resetCharacterCreationState()
    if (session) {
      await loadCatalogData('Rules2024')
    }
    navigate('/characters')
  }

  async function handleDiscardNewCharacter() {
    const confirmed = window.confirm('Discard this character draft and exit character creation?')
    if (!confirmed) {
      return
    }

    try {
      if (currentCharacterId) {
        await deleteCharacter(currentCharacterId)
      }
      setActiveDraft(null)
      setSelectedCharacterId('')
      setSelectedSubclassModuleId('')
      setSelectedSubraceModuleId('')
      setMultiClassSelections([])
      setSelectedToolPicks([])
      setSelectedLanguagePicks([])
      setSpellEntries([])
      setResourcePools([])
      setInventoryState(null)
      setCurrencyState(null)
      setNewCharacterStep(1)
      resetCharacterCreationState()
      if (session) {
        await loadCatalogData('Rules2024')
      }
      navigate('/characters')
      await refreshCharacters()
    } catch (e) {
      setError(String(e))
    }
  }

  function setSkillTraining(skill: SkillName, level: SkillTrainingLevel) {
    const isAutoGranted = autoGrantedSkills.some((x) => x.toLowerCase() === skill.toLowerCase())
    const isSelectableChoice = selectableSkillChoices.some((x) => x.toLowerCase() === skill.toLowerCase())
    const currentLevel = effectiveSkillTrainingBySkill[skill]
    const isCurrentlyPicked = currentLevel !== 'None' && !isAutoGranted

    if (level === 'None' && isAutoGranted) {
      setSkillTrainingBySkill((prev) => ({ ...prev, [skill]: 'Proficient' }))
      return
    }

    if (level !== 'None' && !isAutoGranted && !isSelectableChoice) {
      return
    }

    if (level !== 'None' && !isAutoGranted && !isCurrentlyPicked && nonAutoSkillPicksUsed >= skillChoiceCount) {
      return
    }

    const nextExpertiseUsed =
      expertiseSlotsUsed -
      (currentLevel === 'Expertise' ? 1 : 0) +
      (level === 'Expertise' ? 1 : 0)
    if (level === 'Expertise' && nextExpertiseUsed > expertiseSlotsAvailable) {
      return
    }

    setSkillTrainingBySkill((prev) => ({ ...prev, [skill]: level }))
  }

  function setSkillTrainingInView(skill: SkillName, level: SkillTrainingLevel) {
    setSkillTrainingBySkill((prev) => ({ ...prev, [skill]: level }))
  }

  function toggleToolPick(toolName: string) {
    const normalized = toolName.trim()
    if (!normalized) return
    if (!selectableTools.some((x) => x.toLowerCase() === normalized.toLowerCase())) {
      return
    }

    setSelectedToolPicks((prev) => {
      const exists = prev.some((x) => x.toLowerCase() === normalized.toLowerCase())
      if (exists) {
        return prev.filter((x) => x.toLowerCase() !== normalized.toLowerCase())
      }
      if (prev.length >= toolChoiceCount) {
        return prev
      }
      return [...prev, normalized]
    })
  }

  function toggleLanguagePick(languageName: string) {
    const normalized = languageName.trim()
    if (!normalized) return
    if (!selectableLanguages.some((x) => x.toLowerCase() === normalized.toLowerCase())) {
      return
    }

    setSelectedLanguagePicks((prev) => {
      const exists = prev.some((x) => x.toLowerCase() === normalized.toLowerCase())
      if (exists) {
        return prev.filter((x) => x.toLowerCase() !== normalized.toLowerCase())
      }
      if (prev.length >= languageChoiceCount) {
        return prev
      }
      return [...prev, normalized]
    })
  }

  function setManualAbilityScore(ability: AbilityName, score: number) {
    if (score < -10000 || score > 10000) {
      alert('Ambitions... but no.')
      return
    }
    const bounded = Math.max(1, Math.min(30, score))
    setManualScores((prev) => ({ ...prev, [ability]: bounded }))
  }

  function setPointBuyAbilityScore(ability: AbilityName, score: number) {
    const bounded = Math.max(8, Math.min(15, score))
    const candidate = { ...pointBuyScores, [ability]: bounded }
    const spent = ABILITIES.reduce((sum, key) => sum + POINT_BUY_COST[candidate[key]], 0)
    if (spent > 27) return
    setPointBuyScores(candidate)
  }

  function toNonNegativeInt(value: string, fallback = 0) {
    const parsed = Number(value)
    if (!Number.isFinite(parsed)) {
      return fallback
    }

    return Math.max(0, Math.trunc(parsed))
  }

  function handleRollPoolGenerate() {
    setRolledPool(rollAbilityValues(rerollOnes))
    setRollAssignments({})
  }

  function unassignRoll(ability: AbilityName) {
    const assigned = rollAssignments[ability]
    if (typeof assigned !== 'number') return
    setRollAssignments((prev) => {
      const next = { ...prev }
      delete next[ability]
      return next
    })
    setRolledPool((prev) => [...prev, assigned].sort((a, b) => b - a))
  }

  function assignRollToAbility(ability: AbilityName, value: number) {
    const previousAssigned = rollAssignments[ability]
    setRollAssignments((prev) => {
      const next = { ...prev, [ability]: value }
      return next
    })
    setRolledPool((prev) => {
      const index = prev.indexOf(value)
      if (index < 0) return prev
      const next = [...prev]
      next.splice(index, 1)
      if (typeof previousAssigned === 'number') {
        next.push(previousAssigned)
      }
      return next.sort((a, b) => b - a)
    })
  }

  async function handleAddItemFromCatalog() {
    if (!currentCharacterId || !effectiveSelectedCatalogItemId) return
    try {
      if (purchaseFromCurrencyMode) {
        const item = filteredItemCatalog.find((x) => x.itemId === effectiveSelectedCatalogItemId)
        const costInGold = Number(item?.goldValue ?? 0)
        if (costInGold > 0) {
          const currency = await purchaseFromCharacterCurrency(currentCharacterId, {
            costInGold,
            quantity: selectedCatalogQuantity,
          })
          setCurrencyState(currency)
          setCurrencyDraft({ cp: currency.cp, sp: currency.sp, ep: currency.ep, gp: currency.gp, pp: currency.pp })
        }
      }
      const next = await addInventoryItem(currentCharacterId, effectiveSelectedCatalogItemId, selectedCatalogQuantity)
      setInventoryState(next)
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleSaveCurrency() {
    if (!currentCharacterId) return
    try {
      const next = await upsertCharacterCurrency(currentCharacterId, currencyDraft)
      setCurrencyState(next)
      setCurrencyDraft({ cp: next.cp, sp: next.sp, ep: next.ep, gp: next.gp, pp: next.pp })
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleConvertCurrency() {
    if (!currentCharacterId) return
    try {
      const next = await convertCharacterCurrency(currentCharacterId, currencyConvert)
      setCurrencyState(next)
      setCurrencyDraft({ cp: next.cp, sp: next.sp, ep: next.ep, gp: next.gp, pp: next.pp })
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleConsolidateCurrency() {
    if (!currentCharacterId) return
    try {
      const next = await consolidateCharacterCurrency(currentCharacterId, usePlatinumConsolidation)
      setCurrencyState(next)
      setCurrencyDraft({ cp: next.cp, sp: next.sp, ep: next.ep, gp: next.gp, pp: next.pp })
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleUpdateInventoryItem(
    inventoryItemId: string,
    update: { isEquipped?: boolean; isAttuned?: boolean; quantity?: number },
  ) {
    if (!currentCharacterId) return
    try {
      const next = await patchInventoryItem(currentCharacterId, inventoryItemId, update)
      setInventoryState(next)
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleRemoveItem(inventoryItemId: string) {
    if (!currentCharacterId) return
    try {
      await removeInventoryItem(currentCharacterId, inventoryItemId)
      const next = await getCharacterInventory(currentCharacterId)
      setInventoryState(next)
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleSaveVitals() {
    if (!currentCharacterId) return
    try {
      const saved = await upsertCharacterVitals(currentCharacterId, vitals)
      setVitals({
        maxHitPoints: saved.maxHitPoints,
        currentHitPoints: saved.currentHitPoints,
        tempHitPoints: saved.tempHitPoints,
        baseMoveSpeed: saved.baseMoveSpeed,
        baseArmorClass: saved.baseArmorClass,
      })
      setSheetResult('Saved vitals.')
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleSaveSpells() {
    if (!currentCharacterId) return
    try {
      const saved = await upsertCharacterSpells(currentCharacterId, spellEntries)
      setSpellEntries(saved.entries)
      setSheetResult('Saved spells.')
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleSaveResources() {
    if (!currentCharacterId) return
    try {
      const saved = await upsertCharacterResources(currentCharacterId, resourcePools)
      setResourcePools(saved.resources)
      setSheetResult('Saved resources.')
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleLoadRecommendedSpells(classModuleId: string, className: string, classLevel: number) {
    if (!currentCharacterId) return
    try {
      const result = await getRecommendedSpells(currentCharacterId, classModuleId, classLevel)
      const lines = [
        `${className} level ${classLevel}: ${result.advisoryMessage}`,
        result.dataGap ?? '',
        result.recommendedSpells.length > 0
          ? `Recommendations: ${result.recommendedSpells.map((x) => x.spellName).join(', ')}`
          : 'Recommendations: none available from curated source.',
      ].filter((x) => x.trim().length > 0)
      setRecommendedSpellsByClass((prev) => ({ ...prev, [classModuleId]: lines.join('\n') }))
    } catch (e) {
      setError(String(e))
    }
  }

  function skillModifier(skill: SkillName) {
    const ability = SKILL_ABILITY[skill]
    const abilityMod = abilityModifier(totalAbilityScores[ability])
    const training = effectiveSkillTrainingBySkill[skill]
    if (training === 'Expertise') {
      return abilityMod + proficiencyBonus * 2
    }
    if (training === 'Proficient') {
      return abilityMod + proficiencyBonus
    }
    return abilityMod
  }

  function moduleCompatibilityIssues(module: ModuleCatalogItem, levelForCheck: number) {
    const issues: string[] = []
    if (module.minLevelRequirement > 0 && levelForCheck < module.minLevelRequirement) {
      issues.push(`requires level ${module.minLevelRequirement}+`)
    }

    for (const [ability, required] of Object.entries(module.abilityScoreRequirements ?? {})) {
      const key = ability as AbilityName
      const actual = totalAbilityScores[key] ?? 0
      if (actual < required) {
        issues.push(`requires ${ability} ${required}+`)
      }
    }

    return issues
  }

  const viewedCharacter = [...characters, ...archivedCharacters].find(
    (entry) => entry.characterId === (viewCharacterIdFromPath || selectedCharacterId),
  )
  const isViewingArchivedCharacter = viewedCharacter?.isArchived ?? false
  const saveModifiers = ABILITIES.map((ability) => {
    const baseMod = abilityModifier(totalAbilityScores[ability])
    const isProficient = savedBuild?.saveProficiencies.includes(ability) ?? false
    return { ability, value: baseMod + (isProficient ? proficiencyBonus : 0), isProficient }
  })
  const sortedSkillsForView = [...ALL_SKILLS].sort((a, b) => {
    if (viewSkillSort === 'name') {
      return a.localeCompare(b)
    }
    const abilityDiff = SKILL_ABILITY[a].localeCompare(SKILL_ABILITY[b])
    if (abilityDiff !== 0) {
      return abilityDiff
    }
    const skillDiff = skillModifier(b) - skillModifier(a)
    return skillDiff !== 0 ? skillDiff : a.localeCompare(b)
  })
  const viewSkillGroups = viewSkillSort === 'ability'
    ? ABILITIES.map((ability) => ({
        ability,
        skills: sortedSkillsForView.filter((skill) => SKILL_ABILITY[skill] === ability),
      })).filter((group) => group.skills.length > 0)
    : [{ ability: null, skills: sortedSkillsForView }]
  const featSelections = (savedBuild?.selectedModules ?? []).filter((module) => module.slot.toLowerCase() === 'feat')
  const equippedItems = (inventoryState?.items ?? []).filter((item) => item.isEquipped)
  const unequippedItems = (inventoryState?.items ?? []).filter((item) => !item.isEquipped)
  const weaponItems = (inventoryState?.items ?? []).filter((item) => item.isWeapon)
  const normalizedClassName = selectedPrimaryClassName.trim().toLowerCase()
  const casterAbility = CLASS_SPELLCASTING_ABILITY[normalizedClassName]
  const spellAttackBonus = casterAbility ? abilityModifier(totalAbilityScores[casterAbility]) + proficiencyBonus : null
  const spellSaveDc = casterAbility ? 8 + proficiencyBonus + abilityModifier(totalAbilityScores[casterAbility]) : null
  const calculatedAc = Math.max(
    10 + abilityModifier(totalAbilityScores.Dexterity),
    inventoryState?.pipelineResult?.derivedStats?.armorClass ?? 10 + abilityModifier(totalAbilityScores.Dexterity),
  )
  const displayedAc = acMode === 'manual' ? vitals.baseArmorClass : calculatedAc
  const totalSpellSlots = resourcePools
    .filter((resource) => resource.resourceKey.toLowerCase().includes('slot'))
    .reduce((sum, resource) => sum + Math.max(0, resource.maxValue), 0)

  return (
    <main className="layout">
      {!isNewCharacterRoute && (
        <header>
          <h1>Welcome to DndAppName</h1>
          <p>{status}</p>
        </header>
      )}

      {error && <p className="error">{error}</p>}

      {!session ? (
        <>
          <section className="card">
            <h2>Login</h2>
            <div className="row">
              <label htmlFor="login-username">Username</label>
              <input id="login-username" value={loginName} onChange={(e) => setLoginName(e.target.value)} placeholder="Enter username" />
              <label htmlFor="login-password">Password</label>
              <input id="login-password" type="password" value={loginPassword} onChange={(e) => setLoginPassword(e.target.value)} placeholder="Enter password" />
              <label>
                <input type="checkbox" checked={isRegisterMode} onChange={(e) => setIsRegisterMode(e.target.checked)} /> Register
                new user
              </label>
              <button onClick={handleLogin}>{isRegisterMode ? 'Register + Start Session' : 'Start Session'}</button>
            </div>
          </section>
          <section className="card">
            <h2>About DndAppName</h2>
            <p>
              Build and manage D&D characters across 2014 and 2024 rules, mix content sources when allowed, run
              persisted skill checks, and manage attunement-aware inventory from the shared rules database.
            </p>
          </section>
        </>
      ) : (
        <>
          {!isCharacterViewRoute && (
          <section className="card">
            <h2>Session</h2>
            <div className="row">
              <p>
                Signed in as <strong>{session.userName}</strong>
              </p>
              <button onClick={handleLogout}>Log out</button>
            </div>
            <div className="row">
              <button onClick={() => navigate('/characters')}>Your characters</button>
              <button onClick={() => navigate('/characters/archived')}>Archived characters</button>
              <button
                onClick={startNewCharacterFlow}
              >
                Create new character
              </button>
              <button onClick={() => navigate('/settings')}>Settings</button>
            </div>
            <p>
              <strong>Getting started:</strong> pick a character (or create a new one), then complete build setup and
              use the sheet sections for vitals, spells, resources, skills, and inventory.
            </p>
          </section>
          )}

          {isCharactersRoute && (
            <CharactersPage
              sessionReady={Boolean(session)}
              selectedCharacterId={selectedCharacterId}
              characters={characters}
              classSummaryByCharacterId={classSummaryByCharacterId}
              history={history}
              rulesetLabel={rulesetLabel}
              onRefresh={() => void refreshCharacters()}
              onCreateNew={startNewCharacterFlow}
              onOpenArchived={() => navigate('/characters/archived')}
              onCopyRuleset={() => void handleCopyRuleset()}
              onSelectCharacter={(characterId) => void handleSelectCharacter(characterId)}
              onViewCharacter={(characterId) => void handleViewCharacter(characterId)}
              onArchiveCharacter={(characterId) => void handleArchive(characterId)}
              onDuplicateCharacter={(characterId) => void handleDuplicate(characterId)}
              onLoadHistory={(characterId) => void handleLoadHistory(characterId)}
            />
          )}

          {isArchivedRoute && (
            <ArchivedCharactersPage
              sessionReady={Boolean(session)}
              archivedCharacters={archivedCharacters}
              rulesetLabel={rulesetLabel}
              onRefreshArchived={() => void refreshArchivedCharacters()}
              onBackToCharacters={() => navigate('/characters')}
              onSelectCharacter={(characterId) => void handleSelectCharacter(characterId)}
              onViewCharacter={(characterId) => void handleViewCharacter(characterId)}
              onRestoreCharacter={(characterId) => void handleRestore(characterId)}
              onDeleteCharacter={(characterId) => void handleDelete(characterId)}
            />
          )}

          {isSettingsRoute && (
            <SettingsPage
              themeName={themeName}
              onBackToCharacters={() => navigate('/characters')}
              onThemeChange={setThemeName}
            />
          )}

          {isCharacterViewRoute && (
            <section className="card">
              <h2>Character view</h2>
              <div className="row">
                <button onClick={() => navigate('/characters')}>Back to characters</button>
                <button onClick={handleLogout}>Logout</button>
                <button
                  onClick={() => {
                    if (viewEditMode) {
                      void handleSaveViewEdits()
                      return
                    }
                    setViewEditMode(true)
                  }}
                  disabled={isViewingArchivedCharacter}
                >
                  {viewEditMode ? 'Done editing' : 'Edit'}
                </button>
              </div>
              {isViewingArchivedCharacter && <small>This character is archived. Restore it to enable editing.</small>}
              <p>
                <strong>{(viewedCharacter?.characterName ?? wizardName) || 'Character'}</strong> |{' '}
                {viewedCharacter ? rulesetLabel(viewedCharacter.baseRuleSystem) : rulesetLabel(baseRules)} |{' '}
                {mixedModeLabel(
                  viewedCharacter?.baseRuleSystem ?? baseRules,
                  viewedCharacter?.mixedModeEnabled ?? mixedMode,
                )}
              </p>
              <p>
                Classes: {classSections.map((entry) => `${entry.className} ${entry.level}`).join(', ') || 'None'} | Hit
                dice:{' '}
                {classSections.map((entry) => `${entry.className} d${parseHitDieSides(entry.className)} x${entry.level}`).join(', ') ||
                  'Not tracked'}
              </p>
              <div className="grid">
                <label>Max HP</label>
                {viewEditMode ? (
                  <input
                    type="number"
                    min={0}
                    value={vitals.maxHitPoints}
                    onChange={(e) => setVitals((prev) => ({ ...prev, maxHitPoints: toNonNegativeInt(e.target.value) }))}
                  />
                ) : (
                  <span>{vitals.maxHitPoints}</span>
                )}
                <label>Current HP</label>
                <input
                  type="number"
                  min={0}
                  value={vitals.currentHitPoints}
                  onChange={(e) => setVitals((prev) => ({ ...prev, currentHitPoints: toNonNegativeInt(e.target.value) }))}
                  onBlur={() => void handleSaveVitals()}
                />
                <label>Temp HP</label>
                <input
                  type="number"
                  min={0}
                  value={vitals.tempHitPoints}
                  onChange={(e) => setVitals((prev) => ({ ...prev, tempHitPoints: toNonNegativeInt(e.target.value) }))}
                  onBlur={() => void handleSaveVitals()}
                />
                <label>AC</label><span>{displayedAc}</span>
                <label>Speed</label>
                {viewEditMode ? (
                  <input
                    type="number"
                    min={0}
                    value={vitals.baseMoveSpeed}
                    onChange={(e) => setVitals((prev) => ({ ...prev, baseMoveSpeed: toNonNegativeInt(e.target.value) }))}
                  />
                ) : (
                  <span>{vitals.baseMoveSpeed}</span>
                )}
                <label>Initiative</label><span>{abilityModifier(totalAbilityScores.Dexterity) >= 0 ? '+' : ''}{abilityModifier(totalAbilityScores.Dexterity)}</span>
                <label>Passive Perception</label><span>{10 + skillModifier('Perception')}</span>
              </div>
              {viewEditMode && (
                <div className="grid">
                  <label htmlFor="ac-mode">Armor class mode</label>
                  <select id="ac-mode" value={acMode} onChange={(e) => setAcMode(e.target.value as 'manual' | 'calculated')}>
                    <option value="calculated">Calculated</option>
                    <option value="manual">Manual</option>
                  </select>
                  {acMode === 'manual' && (
                    <>
                      <label htmlFor="manual-ac">Manual AC</label>
                      <input
                        id="manual-ac"
                        type="number"
                        min={0}
                        value={vitals.baseArmorClass}
                        onChange={(e) => setVitals((prev) => ({ ...prev, baseArmorClass: toNonNegativeInt(e.target.value) }))}
                      />
                    </>
                  )}
                </div>
              )}
              {vitals.currentHitPoints <= 0 && (
                <div className="card">
                  <h3>Death saves</h3>
                  <p>Successes {deathSaveSuccesses}/3 | Failures {deathSaveFailures}/3</p>
                  <div className="row">
                    {[1, 2, 3].map((slot) => (
                      <button
                        key={`success-${slot}`}
                        onClick={() => setDeathSaveSuccesses((prev) => (prev >= slot ? slot - 1 : slot))}
                      >
                        Success {slot} {deathSaveSuccesses >= slot ? '✓' : ''}
                      </button>
                    ))}
                    {[1, 2, 3].map((slot) => (
                      <button
                        key={`failure-${slot}`}
                        onClick={() => setDeathSaveFailures((prev) => (prev >= slot ? slot - 1 : slot))}
                      >
                        Failure {slot} {deathSaveFailures >= slot ? '✗' : ''}
                      </button>
                    ))}
                    <button onClick={() => { setDeathSaveSuccesses(0); setDeathSaveFailures(0) }}>Reset</button>
                  </div>
                </div>
              )}
              <h3>Ability scores</h3>
              <div className="skills-grid">
                {ABILITIES.map((ability) => (
                  <label key={ability} className="row">
                    {ability}
                    {viewEditMode ? (
                      <input
                        type="number"
                        min={1}
                        max={30}
                        value={manualScores[ability]}
                        onChange={(e) => {
                          const value = Math.max(1, Math.min(30, Number(e.target.value) || 1))
                          setManualScores((prev) => ({ ...prev, [ability]: value }))
                          setPointBuyScores((prev) => ({ ...prev, [ability]: Math.max(8, Math.min(15, value)) }))
                          setRollAssignments((prev) => ({ ...prev, [ability]: value }))
                        }}
                      />
                    ) : (
                      <small>
                        {totalAbilityScores[ability]} ({abilityModifier(totalAbilityScores[ability]) >= 0 ? '+' : ''}{abilityModifier(totalAbilityScores[ability])})
                      </small>
                    )}
                  </label>
                ))}
              </div>
              <h3>Saving throws</h3>
              <div className="skills-grid">
                {saveModifiers.map((save) => (
                  <small key={save.ability}>{save.ability}: {save.value >= 0 ? '+' : ''}{save.value} {save.isProficient ? '(proficient)' : ''}</small>
                ))}
              </div>
              <h3>Skills</h3>
              <div className="row">
                <label htmlFor="view-skill-sort">Sort</label>
                <select id="view-skill-sort" value={viewSkillSort} onChange={(e) => setViewSkillSort(e.target.value as 'name' | 'ability')}>
                  <option value="ability">By ability</option>
                  <option value="name">By name</option>
                </select>
              </div>
              {viewSkillGroups.map((group) => (
                <div key={group.ability ?? 'alpha'}>
                  {group.ability && <h4>{group.ability}</h4>}
                  <div className="skills-grid">
                    {group.skills.map((skill) => (
                      <label key={skill} className="row">
                        <span>
                          {skill}: {skillModifier(skill) >= 0 ? '+' : ''}{skillModifier(skill)}
                        </span>
                        {viewEditMode ? (
                          <select
                            value={effectiveSkillTrainingBySkill[skill]}
                            onChange={(e) => setSkillTrainingInView(skill, e.target.value as SkillTrainingLevel)}
                          >
                            <option value="None">None</option>
                            <option value="Proficient">Proficient</option>
                            <option value="Expertise">Expertise</option>
                          </select>
                        ) : (
                          <small>({effectiveSkillTrainingBySkill[skill]})</small>
                        )}
                      </label>
                    ))}
                  </div>
                </div>
              ))}
              <h3>Feats</h3>
              <p>{featSelections.length > 0 ? featSelections.map((feat) => feat.displayName).join(', ') : 'None selected'}</p>
              <h3>Spells</h3>
              <p>
                Spellcasting ability: {casterAbility ?? 'Not available'} | Spell attack bonus:{' '}
                {spellAttackBonus === null ? 'N/A' : `${spellAttackBonus >= 0 ? '+' : ''}${spellAttackBonus}`} | Spell save
                DC: {spellSaveDc ?? 'N/A'} | Total slots: {totalSpellSlots}
              </p>
              {viewEditMode && (
                <div className="skills-grid">
                  {resourcePools
                    .filter((resource) => resource.resourceKey.toLowerCase().includes('slot'))
                    .map((resource, index) => (
                      <label key={`${resource.resourceKey}-${index}`} className="row">
                        {resource.resourceKey}
                        <input
                          type="number"
                          min={0}
                          value={resource.maxValue}
                          onChange={(e) =>
                            setResourcePools((prev) =>
                              prev.map((row, i) =>
                                i === index ? { ...row, maxValue: toNonNegativeInt(e.target.value) } : row,
                              ),
                            )
                          }
                        />
                      </label>
                    ))}
                </div>
              )}
              <ul className="inventory-list">
                {spellEntries.map((entry, index) => (
                  <li key={`${entry.spellModuleId}-${index}`}>{entry.spellName} ({entry.preparationMode})</li>
                ))}
              </ul>
              <h3>Inventory</h3>
              {currencyState && (
                <small>
                  Coin purse: {currencyState.pp} pp | {currencyState.gp} gp | {currencyState.sp} sp | {currencyState.cp} cp
                  {currencyState.ep > 0 ? ` | ${currencyState.ep} ep` : ''}
                </small>
              )}
              <div className="row">
                <label htmlFor="view-catalog-item">Manage items</label>
                <select id="view-catalog-item" value={effectiveSelectedCatalogItemId} onChange={(e) => setSelectedCatalogItemId(e.target.value)}>
                  {filteredItemCatalog.length === 0 ? (
                    <option value="">No item definitions found in DB</option>
                  ) : (
                    filteredItemCatalog.map((item) => (
                      <option key={item.itemId} value={item.itemId}>
                        {getDisplayItemName(item.itemName, item.itemId)} ({item.sourceCode}) {item.requiresAttunement ? '[attunement]' : ''}
                      </option>
                    ))
                  )}
                </select>
                <label htmlFor="view-catalog-item-quantity">Qty</label>
                <input
                  id="view-catalog-item-quantity"
                  type="number"
                  min={1}
                  value={selectedCatalogQuantity}
                  onChange={(e) => setSelectedCatalogQuantity(Math.max(1, Number(e.target.value) || 1))}
                />
                <label>
                  <input type="checkbox" checked={purchaseFromCurrencyMode} onChange={(e) => setPurchaseFromCurrencyMode(e.target.checked)} /> Purchase
                </label>
                <button onClick={() => void handleAddItemFromCatalog()} disabled={!effectiveSelectedCatalogItemId || !currentCharacterId || isViewingArchivedCharacter}>
                  Add item
                </button>
              </div>
              {effectiveSelectedCatalogItemId && (
                <small>
                  {(() => {
                    const selectedItem = filteredItemCatalog.find((item) => item.itemId === effectiveSelectedCatalogItemId)
                    if (!selectedItem) {
                      return 'Select an item to see details.'
                    }
                    return `${selectedItem.itemType} | ${selectedItem.rarity} | ${selectedItem.goldValue} gp | ${selectedItem.weight} lb${selectedItem.description ? ` | ${selectedItem.description}` : ''}`
                  })()}
                </small>
              )}
              <div className="row">
                <label htmlFor="view-currency-convert-from">Convert</label>
                <select id="view-currency-convert-from" value={currencyConvert.fromDenomination} onChange={(e) => setCurrencyConvert((prev) => ({ ...prev, fromDenomination: e.target.value }))}>
                  <option value="cp">cp</option>
                  <option value="sp">sp</option>
                  <option value="ep">ep</option>
                  <option value="gp">gp</option>
                  <option value="pp">pp</option>
                </select>
                <span>to</span>
                <select id="view-currency-convert-to" value={currencyConvert.toDenomination} onChange={(e) => setCurrencyConvert((prev) => ({ ...prev, toDenomination: e.target.value }))}>
                  <option value="cp">cp</option>
                  <option value="sp">sp</option>
                  <option value="ep">ep</option>
                  <option value="gp">gp</option>
                  <option value="pp">pp</option>
                </select>
                <input id="view-currency-convert-amount" type="number" min={1} value={currencyConvert.amount} onChange={(e) => setCurrencyConvert((prev) => ({ ...prev, amount: Math.max(1, Number(e.target.value) || 1) }))} />
                <button onClick={() => void handleConvertCurrency()} disabled={!currentCharacterId || isViewingArchivedCharacter}>Convert</button>
                <button onClick={() => void handleConsolidateCurrency()} disabled={!currentCharacterId || isViewingArchivedCharacter}>Consolidate pocket change</button>
              </div>
              <p>Equipped items: {equippedItems.length} | Unequipped items: {unequippedItems.length}</p>
              <ul className="inventory-list">
                {equippedItems.map((item) => (
                  <li key={item.inventoryItemId}>{getDisplayItemName(item.itemName, item.itemDefinitionId)} x{item.quantity} (equipped)</li>
                ))}
                {unequippedItems.map((item) => (
                  <li key={item.inventoryItemId}>{getDisplayItemName(item.itemName, item.itemDefinitionId)} x{item.quantity} (unequipped)</li>
                ))}
              </ul>
              <h4>Weapons</h4>
              <ul className="inventory-list">
                {weaponItems.map((item) => (
                  <li key={`weapon-${item.inventoryItemId}`}>
                    {getDisplayItemName(item.itemName, item.itemDefinitionId)} {item.damageDice} atk {item.attackBonus >= 0 ? '+' : ''}{item.attackBonus}
                  </li>
                ))}
              </ul>
              <h3>Notes</h3>
              <textarea value={characterNotes} onChange={(e) => setCharacterNotes(e.target.value)} rows={5} />
              <div className="row">
                <button onClick={() => void handleSaveCharacterNotes()} disabled={!currentCharacterId || !savedBuild}>
                  Save notes
                </button>
                {viewEditMode && (
                  <>
                    <button onClick={() => void handleSaveViewEdits()} disabled={!currentCharacterId || !savedBuild || isViewingArchivedCharacter}>
                      Save edits
                    </button>
                    <button onClick={() => void handleResetViewDefaults()}>
                      Reset to default
                    </button>
                  </>
                )}
              </div>
            </section>
          )}

          {isNewCharacterRoute && (
            <>
            <section className="card">
              <h2>Character creation flow</h2>
              <p>
                Page {newCharacterStep} of 4
              </p>
              <div className="row">
                <button type="button" onClick={() => moveNewCharacterStep(-1)} disabled={newCharacterStep <= 1}>
                  Back
                </button>
                {newCharacterStep < 4 ? (
                  <button type="button" onClick={() => moveNewCharacterStep(1)}>
                    Next
                  </button>
                ) : (
                  <button type="button" onClick={() => void handleSubmitCharacter()} disabled={!session}>
                    Submit character
                  </button>
                )}
                <button type="button" onClick={() => void handleDiscardNewCharacter()}>
                  Exit + discard
                </button>
              </div>
            </section>
            <section className="card" hidden={newCharacterStep !== 1}>
              <h2>1. Ruleset selection</h2>
              <div className="grid">
                <label htmlFor="base-rules">Base ruleset</label>
                <select id="base-rules" value={baseRules} onChange={(e) => void handleBaseRulesChange(e.target.value as RuleSystemMode)}>
                  <option value="Rules2024">2024 rules</option>
                  <option value="Rules2014">2014 rules</option>
                </select>
                <label>
                  <input
                    type="checkbox"
                    checked={mixedMode}
                    onChange={(e) => {
                      const nextMixedMode = e.target.checked
                      setMixedMode(nextMixedMode)
                      if (session) {
                        void loadCatalogData(baseRules).catch((err) => setError(String(err)))
                      }
                    }}
                  />{' '}
                  {baseRules === 'Rules2024' ? 'Plus 2014 content' : 'Plus 2024 content'}
                </label>
              </div>
            </section>
            <section className="card" hidden={newCharacterStep !== 2}>
        <h2>2. Character build setup</h2>
        <div className="grid">
          <label htmlFor="character-name">Character name</label>
          <input id="character-name" value={wizardName} onChange={(e) => setWizardName(e.target.value)} placeholder="Character name" />
          <label htmlFor="build-method">Ability score method</label>
          <select id="build-method" value={buildMethod} onChange={(e) => setBuildMethod(e.target.value as BuildMethod)}>
            <option value="PointBuy">Point buy</option>
            <option value="Manual">Manual entry</option>
            <option value="Roll">Roll</option>
          </select>
          <label htmlFor="starting-equipment-mode">Starting gear mode</label>
          <select
            id="starting-equipment-mode"
            value={startingEquipmentMode}
            onChange={(e) => {
              const next = e.target.value as 'package' | 'gold-only'
              setStartingEquipmentMode(next)
            }}
          >
            <option value="package">Equipment package</option>
            <option value="gold-only">Starting gold</option>
          </select>
        </div>
        <div className="grid" hidden={newCharacterStep !== 2}>
          <label htmlFor="main-class-module-setup">Primary class</label>
          <select id="main-class-module-setup" value={effectiveSelectedClassModuleId} onChange={(e) => handlePrimaryClassChange(e.target.value)}>
            {mainClassOptions.length === 0 ? (
              <option value="">No class modules found in DB</option>
            ) : (
              mainClassOptions.map((item) => (
                <option key={item.moduleId} value={item.moduleId}>
                  {item.className} ({item.sourceCode})
                </option>
              ))
            )}
          </select>
          <label htmlFor="primary-class-level-setup">Primary class level</label>
          <input
            id="primary-class-level-setup"
            type="number"
            min={1}
            max={20}
            value={primaryClassLevel}
            onChange={(e) => setPrimaryClassLevel(Math.max(1, Math.min(20, Number(e.target.value))))}
            placeholder="Primary class level"
          />
          <label htmlFor="race-module">Race / species</label>
          <select id="race-module" value={effectiveSelectedRaceModuleId} onChange={(e) => handleRaceModuleChange(e.target.value)}>
            {raceOptions.length === 0 ? (
              <option value="">No race/species modules found</option>
            ) : (
              raceOptions.map((item) => (
                <option key={item.moduleId} value={item.moduleId}>
                  {item.displayName} ({item.sourceCode}){moduleCompatibilityIssues(item, totalCharacterLevel).length > 0 ? ' - incompatible' : ''}
                </option>
              ))
            )}
          </select>
          <label htmlFor="subrace-module">Subrace (if available)</label>
          <select id="subrace-module" value={effectiveSelectedSubraceModuleId} onChange={(e) => setSelectedSubraceModuleId(e.target.value)}>
            <option value="">None</option>
            {subraceOptions.map((item) => (
              <option key={item.moduleId} value={item.moduleId}>
                {item.displayName} ({item.sourceCode})
              </option>
            ))}
          </select>
          {selectedRaceOption && subraceOptions.length === 0 && (
            <small>No linked subraces were found in the catalog for this race.</small>
          )}
          <label htmlFor="background-module">Background / origin</label>
          <select id="background-module" value={effectiveSelectedBackgroundModuleId} onChange={(e) => setSelectedBackgroundModuleId(e.target.value)}>
            {backgroundOptions.length === 0 ? (
              <option value="">No background/origin modules found</option>
            ) : (
              backgroundOptions.map((item) => (
                <option key={item.moduleId} value={item.moduleId}>
                  {item.displayName} ({item.sourceCode}){moduleCompatibilityIssues(item, totalCharacterLevel).length > 0 ? ' - incompatible' : ''}
                </option>
              ))
            )}
          </select>
          <label htmlFor="multiclass-module">Multiclass option</label>
          <select id="multiclass-module" value={secondaryClassModuleId} onChange={(e) => setSecondaryClassModuleId(e.target.value)}>
            <option value="">Add multiclass option...</option>
            {classOptions
              .filter(
                (x) =>
                  x.moduleId !== effectiveSelectedClassModuleId &&
                  !multiClassSelections.some((entry) => entry.moduleId === x.moduleId) &&
                  classNameByModuleId.get(x.moduleId.toLowerCase())?.toLowerCase() !==
                    classNameByModuleId.get(effectiveSelectedClassModuleId.toLowerCase())?.toLowerCase() &&
                  !multiClassSelections.some((entry) => {
                    const entryClassName = classNameByModuleId.get(entry.moduleId.toLowerCase())?.toLowerCase() ?? ''
                    const optionClassName = classNameByModuleId.get(x.moduleId.toLowerCase())?.toLowerCase() ?? ''
                    return entryClassName.length > 0 && entryClassName === optionClassName
                  }),
              )
              .map((item) => (
                <option key={item.moduleId} value={item.moduleId} disabled={moduleCompatibilityIssues(item, totalCharacterLevel).length > 0}>
                  {item.displayName} ({item.sourceCode}){moduleCompatibilityIssues(item, totalCharacterLevel).length > 0 ? ' - incompatible' : ''}
                </option>
              ))}
          </select>
          <button onClick={addMultiClassSelection} disabled={!effectiveSelectedClassModuleId || !secondaryClassModuleId}>
            Add multiclass
          </button>
        </div>
        {multiClassSelections.length > 0 && (
          <div className="list">
            {multiClassSelections.map((entry) => {
              const option = classOptions.find((x) => x.moduleId === entry.moduleId)
              const subclassChoices = getSubclassChoicesForClass(entry.moduleId)
              const unlockedSubclassChoices = getUnlockedSubclassChoicesForClass(entry.moduleId, entry.level)
              return (
                <div key={entry.moduleId} className="row">
                  <span>{option?.displayName ?? entry.moduleId}</span>
                  <label htmlFor={`multiclass-level-${entry.moduleId}`}>Level</label>
                  <input
                    id={`multiclass-level-${entry.moduleId}`}
                    type="number"
                    min={1}
                    max={20}
                    value={entry.level}
                    onChange={(e) => setMultiClassLevel(entry.moduleId, Number(e.target.value))}
                  />
                  {unlockedSubclassChoices.length > 0 ? (
                    <>
                      <label htmlFor={`multiclass-subclass-${entry.moduleId}`}>Subclass</label>
                      <select
                        id={`multiclass-subclass-${entry.moduleId}`}
                        value={entry.subclassModuleId ?? ''}
                        onChange={(e) => setMultiClassSubclass(entry.moduleId, e.target.value)}
                      >
                        <option value="">None</option>
                        {subclassChoices.map((item) => (
                          <option
                            key={item.moduleId}
                            value={item.moduleId}
                            disabled={moduleCompatibilityIssues(item, entry.level).length > 0}
                          >
                            {item.displayName} ({item.sourceCode})
                            {moduleCompatibilityIssues(item, entry.level).length > 0
                              ? ` - ${moduleCompatibilityIssues(item, entry.level).join(', ')}`
                              : ''}
                          </option>
                        ))}
                      </select>
                    </>
                  ) : (
                    <small>Subclass unlocks at a higher class level for this entry.</small>
                  )}
                  <button onClick={() => removeMultiClassSelection(entry.moduleId)}>Remove</button>
                </div>
              )
            })}
          </div>
        )}
        <div className="row" hidden={newCharacterStep !== 2}>
          <small>Mixed mode compatibility: incompatible options are marked and disabled with prerequisite reasons.</small>
        </div>
        <div className="row" hidden={newCharacterStep !== 2}>
          <small>Race/Species bonuses: {Object.entries(raceBonuses).map(([k, v]) => `${k}+${v}`).join(', ') || 'None'}</small>
        </div>
        {crossRulesetLineageBonusSuppressed && (
          <div className="row" hidden={newCharacterStep !== 2}>
            <small>
              Mixed mode note: cross-ruleset lineage bonuses are not applied by default; base-ruleset mechanics still control
              ability math.
            </small>
          </div>
        )}
        <div className="row" hidden={newCharacterStep !== 2}>
          <small>
            Background/Origin bonuses: {Object.entries(backgroundBonuses).map(([k, v]) => `${k}+${v}`).join(', ') || 'None'}
          </small>
        </div>
        <div className="row" hidden={newCharacterStep !== 2}>
          <small>Starting equipment rule: when using equipment mode, only the first class grants starting equipment.</small>
        </div>

        {buildMethod === 'PointBuy' && <p hidden={newCharacterStep !== 2}>Point-buy spent: {pointBuySpent}/27</p>}
        {buildMethod === 'Roll' && (
          <>
            <div className="row" hidden={newCharacterStep !== 2}>
              <label>
                <input type="checkbox" checked={rerollOnes} onChange={(e) => setRerollOnes(e.target.checked)} /> Reroll 1s
                once
              </label>
              <button onClick={handleRollPoolGenerate}>Roll 4d6 drop lowest (6 stats)</button>
              <p>{isRollAssignmentComplete ? 'All rolls assigned.' : 'Drag or tap to assign each roll to an ability.'}</p>
            </div>
            <div className="roll-pool" hidden={newCharacterStep !== 2}>
              {rolledPool.length === 0 ? (
                <small>No unassigned rolls. Roll to generate values.</small>
              ) : (
                rolledPool.map((value, idx) => (
                  <button
                    key={`${value}-${idx}`}
                    className="roll-chip"
                    draggable
                    onDragStart={(event) => event.dataTransfer.setData('text/plain', String(value))}
                  >
                    {value}
                  </button>
                ))
              )}
            </div>
          </>
        )}

        <div className="scores-grid" hidden={newCharacterStep !== 2}>
          {ABILITIES.map((ability) => (
            <label key={ability} className="score-card">
              <span>{ability}</span>
              {buildMethod === 'PointBuy' ? (
                <div className="row">
                  <button onClick={() => setPointBuyAbilityScore(ability, pointBuyScores[ability] - 1)}>-</button>
                  <strong>{pointBuyScores[ability]}</strong>
                  <button onClick={() => setPointBuyAbilityScore(ability, pointBuyScores[ability] + 1)}>+</button>
                </div>
              ) : buildMethod === 'Manual' ? (
                <input
                  type="number"
                  min={1}
                  max={30}
                  value={manualScores[ability]}
                  onChange={(e) => setManualAbilityScore(ability, Number(e.target.value))}
                />
              ) : (
                <>
                  <div
                    className="roll-target"
                    onDragOver={(event) => event.preventDefault()}
                    onDrop={(event) => {
                      event.preventDefault()
                      const value = Number(event.dataTransfer.getData('text/plain'))
                      if (!Number.isNaN(value)) {
                        assignRollToAbility(ability, value)
                      }
                    }}
                  >
                    <strong>{rolledScores[ability]}</strong>
                    {typeof rollAssignments[ability] === 'number' && (
                      <button type="button" onClick={() => unassignRoll(ability)}>
                        Clear
                      </button>
                    )}
                  </div>
                  {rolledPool.length > 0 && (
                    <div className="row">
                      {rolledPool.map((value, idx) => (
                        <button key={`${ability}-pick-${value}-${idx}`} type="button" onClick={() => assignRollToAbility(ability, value)}>
                          Assign {value}
                        </button>
                      ))}
                    </div>
                  )}
                </>
              )}
              <small>
                total {totalAbilityScores[ability]} | mod {abilityModifier(totalAbilityScores[ability]) >= 0 ? '+' : ''}
                {abilityModifier(totalAbilityScores[ability])}
              </small>
            </label>
          ))}
        </div>
      </section>

      <section className="card" hidden={newCharacterStep !== 3}>
        <h2>3. Wizard + persistent build</h2>
        <div className="row">
          <label htmlFor="main-class-module">Class</label>
          <select id="main-class-module" value={effectiveSelectedClassModuleId} onChange={(e) => handlePrimaryClassChange(e.target.value)}>
            {mainClassOptions.length === 0 ? (
              <option value="">No class modules found in DB</option>
            ) : (
              mainClassOptions.map((item) => (
                <option key={item.moduleId} value={item.moduleId}>
                  {item.className} ({item.sourceCode})
                </option>
              ))
            )}
          </select>
          {unlockedPrimarySubclassOptions.length > 0 ? (
            <>
              <label htmlFor="subclass-module">Subclass</label>
              <select id="subclass-module" value={effectiveSelectedSubclassModuleId} onChange={(e) => setSelectedSubclassModuleId(e.target.value)}>
                <option value="">None</option>
                {primaryClassSubclassOptions.map((item) => (
                  <option key={item.moduleId} value={item.moduleId} disabled={moduleCompatibilityIssues(item, primaryClassLevel).length > 0}>
                    {item.displayName} ({item.sourceCode})
                    {moduleCompatibilityIssues(item, primaryClassLevel).length > 0
                      ? ` - ${moduleCompatibilityIssues(item, primaryClassLevel).join(', ')}`
                      : ''}
                  </option>
                ))}
              </select>
            </>
          ) : (
            <small>Subclass unlocks at a higher class level.</small>
          )}
        </div>
        {classSections.length > 0 && (
          <div className="inventory-list">
            {classSections.map((entry) => (
              <div key={`${entry.moduleId}-${entry.level}`} className="row">
                <strong>{entry.className}</strong>
                <span>Level {entry.level}</span>
                <button onClick={() => void handleLoadRecommendedSpells(entry.moduleId, entry.className, entry.level)} disabled={!currentCharacterId}>
                  See recommended spells for this class
                </button>
                {recommendedSpellsByClass[entry.moduleId] && <small>{recommendedSpellsByClass[entry.moduleId]}</small>}
              </div>
            ))}
          </div>
        )}
        {classCatalogResult && <p>{classCatalogResult}</p>}
        {!savedBuild && buildResult && <p>{buildResult}</p>}
      </section>

      <section className="card" hidden={newCharacterStep !== 3}>
        <h2>4. Character sheet: vitals, spells, resources</h2>
        {sheetResult && <p>{sheetResult}</p>}
        <h3>Vitals</h3>
        <div className="grid">
          <label htmlFor="vitals-max-hp-method">Max HP method</label>
          <select id="vitals-max-hp-method" value={maxHpMethod} onChange={(e) => setMaxHpMethod(e.target.value as 'manual' | 'roll')}>
            <option value="manual">Manual</option>
            <option value="roll">Roll</option>
          </select>
          <label htmlFor="vitals-max-hp">Max HP</label>
          {maxHpMethod === 'manual' ? (
            <input
              id="vitals-max-hp"
              type="number"
              min={0}
              value={vitals.maxHitPoints}
              onChange={(e) => setVitals((prev) => ({ ...prev, maxHitPoints: toNonNegativeInt(e.target.value) }))}
            />
          ) : (
            <div className="row">
              <label>
                <input type="checkbox" checked={generousHitPointRolls} onChange={(e) => setGenerousHitPointRolls(e.target.checked)} /> Generous rolls
              </label>
              <button
                type="button"
                onClick={() => {
                  const classNameForHitDie = selectedPrimaryClassName || 'Fighter'
                  const hitDieSides = parseHitDieSides(classNameForHitDie)
                  const rolledMaxHp = rollHitPointsForLevel(totalCharacterLevel, hitDieSides, generousHitPointRolls)
                  setVitals((prev) => ({
                    ...prev,
                    maxHitPoints: rolledMaxHp,
                    currentHitPoints: rolledMaxHp,
                  }))
                }}
              >
                Roll HP
              </button>
              <small>Current value: {vitals.maxHitPoints}</small>
            </div>
          )}
        </div>
        <h3>Spells</h3>
        <div className="row">
          <small>
            Spell source context: {spellOriginNames.size > 0 ? Array.from(spellOriginNames).join(', ') : 'No class/subclass spell list selected'}
          </small>
          <button
            onClick={() => {
              const firstSpell = availableSpellOptions[0]
              if (!firstSpell) {
                return
              }
              setSpellEntries((prev) => [
                ...prev,
                { spellModuleId: firstSpell.moduleId, spellName: firstSpell.displayName, preparationMode: 'Prepared' },
              ])
            }}
            disabled={!currentCharacterId || availableSpellOptions.length === 0}
          >
            Add spell
          </button>
          <button onClick={handleSaveSpells} disabled={!currentCharacterId}>
            Save spells
          </button>
        </div>
        {availableSpellOptions.length === 0 && <small>No spells available for the currently selected class/subclass.</small>}
        <ul className="inventory-list">
          {spellEntries.map((entry, index) => (
            <li key={`${entry.spellModuleId}-${index}`}>
              <div className="grid">
                <label htmlFor={`spell-module-${index}`}>Spell</label>
                <select
                  id={`spell-module-${index}`}
                  value={entry.spellModuleId}
                  onChange={(e) =>
                    setSpellEntries((prev) => {
                      const selectedSpell = availableSpellOptions.find((spell) => spell.moduleId === e.target.value)
                      return prev.map((spell, i) =>
                        i === index
                          ? {
                              ...spell,
                              spellModuleId: e.target.value,
                              spellName: selectedSpell?.displayName ?? spell.spellName,
                            }
                          : spell,
                      )
                    })
                  }
                >
                  {!availableSpellOptions.some((spell) => spell.moduleId === entry.spellModuleId) && (
                    <option value={entry.spellModuleId}>{entry.spellName || 'Unknown spell'}</option>
                  )}
                  {availableSpellOptions.map((spell) => (
                    <option key={spell.moduleId} value={spell.moduleId}>
                      {spell.displayName} ({spell.sourceCode}){spell.spellClasses.length > 0 ? ` - for ${spell.spellClasses.join(', ')}` : ' - class not specified'}
                    </option>
                  ))}
                </select>
                <label htmlFor={`spell-mode-${index}`}>Preparation mode</label>
                <select
                  id={`spell-mode-${index}`}
                  value={entry.preparationMode}
                  onChange={(e) =>
                    setSpellEntries((prev) =>
                      prev.map((spell, i) => (i === index ? { ...spell, preparationMode: e.target.value } : spell)),
                    )
                  }
                >
                  <option value="Known">Known</option>
                  <option value="Prepared">Prepared</option>
                  <option value="Cantrip">Cantrip</option>
                </select>
              </div>
              <button onClick={() => setSpellEntries((prev) => prev.filter((_, i) => i !== index))}>Remove spell</button>
            </li>
          ))}
        </ul>
        <h3>Resources</h3>
        <div className="row">
          <button
            onClick={() =>
              setResourcePools((prev) => [...prev, { resourceKey: '', currentValue: 0, maxValue: 0, metadataJson: '{}' }])
            }
            disabled={!currentCharacterId}
          >
            Add resource
          </button>
          <button onClick={handleSaveResources} disabled={!currentCharacterId}>
            Save resources
          </button>
        </div>
        <ul className="inventory-list">
          {resourcePools.map((resource, index) => (
            <li key={`${resource.resourceKey}-${index}`}>
              <div className="grid">
                <label htmlFor={`resource-key-${index}`}>Resource key</label>
                <input
                  id={`resource-key-${index}`}
                  value={resource.resourceKey}
                  onChange={(e) =>
                    setResourcePools((prev) =>
                      prev.map((row, i) => (i === index ? { ...row, resourceKey: e.target.value } : row)),
                    )
                  }
                />
                <label htmlFor={`resource-current-${index}`}>Current value</label>
                <input
                  id={`resource-current-${index}`}
                  type="number"
                  min={0}
                  value={resource.currentValue}
                  onChange={(e) =>
                    setResourcePools((prev) =>
                      prev.map((row, i) => (i === index ? { ...row, currentValue: toNonNegativeInt(e.target.value) } : row)),
                    )
                  }
                />
                <label htmlFor={`resource-max-${index}`}>Max value</label>
                <input
                  id={`resource-max-${index}`}
                  type="number"
                  min={0}
                  value={resource.maxValue}
                  onChange={(e) =>
                    setResourcePools((prev) =>
                      prev.map((row, i) => (i === index ? { ...row, maxValue: toNonNegativeInt(e.target.value) } : row)),
                    )
                  }
                />
                <label htmlFor={`resource-meta-${index}`}>Metadata JSON</label>
                <input
                  id={`resource-meta-${index}`}
                  value={resource.metadataJson}
                  onChange={(e) =>
                    setResourcePools((prev) =>
                      prev.map((row, i) => (i === index ? { ...row, metadataJson: e.target.value } : row)),
                    )
                  }
                />
              </div>
              <button
                onClick={() => setResourcePools((prev) => prev.filter((_, i) => i !== index))}
                disabled={CURRENCY_RESOURCE_KEYS.has(resource.resourceKey.trim().toLowerCase())}
              >
                Remove resource
              </button>
              {CURRENCY_RESOURCE_KEYS.has(resource.resourceKey.trim().toLowerCase()) && (
                <small>Currency rows cannot be removed; set them to 0 instead.</small>
              )}
            </li>
          ))}
        </ul>
      </section>

      <section className="card" hidden={newCharacterStep !== 3}>
        <h2>5. Skills setup</h2>
        <p>
          Proficiencies available: {proficiencySlotsAvailable - proficiencySlotsUsed} (used {proficiencySlotsUsed} /{' '}
          {proficiencySlotsAvailable}) | Expertise available: {expertiseSlotsAvailable - expertiseSlotsUsed} (used{' '}
          {expertiseSlotsUsed} / {expertiseSlotsAvailable})
        </p>
        <small>
          Auto skill grants: {autoGrantedSkills.join(', ') || 'None'} | Selectable skill picks:{' '}
          {selectableSkillChoices.join(', ') || 'None'} (remaining {Math.max(0, skillChoiceCount - nonAutoSkillPicksUsed)})
        </small>
        <small>
          Fixed tools: {fixedTools.join(', ') || 'None'} | Selectable tools: {selectableTools.join(', ') || 'None'} (remaining{' '}
          {Math.max(0, toolChoiceCount - selectedToolPicks.length)})
        </small>
        <small>
          Fixed languages: {fixedLanguages.join(', ') || 'None'} | Selectable languages: {selectableLanguages.join(', ') || 'None'} (remaining{' '}
          {Math.max(0, languageChoiceCount - selectedLanguagePicks.length)})
        </small>
        {selectableTools.length > 0 && (
          <div className="row">
            {selectableTools.map((tool) => (
              <button key={tool} type="button" onClick={() => toggleToolPick(tool)}>
                {selectedToolPicks.some((x) => x.toLowerCase() === tool.toLowerCase()) ? 'Unpick' : 'Pick'} tool: {tool}
              </button>
            ))}
          </div>
        )}
        {selectableLanguages.length > 0 && (
          <div className="row">
            {selectableLanguages.map((language) => (
              <button key={language} type="button" onClick={() => toggleLanguagePick(language)}>
                {selectedLanguagePicks.some((x) => x.toLowerCase() === language.toLowerCase()) ? 'Unpick' : 'Pick'} language: {language}
              </button>
            ))}
          </div>
        )}
        {skillSections.map((section) => {
          const sectionFixedLower = section.fixed.map((x) => x.toLowerCase())
          const sectionChoiceLower = section.choices.map((x) => x.toLowerCase())
          const sectionPicksUsed = section.choices.filter(
            (skill) =>
              !sectionFixedLower.includes(skill.toLowerCase()) &&
              effectiveSkillTrainingBySkill[skill as SkillName] !== 'None',
          ).length
          return (
            <div key={section.key}>
              <h3>{section.title}</h3>
              {section.choiceCount > 0 && (
                <small>
                  Choose {section.choiceCount} ({Math.max(0, section.choiceCount - sectionPicksUsed)} remaining)
                </small>
              )}
              <div className="skills-grid">
                {Array.from(new Set([...section.fixed, ...section.choices])).map((skillName) => {
                  const skill = skillName as SkillName
                  const isFixedFromThisSection = sectionFixedLower.includes(skill.toLowerCase())
                  const isFixedFromOtherSection =
                    autoGrantedSkills.some((x) => x.toLowerCase() === skill.toLowerCase()) && !isFixedFromThisSection
                  const isChoice = sectionChoiceLower.includes(skill.toLowerCase())
                  const checked = effectiveSkillTrainingBySkill[skill] !== 'None'
                  const disableChoice =
                    !checked && isChoice && section.choiceCount > 0 && sectionPicksUsed >= section.choiceCount
                  return (
                    <label key={`${section.key}-${skill}`} className="row">
                      <input
                        type="checkbox"
                        checked={checked}
                        disabled={isFixedFromThisSection || isFixedFromOtherSection || disableChoice}
                        onChange={() => setSkillTraining(skill, checked ? 'None' : 'Proficient')}
                      />
                      {skill} ({SKILL_ABILITY[skill].slice(0, 3)}) mod {skillModifier(skill) >= 0 ? '+' : ''}
                      {skillModifier(skill)}
                    </label>
                  )
                })}
              </div>
            </div>
          )
        })}
        <h3>All skills (manual override)</h3>
        <div className="skills-grid">
          {ALL_SKILLS.map((skill) => (
            <label key={skill}>
              <select
                value={effectiveSkillTrainingBySkill[skill]}
                onChange={(e) => setSkillTraining(skill, e.target.value as SkillTrainingLevel)}
              >
                <option value="None">None</option>
                <option value="Proficient">Proficient</option>
                <option value="Expertise">Expertise</option>
              </select>
              {skill} ({SKILL_ABILITY[skill].slice(0, 3)}) mod {skillModifier(skill) >= 0 ? '+' : ''}
              {skillModifier(skill)}
            </label>
          ))}
        </div>
      </section>

      <section className="card" hidden={newCharacterStep !== 4}>
        <h2>6. Inventory from database (persisted)</h2>
        <h3>Coin purse</h3>
        <div className="grid">
          <label htmlFor="currency-cp">cp</label>
          <input id="currency-cp" type="number" min={0} value={currencyDraft.cp} onBlur={() => void handleSaveCurrency()} onChange={(e) => setCurrencyDraft((prev) => ({ ...prev, cp: Math.max(0, Number(e.target.value) || 0) }))} />
          <label htmlFor="currency-sp">sp</label>
          <input id="currency-sp" type="number" min={0} value={currencyDraft.sp} onBlur={() => void handleSaveCurrency()} onChange={(e) => setCurrencyDraft((prev) => ({ ...prev, sp: Math.max(0, Number(e.target.value) || 0) }))} />
          <label htmlFor="currency-ep">ep</label>
          <input id="currency-ep" type="number" min={0} value={currencyDraft.ep} onBlur={() => void handleSaveCurrency()} onChange={(e) => setCurrencyDraft((prev) => ({ ...prev, ep: Math.max(0, Number(e.target.value) || 0) }))} />
          <label htmlFor="currency-gp">gp</label>
          <input id="currency-gp" type="number" min={0} value={currencyDraft.gp} onBlur={() => void handleSaveCurrency()} onChange={(e) => setCurrencyDraft((prev) => ({ ...prev, gp: Math.max(0, Number(e.target.value) || 0) }))} />
          <label htmlFor="currency-pp">pp</label>
          <input id="currency-pp" type="number" min={0} value={currencyDraft.pp} onBlur={() => void handleSaveCurrency()} onChange={(e) => setCurrencyDraft((prev) => ({ ...prev, pp: Math.max(0, Number(e.target.value) || 0) }))} />
        </div>
        <div className="row">
          <label>
            <input type="checkbox" checked={usePlatinumConsolidation} onChange={(e) => setUsePlatinumConsolidation(e.target.checked)} /> Consolidate toward platinum
          </label>
          <button onClick={handleConsolidateCurrency} disabled={!currentCharacterId}>Consolidate pocket change (gp default)</button>
        </div>
        <div className="row">
          <label htmlFor="catalog-item">Item</label>
          <select id="catalog-item" value={effectiveSelectedCatalogItemId} onChange={(e) => setSelectedCatalogItemId(e.target.value)}>
            {filteredItemCatalog.length === 0 ? (
              <option value="">No item definitions found in DB</option>
            ) : (
              filteredItemCatalog.map((item) => (
                <option key={item.itemId} value={item.itemId}>
                  {getDisplayItemName(item.itemName, item.itemId)} ({item.sourceCode}) {item.requiresAttunement ? '[attunement]' : ''}
                </option>
              ))
            )}
          </select>
          <label htmlFor="catalog-item-quantity">Quantity</label>
          <input
            id="catalog-item-quantity"
            type="number"
            min={1}
            value={selectedCatalogQuantity}
            onChange={(e) => setSelectedCatalogQuantity(Math.max(1, Number(e.target.value) || 1))}
            placeholder="Quantity"
          />
          <label>
            <input type="checkbox" checked={purchaseFromCurrencyMode} onChange={(e) => setPurchaseFromCurrencyMode(e.target.checked)} /> Purchase (deduct currency)
          </label>
          <button onClick={() => void handleAddItemFromCatalog()} disabled={!effectiveSelectedCatalogItemId || !currentCharacterId}>
            Add to persisted inventory
          </button>
        </div>
        {effectiveSelectedCatalogItemId && (
          <small>
            {(() => {
              const selectedItem = filteredItemCatalog.find((item) => item.itemId === effectiveSelectedCatalogItemId)
              if (!selectedItem) {
                return 'Select an item to see details.'
              }
              return `${selectedItem.itemType} | ${selectedItem.rarity} | ${selectedItem.goldValue} gp | ${selectedItem.weight} lb${selectedItem.description ? ` | ${selectedItem.description}` : ''}`
            })()}
          </small>
        )}
        {currencyState && (
          <small>
            Current purse: {currencyState.cp} cp, {currencyState.sp} sp, {currencyState.ep} ep, {currencyState.gp} gp, {currencyState.pp} pp
          </small>
        )}
        <ul className="inventory-list">
          {(inventoryState?.items ?? []).map((item) => (
            <li key={item.inventoryItemId}>
              <strong>{getDisplayItemName(item.itemName, item.itemDefinitionId)}</strong>
              <span>
                type {item.itemType} | qty {item.quantity} | value {item.goldValue} gp | weight {item.weight} lb |{' '}
                {item.requiresAttunement ? 'requires attunement' : 'no attunement'} | {item.isEquipped ? 'equipped' : 'unequipped'} |{' '}
                {item.isAttuned ? 'attuned' : 'not attuned'}
              </span>
              {item.isWeapon && (
                <small>
                  weapon: {item.damageDice} using {item.weaponAbility || 'Strength'} (atk bonus {item.attackBonus}, dmg bonus {item.damageBonus})
                </small>
              )}
              <div className="row">
                <button
                  onClick={() => void handleUpdateInventoryItem(item.inventoryItemId, { isEquipped: !item.isEquipped })}
                >
                  {item.isEquipped ? 'Unequip' : 'Equip'}
                </button>
                {item.requiresAttunement && (
                  <button
                    onClick={() => void handleUpdateInventoryItem(item.inventoryItemId, { isAttuned: !item.isAttuned })}
                  >
                    {item.isAttuned ? 'Unattune' : 'Attune'}
                  </button>
                )}
                <label htmlFor={`inventory-quantity-${item.inventoryItemId}`}>Qty</label>
                <input
                  id={`inventory-quantity-${item.inventoryItemId}`}
                  type="number"
                  min={1}
                  value={item.quantity}
                  onChange={(e) =>
                    void handleUpdateInventoryItem(item.inventoryItemId, {
                      quantity: Math.max(1, Number(e.target.value) || 1),
                    })
                  }
                />
                <button onClick={() => void handleRemoveItem(item.inventoryItemId)}>Remove</button>
              </div>
            </li>
          ))}
        </ul>
      </section>
            </>
          )}
        </>
      )}
    </main>
  )
}

export default App
