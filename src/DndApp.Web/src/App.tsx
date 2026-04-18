import { type ChangeEvent, useEffect, useMemo, useState } from 'react'
import './App.css'
import {
  addInventoryItem,
  archiveCharacter,
  computePersistedCheck,
  computePersistedAttack,
  copyToRuleset,
  deleteCharacter,
  duplicateCharacter,
  finalizeWizard,
  getArchivedCharacters,
  getAttunementGuidance,
  getCharacterBuild,
  getCharacterHistory,
  getCharacterInventory,
  getCharacterResources,
  getCharacterSpells,
  getCharacterVitals,
  getCharacters,
  getClassCatalog,
  getContentSources,
  getModuleCatalog,
  getItemCatalog,
  health,
  loginLocal,
  registerLocal,
  patchInventoryItem,
  previewOrigin,
  previewSpecies,
  removeInventoryItem,
  restoreCharacter,
  setSessionToken,
  startWizard,
  submitWizardStep,
  upsertCharacterResources,
  upsertCharacterBuild,
  upsertCharacterSpells,
  upsertCharacterVitals,
} from './api'
import type {
  AbilityName,
  AdvantageState,
  BuildMethod,
  CharacterBuildData,
  CharacterHistoryEntry,
  CharacterInventoryState,
  CharacterResourcePoolData,
  CharacterSpellEntryData,
  CharacterSummary,
  CharacterVitalsData,
  CharacterWizardResult,
  ClassCatalogItem,
  ContentSourceCatalogItem,
  ItemCatalogItem,
  LocalSession,
  ModuleCatalogItem,
  RuleSystemMode,
  SkillName,
} from './types'

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
const KNOWN_BACKGROUNDS = new Set([
  'Acolyte',
  'Artisan',
  'Charlatan',
  'Criminal',
  'Entertainer',
  'Folk Hero',
  'Guild Artisan',
  'Hermit',
  'Noble',
  'Sage',
  'Sailor',
  'Soldier',
  'Urchin',
])

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
  const [error, setError] = useState('')
  const [currentPath, setCurrentPath] = useState(window.location.pathname || '/login')

  const [wizardName, setWizardName] = useState('')
  const [baseRules, setBaseRules] = useState<RuleSystemMode>('Rules2024')
  const [mixedMode, setMixedMode] = useState(false)
  const [overlaySourceOptions, setOverlaySourceOptions] = useState<ContentSourceCatalogItem[]>([])
  const [overlaySources, setOverlaySources] = useState<string[]>([])
  const [activeDraft, setActiveDraft] = useState<CharacterWizardResult | null>(null)

  const [classCatalog, setClassCatalog] = useState<ClassCatalogItem[]>([])
  const [moduleCatalog, setModuleCatalog] = useState<ModuleCatalogItem[]>([])
  const [selectedClassModuleId, setSelectedClassModuleId] = useState('')
  const [selectedSubclassModuleId, setSelectedSubclassModuleId] = useState('')
  const [selectedRaceModuleId, setSelectedRaceModuleId] = useState('')
  const [selectedBackgroundModuleId, setSelectedBackgroundModuleId] = useState('')
  const [secondaryClassModuleId, setSecondaryClassModuleId] = useState('')
  const [multiClassSelections, setMultiClassSelections] = useState<Array<{ moduleId: string; level: number }>>([])
  const [classCatalogResult, setClassCatalogResult] = useState('')

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

  const [itemCatalog, setItemCatalog] = useState<ItemCatalogItem[]>([])
  const [selectedCatalogItemId, setSelectedCatalogItemId] = useState('')
  const [selectedCatalogQuantity, setSelectedCatalogQuantity] = useState(1)
  const [inventoryState, setInventoryState] = useState<CharacterInventoryState | null>(null)
  const [inventoryResult, setInventoryResult] = useState('')
  const [attunementGuidance, setAttunementGuidance] = useState('')

  const [selectedSkill, setSelectedSkill] = useState<SkillName>('Stealth')
  const [advantageState, setAdvantageState] = useState<AdvantageState>('None')
  const [rollResult, setRollResult] = useState('')
  const [attackAdvantageState, setAttackAdvantageState] = useState<AdvantageState>('None')
  const [attackResults, setAttackResults] = useState<Record<string, string>>({})
  const [spellEntries, setSpellEntries] = useState<CharacterSpellEntryData[]>([])
  const [resourcePools, setResourcePools] = useState<CharacterResourcePoolData[]>([])
  const [vitals, setVitals] = useState<Omit<CharacterVitalsData, 'characterId' | 'updatedAtUtc'>>({
    ...DEFAULT_VITALS,
  })
  const [sheetResult, setSheetResult] = useState('')

  const [originPreview, setOriginPreview] = useState('')
  const [speciesPreview, setSpeciesPreview] = useState('')

  const currentCharacterId = useMemo(
    () => selectedCharacterId || activeDraft?.draft?.characterId || '',
    [selectedCharacterId, activeDraft?.draft?.characterId],
  )
  const isCharactersRoute = currentPath === '/' || currentPath === '/characters'
  const isArchivedRoute = currentPath === '/characters/archived'
  const isNewCharacterRoute = currentPath.startsWith('/characters/new')

  const classOptions = useMemo(
    () =>
      moduleCatalog.filter(
        (x) => x.moduleType.toLowerCase() === 'class' && KNOWN_CLASSES.has(x.displayName),
      ),
    [moduleCatalog],
  )
  const mainClassOptions = useMemo(() => classCatalog, [classCatalog])
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
        (x) =>
          (x.moduleType.toLowerCase() === 'background' || x.moduleType.toLowerCase() === 'origin') &&
          KNOWN_BACKGROUNDS.has(x.displayName),
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
    return selected?.abilityBonuses ?? {}
  }, [raceOptions, effectiveSelectedRaceModuleId])

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
  const effectiveSelectedSubclassModuleId = useMemo(
    () =>
      selectedSubclassModuleId && subclassOptions.some((x) => x.moduleId === selectedSubclassModuleId)
        ? selectedSubclassModuleId
        : '',
    [selectedSubclassModuleId, subclassOptions],
  )

  const pointBuySpent = useMemo(
    () => ABILITIES.reduce((sum, ability) => sum + POINT_BUY_COST[pointBuyScores[ability]], 0),
    [pointBuyScores],
  )

  const proficiencyBonus = useMemo(() => Math.floor((totalCharacterLevel - 1) / 4) + 2, [totalCharacterLevel])

  const proficiencySlotsAvailable = useMemo(
    () =>
      (selectedClassOption?.skillChoiceCount ?? 0) +
      (selectedClassOption?.fixedSkillProficiencies.length ?? 0) +
      (selectedBackgroundOption?.fixedSkillProficiencies.length ?? 0),
    [selectedClassOption, selectedBackgroundOption],
  )
  const expertiseSlotsAvailable = useMemo(() => selectedClassOption?.expertiseChoiceCount ?? 0, [selectedClassOption])
  const proficiencySlotsUsed = useMemo(
    () => ALL_SKILLS.filter((skill) => skillTrainingBySkill[skill] !== 'None').length,
    [skillTrainingBySkill],
  )
  const expertiseSlotsUsed = useMemo(
    () => ALL_SKILLS.filter((skill) => skillTrainingBySkill[skill] === 'Expertise').length,
    [skillTrainingBySkill],
  )

  const attacks = useMemo(() => {
    const grouped = new Map<string, NonNullable<CharacterInventoryState['items']>[number]>()
    for (const item of inventoryState?.items ?? []) {
      if (!item.isWeapon) continue
      const key = [
        getDisplayItemName(item.itemName, item.itemDefinitionId),
        item.damageDice,
        item.weaponAbility,
        item.attackBonus,
        item.damageBonus,
      ].join('|')
      if (!grouped.has(key)) {
        grouped.set(key, item)
      }
    }
    return Array.from(grouped.values()).map((item) => {
      const abilityName = (item.weaponAbility || 'Strength') as AbilityName
      const abilityMod = abilityModifier(totalAbilityScores[abilityName] ?? 10)
      const toHit = abilityMod + proficiencyBonus + item.attackBonus
      const damageBonus = abilityMod + item.damageBonus
      return { ...item, abilityName, toHit, damageBonus }
    })
  }, [inventoryState, proficiencyBonus, totalAbilityScores])

  useEffect(() => {
    health()
      .then((r) => setStatus(`API online (${r.status})`))
      .catch(() => setStatus('API unreachable (start DndApp.Api on localhost:5080)'))
  }, [])

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
    const [classes, items, guidance, sources] = await Promise.all([
      getClassCatalog(ruleSystem),
      getItemCatalog(),
      getAttunementGuidance(),
      getContentSources(ruleSystem),
    ])
    const validOverlaySources = overlaySources.filter((code) => sources.some((src) => src.sourceCode === code))
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
      (x) =>
        (x.moduleType.toLowerCase() === 'background' || x.moduleType.toLowerCase() === 'origin') &&
        KNOWN_BACKGROUNDS.has(x.displayName),
    )
    setClassCatalog(classes)
    setModuleCatalog(modules)
    setItemCatalog(items)
    setAttunementGuidance(JSON.stringify(guidance, null, 2))
    setOverlaySourceOptions(sources)
    setOverlaySources(validOverlaySources)
    if (!selectedClassModuleId && classes.length > 0) {
      setSelectedClassModuleId(classes[0].moduleId)
    }
    const selectedRaceStillVisible = filteredRaceOptions.some((x) => x.moduleId === selectedRaceModuleId)
    if (!selectedRaceStillVisible) {
      const firstRace = filteredRaceOptions[0]
      if (firstRace) {
        setSelectedRaceModuleId(firstRace.moduleId)
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
      const persistedClassLevels = build.classLevels.length > 0 ? build.classLevels : [{ classModuleId: build.classModuleId, className: build.className, level: build.level, sortOrder: 0 }]
      const primaryPersistedClass = persistedClassLevels[0]
      setSelectedClassModuleId(primaryPersistedClass.classModuleId)
      setPrimaryClassLevel(primaryPersistedClass.level)
      setMultiClassSelections(
        persistedClassLevels
          .slice(1)
          .map((entry) => ({ moduleId: entry.classModuleId, level: entry.level })),
      )
      const subclassModule = build.selectedModules.find((x) => x.slot.toLowerCase() === 'subclass')
      const raceModule = build.selectedModules.find((x) => x.slot.toLowerCase() === 'race' || x.slot.toLowerCase() === 'species')
      const backgroundModule = build.selectedModules.find(
        (x) => x.slot.toLowerCase() === 'background' || x.slot.toLowerCase() === 'origin',
      )
      if (subclassModule) setSelectedSubclassModuleId(subclassModule.moduleId)
      if (raceModule) setSelectedRaceModuleId(raceModule.moduleId)
      if (backgroundModule) setSelectedBackgroundModuleId(backgroundModule.moduleId)
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
    } catch {
      setSavedBuild(null)
      setBuildResult('No persisted build for selected character yet.')
    }

    try {
      const inventory = await getCharacterInventory(characterId)
      setInventoryState(inventory)
      setInventoryResult(JSON.stringify(inventory, null, 2))
    } catch {
      setInventoryState(null)
      setInventoryResult('')
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
    } catch {
      setResourcePools([])
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
    } catch {
      setVitals({ ...DEFAULT_VITALS })
    }

    setSheetResult('')
  }

  async function refreshCharacters() {
    const list = await getCharacters(false, true)
    setCharacters(list)
    if (!selectedCharacterId && list.length > 0) {
      setSelectedCharacterId(list[0].characterId)
      await loadPersistedCharacterState(list[0].characterId)
    }
  }

  async function refreshArchivedCharacters() {
    const list = await getArchivedCharacters(true)
    setArchivedCharacters(list)
  }

  async function handleSelectCharacter(characterId: string) {
    setSelectedCharacterId(characterId)
    await loadPersistedCharacterState(characterId)
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
    setSelectedCharacterId('')
    setHistory([])
    setSavedBuild(null)
    setInventoryState(null)
    setActiveDraft(null)
    setSpellEntries([])
    setResourcePools([])
    setVitals({ ...DEFAULT_VITALS })
    setSheetResult('')
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

  async function handleStartWizard() {
    if (!session) return
    try {
      setError('')
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
      navigate('/characters/new')
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleApplySelectedClassToWizard() {
    if (!activeDraft?.draft || !effectiveSelectedClassModuleId) return
    const selectedClass = classCatalog.find((x) => x.moduleId === effectiveSelectedClassModuleId)
    const selectedClassModule = classOptions.find((x) => x.moduleId === effectiveSelectedClassModuleId)
    if (!selectedClass && !selectedClassModule) return
    try {
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

      let result = await submitWizardStep(activeDraft.draft.characterId, 'class', classSelections)

      if (effectiveSelectedRaceModuleId) {
        const race = raceOptions.find((x) => x.moduleId === effectiveSelectedRaceModuleId)
        if (race) {
          result = await submitWizardStep(activeDraft.draft.characterId, 'race', [
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

      if (effectiveSelectedBackgroundModuleId) {
        const background = backgroundOptions.find((x) => x.moduleId === effectiveSelectedBackgroundModuleId)
        if (background) {
          result = await submitWizardStep(activeDraft.draft.characterId, 'background', [
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
        const subclass = subclassOptions.find((x) => x.moduleId === effectiveSelectedSubclassModuleId)
        if (subclass) {
          result = await submitWizardStep(activeDraft.draft.characterId, 'subclass', [
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
    if (!activeDraft?.draft) return
    try {
      setError('')
      const result = await finalizeWizard(activeDraft.draft.characterId)
      setActiveDraft(result)
      await refreshCharacters()
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleSaveBuildToDb() {
    if (!currentCharacterId) {
      setError('Select or create a character first.')
      return
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
        ...(effectiveSelectedSubclassModuleId
          ? [
              (() => {
                const subclass = subclassOptions.find((x) => x.moduleId === effectiveSelectedSubclassModuleId)
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
      ].filter((x): x is { slot: string; moduleId: string; displayName: string; sourceCode: string } => x !== null)

      const saved = await upsertCharacterBuild(currentCharacterId, {
        characterName: wizardName,
        baseRuleSystem: baseRules,
        buildMethod,
        classModuleId: effectiveSelectedClassModuleId,
        className: secondarySummary ? `${primaryClassName} (Primary); ${secondarySummary}` : primaryClassName,
        level: totalCharacterLevel,
        proficiencyBonus,
        abilityScores: totalAbilityScores,
        proficientSkills: ALL_SKILLS.filter((skill) => skillTrainingBySkill[skill] !== 'None'),
        skillTrainingBySkill,
        classLevels,
        selectedModules,
      })
      setSavedBuild(saved)
      setBuildResult(JSON.stringify(saved, null, 2))
      const inventory = await getCharacterInventory(currentCharacterId).catch(() => null)
      if (inventory) {
        setInventoryState(inventory)
        setInventoryResult(JSON.stringify(inventory, null, 2))
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

  function handleOverlaySourcesChange(event: ChangeEvent<HTMLSelectElement>) {
    const selected = Array.from(event.target.selectedOptions).map((x) => x.value)
    setOverlaySources(selected)
    if (session) {
      void loadCatalogData(baseRules).catch((e) => setError(String(e)))
    }
  }

  function addMultiClassSelection() {
    if (!secondaryClassModuleId) return
    if (secondaryClassModuleId === effectiveSelectedClassModuleId) return
    if (multiClassSelections.some((x) => x.moduleId === secondaryClassModuleId)) return
    setMultiClassSelections((prev) => [...prev, { moduleId: secondaryClassModuleId, level: 1 }])
  }

  function removeMultiClassSelection(moduleId: string) {
    setMultiClassSelections((prev) => prev.filter((x) => x.moduleId !== moduleId))
  }

  function setMultiClassLevel(moduleId: string, level: number) {
    const bounded = Math.max(1, Math.min(20, level))
    setMultiClassSelections((prev) => prev.map((x) => (x.moduleId === moduleId ? { ...x, level: bounded } : x)))
  }

  function setSkillTraining(skill: SkillName, level: SkillTrainingLevel) {
    setSkillTrainingBySkill((prev) => ({ ...prev, [skill]: level }))
  }

  function setManualAbilityScore(ability: AbilityName, score: number) {
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
    if (!currentCharacterId || !selectedCatalogItemId) return
    try {
      const next = await addInventoryItem(currentCharacterId, selectedCatalogItemId, selectedCatalogQuantity)
      setInventoryState(next)
      setInventoryResult(JSON.stringify(next, null, 2))
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
      setInventoryResult(JSON.stringify(next, null, 2))
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
      setInventoryResult(JSON.stringify(next, null, 2))
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleRollSkillCheck() {
    if (!currentCharacterId) return
    try {
      const result = await computePersistedCheck(currentCharacterId, {
        skillName: selectedSkill,
        advantageState,
        rollDice: true,
        additionalModifier: 0,
        hasExpertise: skillTrainingBySkill[selectedSkill] === 'Expertise',
      })
      setRollResult(JSON.stringify((result as { result?: unknown }).result ?? result, null, 2))
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleRollAttack(attackKey: string, attack: (typeof attacks)[number]) {
    if (!currentCharacterId) return
    try {
      const result = await computePersistedAttack(currentCharacterId, {
        weaponName: getDisplayItemName(attack.itemName, attack.itemDefinitionId),
        abilityName: attack.abilityName,
        isProficientWithWeapon: true,
        additionalAttackModifier: attack.attackBonus,
        damageDice: attack.damageDice || '1d6',
        additionalDamageModifier: attack.damageBonus,
        advantageState: attackAdvantageState,
        rollDice: true,
      })
      setAttackResults((prev) => ({
        ...prev,
        [attackKey]: JSON.stringify((result as { result?: unknown }).result ?? result),
      }))
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

  async function handlePreviewOrigin() {
    const result = await previewOrigin({ name: 'Wanderer-Born', mode: 'GuidedCustom' })
    setOriginPreview(JSON.stringify(result, null, 2))
  }

  async function handlePreviewSpecies() {
    const result = await previewSpecies({ name: 'Stormkin', mode: 'GuidedCustom' })
    setSpeciesPreview(JSON.stringify(result, null, 2))
  }

  function skillModifier(skill: SkillName) {
    const ability = SKILL_ABILITY[skill]
    const abilityMod = abilityModifier(totalAbilityScores[ability])
    const training = skillTrainingBySkill[skill]
    if (training === 'Expertise') {
      return abilityMod + proficiencyBonus * 2
    }
    if (training === 'Proficient') {
      return abilityMod + proficiencyBonus
    }
    return abilityMod
  }

  return (
    <main className="layout">
      <header>
        <h1>Welcome to DndAppName</h1>
        <p>{status}</p>
      </header>

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
              <button onClick={() => navigate('/characters/new')}>Create new character</button>
            </div>
            <p>
              <strong>Getting started:</strong> pick a character (or start a wizard draft), then complete build setup and
              use the sheet sections for vitals, spells, resources, skills, and inventory.
            </p>
          </section>

          {isCharactersRoute && (
            <section className="card">
            <h2>1. Your characters</h2>
            <div className="row">
              <button onClick={() => void refreshCharacters()} disabled={!session}>
                Refresh
              </button>
              <button onClick={() => navigate('/characters/new')}>Create new character</button>
              <button onClick={() => navigate('/characters/archived')}>Archived characters</button>
              <button onClick={handleCopyRuleset} disabled={!selectedCharacterId}>
                Copy to other ruleset
              </button>
            </div>
            <ul className="list">
              {characters.map((c) => (
                <li key={c.characterId} className={selectedCharacterId === c.characterId ? 'selected' : ''}>
                  <button onClick={() => void handleSelectCharacter(c.characterId)}>{c.characterName}</button>
                  <span className="ruleset-badge">{rulesetLabel(c.baseRuleSystem)}</span>
                  <button onClick={() => void handleArchive(c.characterId)}>Archive</button>
                  <button onClick={() => void handleDuplicate(c.characterId)}>Duplicate</button>
                  <button onClick={() => void handleLoadHistory(c.characterId)}>History</button>
                </li>
              ))}
            </ul>
            {history.length > 0 && (
              <details>
                <summary>Technical details</summary>
                <pre>{JSON.stringify(history, null, 2)}</pre>
              </details>
            )}
            </section>
          )}

          {isArchivedRoute && (
            <section className="card">
              <h2>Archived characters</h2>
              <div className="row">
                <button onClick={() => void refreshArchivedCharacters()} disabled={!session}>
                  Refresh archived
                </button>
                <button onClick={() => navigate('/characters')}>Back to active characters</button>
              </div>
              <ul className="list">
                {archivedCharacters.map((c) => (
                  <li key={c.characterId}>
                    <button onClick={() => void handleSelectCharacter(c.characterId)}>{c.characterName}</button>
                    <span className="ruleset-badge">{rulesetLabel(c.baseRuleSystem)}</span>
                    <button onClick={() => void handleRestore(c.characterId)}>Restore</button>
                    <button onClick={() => void handleDelete(c.characterId)}>Delete permanently</button>
                  </li>
                ))}
              </ul>
            </section>
          )}

          {isNewCharacterRoute && (
            <>
            <section className="card">
        <h2>2. Character build setup</h2>
        <div className="grid">
          <label htmlFor="character-name">Character name</label>
          <input id="character-name" value={wizardName} onChange={(e) => setWizardName(e.target.value)} placeholder="Character name" />
          <label htmlFor="base-rules">Base ruleset</label>
          <select id="base-rules" value={baseRules} onChange={(e) => void handleBaseRulesChange(e.target.value as RuleSystemMode)}>
            <option value="Rules2024">2024 rules</option>
            <option value="Rules2014">2014 rules</option>
          </select>
          <label htmlFor="build-method">Ability score method</label>
          <select id="build-method" value={buildMethod} onChange={(e) => setBuildMethod(e.target.value as BuildMethod)}>
            <option value="PointBuy">Point buy</option>
            <option value="Manual">Manual entry</option>
            <option value="Roll">Roll</option>
          </select>
          <label>
            <input
              type="checkbox"
              checked={mixedMode}
              onChange={(e) => {
                const nextMixedMode = e.target.checked
                setMixedMode(nextMixedMode)
                if (!nextMixedMode) {
                  setOverlaySources([])
                }
              }}
            />{' '}
            Mixed mode
          </label>
          <label htmlFor="overlay-sources">Overlay sources</label>
          <select id="overlay-sources" multiple value={overlaySources} onChange={handleOverlaySourcesChange} disabled={!mixedMode}>
            {overlaySourceOptions.length === 0 ? (
              <option value="">No overlay sources available</option>
            ) : (
              overlaySourceOptions.map((source) => (
                <option key={source.sourceCode} value={source.sourceCode}>
                  {source.sourceCode} - {source.sourceName}
                </option>
              ))
            )}
          </select>
          <label htmlFor="primary-class-level">Primary class level</label>
          <input
            id="primary-class-level"
            type="number"
            min={1}
            max={20}
            value={primaryClassLevel}
            onChange={(e) => setPrimaryClassLevel(Math.max(1, Math.min(20, Number(e.target.value))))}
            placeholder="Primary class level"
          />
        </div>
        <div className="row">
          <button onClick={() => void loadCatalogData(baseRules)} disabled={!session}>
            Refresh content catalogs
          </button>
        </div>
        {mixedMode && overlaySources.length === 0 && (
          <small>Select one or more overlay sources to include mixed-rule catalog modules.</small>
        )}
        <p>
          Total level: {totalCharacterLevel} | Proficiency bonus: +{proficiencyBonus}
        </p>
        <div className="grid">
          <label htmlFor="race-module">Race / species</label>
          <select id="race-module" value={effectiveSelectedRaceModuleId} onChange={(e) => setSelectedRaceModuleId(e.target.value)}>
            {raceOptions.length === 0 ? (
              <option value="">No race/species modules found</option>
            ) : (
              raceOptions.map((item) => (
                <option key={item.moduleId} value={item.moduleId}>
                  {item.displayName} ({item.sourceCode})
                </option>
              ))
            )}
          </select>
          <label htmlFor="background-module">Background / origin</label>
          <select id="background-module" value={effectiveSelectedBackgroundModuleId} onChange={(e) => setSelectedBackgroundModuleId(e.target.value)}>
            {backgroundOptions.length === 0 ? (
              <option value="">No background/origin modules found</option>
            ) : (
              backgroundOptions.map((item) => (
                <option key={item.moduleId} value={item.moduleId}>
                  {item.displayName} ({item.sourceCode})
                </option>
              ))
            )}
          </select>
          <label htmlFor="multiclass-module">Multiclass option</label>
          <select id="multiclass-module" value={secondaryClassModuleId} onChange={(e) => setSecondaryClassModuleId(e.target.value)}>
            <option value="">Add multiclass option...</option>
            {classOptions
              .filter((x) => x.moduleId !== effectiveSelectedClassModuleId)
              .map((item) => (
                <option key={item.moduleId} value={item.moduleId}>
                  {item.displayName} ({item.sourceCode})
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
                  <button onClick={() => removeMultiClassSelection(entry.moduleId)}>Remove</button>
                </div>
              )
            })}
          </div>
        )}
        <div className="row">
          <small>Race/Species bonuses: {Object.entries(raceBonuses).map(([k, v]) => `${k}+${v}`).join(', ') || 'None'}</small>
        </div>
        <div className="row">
          <small>
            Background/Origin bonuses: {Object.entries(backgroundBonuses).map(([k, v]) => `${k}+${v}`).join(', ') || 'None'}
          </small>
        </div>

        {buildMethod === 'PointBuy' && <p>Point-buy spent: {pointBuySpent}/27</p>}
        {buildMethod === 'Roll' && (
          <>
            <div className="row">
              <label>
                <input type="checkbox" checked={rerollOnes} onChange={(e) => setRerollOnes(e.target.checked)} /> Reroll 1s
                once
              </label>
              <button onClick={handleRollPoolGenerate}>Roll 4d6 drop lowest (6 stats)</button>
              <p>{isRollAssignmentComplete ? 'All rolls assigned.' : 'Drag or tap to assign each roll to an ability.'}</p>
            </div>
            <div className="roll-pool">
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

        <div className="scores-grid">
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

      <section className="card">
        <h2>3. Wizard + persistent build</h2>
        <button onClick={handleStartWizard} disabled={!session}>
          Start Wizard Draft
        </button>
        <div className="row">
          <label htmlFor="main-class-module">Class</label>
          <select id="main-class-module" value={effectiveSelectedClassModuleId} onChange={(e) => setSelectedClassModuleId(e.target.value)}>
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
          <label htmlFor="subclass-module">Subclass</label>
          <select id="subclass-module" value={effectiveSelectedSubclassModuleId} onChange={(e) => setSelectedSubclassModuleId(e.target.value)}>
            <option value="">None</option>
            {subclassOptions.map((item) => (
              <option key={item.moduleId} value={item.moduleId}>
                {item.displayName} ({item.sourceCode})
              </option>
            ))}
          </select>
          <button
            onClick={handleApplySelectedClassToWizard}
            disabled={!activeDraft?.draft || mainClassOptions.length === 0}
          >
            Apply selected class to wizard
          </button>
          <button
            onClick={handleSaveBuildToDb}
            disabled={
              !currentCharacterId ||
              mainClassOptions.length === 0 ||
              (buildMethod === 'Roll' && !isRollAssignmentComplete)
            }
          >
            Save build to persistent model
          </button>
        </div>
        <div className="row">
          <button onClick={handleFinalizeWizard} disabled={!activeDraft?.draft}>
            Finalize
          </button>
        </div>
        {classCatalogResult && <p>{classCatalogResult}</p>}
        {savedBuild && (
          <details>
            <summary>Technical details</summary>
            <pre>{JSON.stringify(savedBuild, null, 2)}</pre>
          </details>
        )}
        {!savedBuild && buildResult && <p>{buildResult}</p>}
        {activeDraft && (
          <details>
            <summary>Technical details</summary>
            <pre>{JSON.stringify(activeDraft, null, 2)}</pre>
          </details>
        )}
      </section>

      <section className="card">
        <h2>4. Character sheet: vitals, spells, resources</h2>
        {sheetResult && <p>{sheetResult}</p>}
        <h3>Vitals</h3>
        <div className="grid">
          <label htmlFor="vitals-max-hp">Max HP</label>
          <input
            id="vitals-max-hp"
            type="number"
            min={0}
            value={vitals.maxHitPoints}
            onChange={(e) => setVitals((prev) => ({ ...prev, maxHitPoints: toNonNegativeInt(e.target.value) }))}
          />
          <label htmlFor="vitals-current-hp">Current HP</label>
          <input
            id="vitals-current-hp"
            type="number"
            min={0}
            value={vitals.currentHitPoints}
            onChange={(e) => setVitals((prev) => ({ ...prev, currentHitPoints: toNonNegativeInt(e.target.value) }))}
          />
          <label htmlFor="vitals-temp-hp">Temp HP</label>
          <input
            id="vitals-temp-hp"
            type="number"
            min={0}
            value={vitals.tempHitPoints}
            onChange={(e) => setVitals((prev) => ({ ...prev, tempHitPoints: toNonNegativeInt(e.target.value) }))}
          />
          <label htmlFor="vitals-speed">Base move speed</label>
          <input
            id="vitals-speed"
            type="number"
            min={0}
            value={vitals.baseMoveSpeed}
            onChange={(e) => setVitals((prev) => ({ ...prev, baseMoveSpeed: toNonNegativeInt(e.target.value) }))}
          />
          <label htmlFor="vitals-ac">Base armor class</label>
          <input
            id="vitals-ac"
            type="number"
            min={0}
            value={vitals.baseArmorClass}
            onChange={(e) => setVitals((prev) => ({ ...prev, baseArmorClass: toNonNegativeInt(e.target.value) }))}
          />
        </div>
        <div className="row">
          <button onClick={handleSaveVitals} disabled={!currentCharacterId}>
            Save vitals
          </button>
        </div>
        <h3>Spells</h3>
        <div className="row">
          <button
            onClick={() =>
              setSpellEntries((prev) => [...prev, { spellModuleId: '', spellName: '', preparationMode: 'Prepared' }])
            }
            disabled={!currentCharacterId}
          >
            Add spell
          </button>
          <button onClick={handleSaveSpells} disabled={!currentCharacterId}>
            Save spells
          </button>
        </div>
        <ul className="inventory-list">
          {spellEntries.map((entry, index) => (
            <li key={`${entry.spellModuleId}-${index}`}>
              <div className="grid">
                <label htmlFor={`spell-module-${index}`}>Spell module id</label>
                <input
                  id={`spell-module-${index}`}
                  value={entry.spellModuleId}
                  onChange={(e) =>
                    setSpellEntries((prev) =>
                      prev.map((spell, i) => (i === index ? { ...spell, spellModuleId: e.target.value } : spell)),
                    )
                  }
                />
                <label htmlFor={`spell-name-${index}`}>Spell name</label>
                <input
                  id={`spell-name-${index}`}
                  value={entry.spellName}
                  onChange={(e) =>
                    setSpellEntries((prev) =>
                      prev.map((spell, i) => (i === index ? { ...spell, spellName: e.target.value } : spell)),
                    )
                  }
                />
                <label htmlFor={`spell-mode-${index}`}>Preparation mode</label>
                <input
                  id={`spell-mode-${index}`}
                  value={entry.preparationMode}
                  onChange={(e) =>
                    setSpellEntries((prev) =>
                      prev.map((spell, i) => (i === index ? { ...spell, preparationMode: e.target.value } : spell)),
                    )
                  }
                />
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
              <button onClick={() => setResourcePools((prev) => prev.filter((_, i) => i !== index))}>Remove resource</button>
            </li>
          ))}
        </ul>
      </section>

      <section className="card">
        <h2>5. Skills menu and persisted checks</h2>
        <div className="row">
          <label htmlFor="selected-skill">Skill</label>
          <select id="selected-skill" value={selectedSkill} onChange={(e) => setSelectedSkill(e.target.value as SkillName)}>
            {ALL_SKILLS.map((skill) => (
              <option key={skill} value={skill}>
                {skill}
              </option>
            ))}
          </select>
          <label htmlFor="skill-advantage-state">Roll mode</label>
          <select id="skill-advantage-state" value={advantageState} onChange={(e) => setAdvantageState(e.target.value as AdvantageState)}>
            <option value="None">None</option>
            <option value="Advantage">Advantage</option>
            <option value="Disadvantage">Disadvantage</option>
          </select>
          <button onClick={handleRollSkillCheck} disabled={!currentCharacterId}>
            Roll persisted check
          </button>
        </div>
        <p>
          Proficiencies available: {proficiencySlotsAvailable - proficiencySlotsUsed} (used {proficiencySlotsUsed} /{' '}
          {proficiencySlotsAvailable}) | Expertise available: {expertiseSlotsAvailable - expertiseSlotsUsed} (used{' '}
          {expertiseSlotsUsed} / {expertiseSlotsAvailable})
        </p>
        <small>
          Class fixed: {selectedClassOption?.fixedSkillProficiencies.join(', ') || 'None'} | Class choices:{' '}
          {selectedClassOption?.skillChoices.join(', ') || 'None'} | Background fixed:{' '}
          {selectedBackgroundOption?.fixedSkillProficiencies.join(', ') || 'None'}
        </small>
        <div className="skills-grid">
          {ALL_SKILLS.map((skill) => (
            <label key={skill}>
              <select
                value={skillTrainingBySkill[skill]}
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
        {rollResult && (
          <details>
            <summary>Technical details</summary>
            <pre>{rollResult}</pre>
          </details>
        )}
      </section>

      <section className="card">
        <h2>6. Inventory from database (persisted)</h2>
        <div className="row">
          <label htmlFor="catalog-item">Item</label>
          <select id="catalog-item" value={selectedCatalogItemId} onChange={(e) => setSelectedCatalogItemId(e.target.value)}>
            {itemCatalog.length === 0 ? (
              <option value="">No item definitions found in DB</option>
            ) : (
              itemCatalog.map((item) => (
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
          <button onClick={() => void handleAddItemFromCatalog()} disabled={!selectedCatalogItemId || !currentCharacterId}>
            Add to persisted inventory
          </button>
        </div>
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
        {attunementGuidance && (
          <details>
            <summary>Technical details</summary>
            <pre>{attunementGuidance}</pre>
          </details>
        )}
        {inventoryResult && (
          <details>
            <summary>Technical details</summary>
            <pre>{inventoryResult}</pre>
          </details>
        )}
      </section>

      <section className="card">
        <h2>7. Attacks</h2>
        <div className="row">
          <label htmlFor="attack-advantage-state">Roll mode</label>
          <select id="attack-advantage-state" value={attackAdvantageState} onChange={(e) => setAttackAdvantageState(e.target.value as AdvantageState)}>
            <option value="None">None</option>
            <option value="Advantage">Advantage</option>
            <option value="Disadvantage">Disadvantage</option>
          </select>
        </div>
        <ul className="inventory-list">
          {attacks.map((attack) => {
            const key = [
              getDisplayItemName(attack.itemName, attack.itemDefinitionId),
              attack.damageDice,
              attack.weaponAbility,
              attack.attackBonus,
              attack.damageBonus,
            ].join('|')
            return (
              <li key={key}>
                <strong>{getDisplayItemName(attack.itemName, attack.itemDefinitionId)}</strong>
                <span>
                  to hit {attack.toHit >= 0 ? '+' : ''}
                  {attack.toHit} | damage {attack.damageDice}
                  {attack.damageBonus >= 0 ? '+' : ''}
                  {attack.damageBonus} ({attack.abilityName})
                </span>
                <button onClick={() => void handleRollAttack(key, attack)}>Roll attack</button>
                {attackResults[key] && <small>{attackResults[key]}</small>}
              </li>
            )
          })}
        </ul>
      </section>

      <section className="card">
        <h2>8. Custom builder previews</h2>
        <div className="row">
          <button onClick={handlePreviewOrigin}>Preview Origin</button>
          <button onClick={handlePreviewSpecies}>Preview Species</button>
        </div>
        {originPreview && (
          <details>
            <summary>Technical details</summary>
            <pre>{originPreview}</pre>
          </details>
        )}
        {speciesPreview && (
          <details>
            <summary>Technical details</summary>
            <pre>{speciesPreview}</pre>
          </details>
        )}
      </section>
            </>
          )}
        </>
      )}
    </main>
  )
}

export default App
