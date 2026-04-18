import { type ChangeEvent, useEffect, useMemo, useState } from 'react'
import './App.css'
import {
  addInventoryItem,
  archiveCharacter,
  computePersistedCheck,
  copyToRuleset,
  duplicateCharacter,
  finalizeWizard,
  getAttunementGuidance,
  getCharacterBuild,
  getCharacterHistory,
  getCharacterInventory,
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
  setSessionToken,
  startWizard,
  submitWizardStep,
  upsertCharacterBuild,
} from './api'
import type {
  AbilityName,
  AdvantageState,
  BuildMethod,
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

const DEFAULT_SCORES: Record<AbilityName, number> = {
  Strength: 8,
  Dexterity: 8,
  Constitution: 8,
  Intelligence: 8,
  Wisdom: 8,
  Charisma: 8,
}

const POINT_BUY_COST: Record<number, number> = { 8: 0, 9: 1, 10: 2, 11: 3, 12: 4, 13: 5, 14: 7, 15: 9 }

function abilityModifier(score: number) {
  return Math.floor((score - 10) / 2)
}

function rollAbilityValues(): number[] {
  return ABILITIES.map(() => {
    const rolls = Array.from({ length: 4 }, () => Math.floor(Math.random() * 6) + 1).sort((a, b) => a - b)
    return rolls[1] + rolls[2] + rolls[3]
  }).sort((a, b) => b - a)
}

function App() {
  const [status, setStatus] = useState('Checking API...')
  const [session, setSession] = useState<LocalSession | null>(null)
  const [loginName, setLoginName] = useState('casey')
  const [loginPassword, setLoginPassword] = useState('password123')
  const [isRegisterMode, setIsRegisterMode] = useState(false)
  const [characters, setCharacters] = useState<CharacterSummary[]>([])
  const [selectedCharacterId, setSelectedCharacterId] = useState('')
  const [history, setHistory] = useState<CharacterHistoryEntry[]>([])
  const [error, setError] = useState('')

  const [wizardName, setWizardName] = useState('New Adventurer')
  const [baseRules, setBaseRules] = useState<RuleSystemMode>('Rules2024')
  const [mixedMode, setMixedMode] = useState(false)
  const [overlaySourceOptions, setOverlaySourceOptions] = useState<ContentSourceCatalogItem[]>([])
  const [overlaySources, setOverlaySources] = useState<string[]>([])
  const [activeDraft, setActiveDraft] = useState<CharacterWizardResult | null>(null)
  const [selectionStepName, setSelectionStepName] = useState('class')
  const [selectionModuleId, setSelectionModuleId] = useState('class-fighter')
  const [selectionSourceCode, setSelectionSourceCode] = useState('PHB2024')

  const [classCatalog, setClassCatalog] = useState<ClassCatalogItem[]>([])
  const [moduleCatalog, setModuleCatalog] = useState<ModuleCatalogItem[]>([])
  const [selectedClassModuleId, setSelectedClassModuleId] = useState('')
  const [selectedRaceModuleId, setSelectedRaceModuleId] = useState('')
  const [selectedBackgroundModuleId, setSelectedBackgroundModuleId] = useState('')
  const [secondaryClassModuleId, setSecondaryClassModuleId] = useState('')
  const [multiClassSelections, setMultiClassSelections] = useState<Array<{ moduleId: string; level: number }>>([])
  const [classCatalogResult, setClassCatalogResult] = useState('')

  const [buildMethod, setBuildMethod] = useState<BuildMethod>('PointBuy')
  const [manualScores, setManualScores] = useState<Record<AbilityName, number>>({ ...DEFAULT_SCORES })
  const [pointBuyScores, setPointBuyScores] = useState<Record<AbilityName, number>>({ ...DEFAULT_SCORES })
  const [rolledPool, setRolledPool] = useState<number[]>([])
  const [rollAssignments, setRollAssignments] = useState<Partial<Record<AbilityName, number>>>({})
  const [characterLevel, setCharacterLevel] = useState(1)
  const [proficientSkills, setProficientSkills] = useState<SkillName[]>([])
  const [savedBuild, setSavedBuild] = useState<CharacterBuildData | null>(null)
  const [buildResult, setBuildResult] = useState('')

  const [itemCatalog, setItemCatalog] = useState<ItemCatalogItem[]>([])
  const [selectedCatalogItemId, setSelectedCatalogItemId] = useState('')
  const [inventoryState, setInventoryState] = useState<CharacterInventoryState | null>(null)
  const [inventoryResult, setInventoryResult] = useState('')
  const [attunementGuidance, setAttunementGuidance] = useState('')

  const [selectedSkill, setSelectedSkill] = useState<SkillName>('Stealth')
  const [advantageState, setAdvantageState] = useState<AdvantageState>('None')
  const [hasExpertise, setHasExpertise] = useState(false)
  const [rollResult, setRollResult] = useState('')

  const [originPreview, setOriginPreview] = useState('')
  const [speciesPreview, setSpeciesPreview] = useState('')

  const currentCharacterId = useMemo(
    () => selectedCharacterId || activeDraft?.draft?.characterId || '',
    [selectedCharacterId, activeDraft?.draft?.characterId],
  )

  const classOptions = useMemo(
    () => moduleCatalog.filter((x) => x.moduleType.toLowerCase() === 'class'),
    [moduleCatalog],
  )
  const raceOptions = useMemo(
    () => moduleCatalog.filter((x) => x.moduleType.toLowerCase() === 'race' || x.moduleType.toLowerCase() === 'species'),
    [moduleCatalog],
  )
  const backgroundOptions = useMemo(
    () => moduleCatalog.filter((x) => x.moduleType.toLowerCase() === 'background' || x.moduleType.toLowerCase() === 'origin'),
    [moduleCatalog],
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
    const selected = raceOptions.find((x) => x.moduleId === selectedRaceModuleId)
    return selected?.abilityBonuses ?? {}
  }, [raceOptions, selectedRaceModuleId])

  const backgroundBonuses = useMemo(() => {
    const selected = backgroundOptions.find((x) => x.moduleId === selectedBackgroundModuleId)
    return selected?.abilityBonuses ?? {}
  }, [backgroundOptions, selectedBackgroundModuleId])

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
    () => characterLevel + multiClassSelections.reduce((sum, entry) => sum + entry.level, 0),
    [characterLevel, multiClassSelections],
  )

  const pointBuySpent = useMemo(
    () => ABILITIES.reduce((sum, ability) => sum + POINT_BUY_COST[pointBuyScores[ability]], 0),
    [pointBuyScores],
  )

  const proficiencyBonus = useMemo(() => Math.floor((totalCharacterLevel - 1) / 4) + 2, [totalCharacterLevel])

  useEffect(() => {
    health()
      .then((r) => setStatus(`API online (${r.status})`))
      .catch(() => setStatus('API unreachable (start DndApp.Api on localhost:5080)'))
  }, [])

  async function loadCatalogData(ruleSystem: RuleSystemMode) {
    const [classes, modules, items, guidance, sources] = await Promise.all([
      getClassCatalog(ruleSystem),
      getModuleCatalog({
        baseRuleSystem: ruleSystem,
        mixedMode,
        overlaySources,
        moduleTypes: ['class', 'race', 'species', 'background', 'origin', 'subclass', 'feat', 'spell'],
      }),
      getItemCatalog(),
      getAttunementGuidance(),
      getContentSources(ruleSystem),
    ])
    setClassCatalog(classes)
    setModuleCatalog(modules)
    setItemCatalog(items)
    setAttunementGuidance(JSON.stringify(guidance, null, 2))
    setOverlaySourceOptions(sources)
    setOverlaySources((prev) => prev.filter((code) => sources.some((src) => src.sourceCode === code)))
    if (!selectedClassModuleId && classes.length > 0) {
      setSelectedClassModuleId(classes[0].moduleId)
      setSelectionModuleId(classes[0].moduleId)
      setSelectionSourceCode(classes[0].sourceCode)
    } else if (!selectedClassModuleId) {
      const firstClass = modules.find((x) => x.moduleType.toLowerCase() === 'class')
      if (firstClass) {
        setSelectedClassModuleId(firstClass.moduleId)
        setSelectionModuleId(firstClass.moduleId)
        setSelectionSourceCode(firstClass.sourceCode)
      }
    }
    if (!selectedRaceModuleId) {
      const firstRace = modules.find((x) => x.moduleType.toLowerCase() === 'race' || x.moduleType.toLowerCase() === 'species')
      if (firstRace) {
        setSelectedRaceModuleId(firstRace.moduleId)
      }
    }
    if (!selectedBackgroundModuleId) {
      const firstBackground = modules.find((x) => x.moduleType.toLowerCase() === 'background' || x.moduleType.toLowerCase() === 'origin')
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
      setSelectedClassModuleId(build.classModuleId)
      setCharacterLevel(build.level)
      setProficientSkills(build.proficientSkills)
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
  }

  async function refreshCharacters() {
    const list = await getCharacters(true, true)
    setCharacters(list)
    if (!selectedCharacterId && list.length > 0) {
      setSelectedCharacterId(list[0].characterId)
      await loadPersistedCharacterState(list[0].characterId)
    }
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
      await loadCatalogData(baseRules)
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
        characterName: wizardName,
        baseRuleSystem: baseRules,
        mixedModeEnabled: mixedMode,
        overlaySources: mixedMode ? overlaySources : [],
      })
      setActiveDraft(result)
      if (result.draft?.characterId) {
        setSelectedCharacterId(result.draft.characterId)
      }
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleSubmitStep() {
    if (!activeDraft?.draft) return
    try {
      setError('')
      const result = await submitWizardStep(activeDraft.draft.characterId, selectionStepName, [
        {
          slot: selectionStepName,
          moduleId: selectionModuleId,
          sourceCode: selectionSourceCode,
          compatible2014: true,
          compatible2024: true,
        },
      ])
      setActiveDraft(result)
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleApplySelectedClassToWizard() {
    if (!activeDraft?.draft || !selectedClassModuleId) return
    const selectedClass = classCatalog.find((x) => x.moduleId === selectedClassModuleId)
    const selectedClassModule = classOptions.find((x) => x.moduleId === selectedClassModuleId)
    if (!selectedClass && !selectedClassModule) return
    try {
      const classSelections = [
        {
          slot: 'class',
          moduleId: selectedClassModuleId,
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

      if (selectedRaceModuleId) {
        const race = raceOptions.find((x) => x.moduleId === selectedRaceModuleId)
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

      if (selectedBackgroundModuleId) {
        const background = backgroundOptions.find((x) => x.moduleId === selectedBackgroundModuleId)
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

      setActiveDraft(result)
      setClassCatalogResult(
        `Submitted selections: primary class + ${multiClassSelections.length} multiclass entries, race/background selections.`,
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
    const selectedClass = classCatalog.find((x) => x.moduleId === selectedClassModuleId)
    const selectedClassFromModules = classOptions.find((x) => x.moduleId === selectedClassModuleId)
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
      const secondarySummary = multiClassSelections
        .map((entry) => {
          const option = classOptions.find((x) => x.moduleId === entry.moduleId)
          return option ? `${option.displayName} ${entry.level}` : `${entry.moduleId} ${entry.level}`
        })
        .join(', ')

      const saved = await upsertCharacterBuild(currentCharacterId, {
        characterName: wizardName,
        baseRuleSystem: baseRules,
        buildMethod,
        classModuleId: selectedClassModuleId,
        className: secondarySummary ? `${primaryClassName} (Primary); ${secondarySummary}` : primaryClassName,
        level: totalCharacterLevel,
        proficiencyBonus,
        abilityScores: totalAbilityScores,
        proficientSkills,
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
  }

  function addMultiClassSelection() {
    if (!secondaryClassModuleId) return
    if (secondaryClassModuleId === selectedClassModuleId) return
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

  function toggleProficientSkill(skill: SkillName) {
    if (proficientSkills.includes(skill)) {
      setProficientSkills((prev) => prev.filter((x) => x !== skill))
      return
    }
    setProficientSkills((prev) => [...prev, skill])
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

  function handleRollPoolGenerate() {
    setRolledPool(rollAbilityValues())
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
      const next = await addInventoryItem(currentCharacterId, selectedCatalogItemId)
      setInventoryState(next)
      setInventoryResult(JSON.stringify(next, null, 2))
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleUpdateInventoryItem(
    inventoryItemId: string,
    update: { isEquipped?: boolean; isAttuned?: boolean },
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
        hasExpertise,
      })
      setRollResult(JSON.stringify(result, null, 2))
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
    const prof = proficientSkills.includes(skill) ? proficiencyBonus : 0
    return abilityMod + prof
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
              <input value={loginName} onChange={(e) => setLoginName(e.target.value)} placeholder="Username" />
              <input
                type="password"
                value={loginPassword}
                onChange={(e) => setLoginPassword(e.target.value)}
                placeholder="Password"
              />
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
                Signed in as <strong>{session.userName}</strong> ({session.sessionToken.slice(0, 10)}...)
              </p>
              <button onClick={handleLogout}>Log out</button>
            </div>
          </section>

      <section className="card">
        <h2>2. Character build setup</h2>
        <div className="grid">
          <input value={wizardName} onChange={(e) => setWizardName(e.target.value)} placeholder="Character name" />
          <select value={baseRules} onChange={(e) => void handleBaseRulesChange(e.target.value as RuleSystemMode)}>
            <option value="Rules2024">Rules2024</option>
            <option value="Rules2014">Rules2014</option>
          </select>
          <select value={buildMethod} onChange={(e) => setBuildMethod(e.target.value as BuildMethod)}>
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
          <select multiple value={overlaySources} onChange={handleOverlaySourcesChange} disabled={!mixedMode}>
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
          <input
            type="number"
            min={1}
            max={20}
            value={characterLevel}
            onChange={(e) => setCharacterLevel(Math.max(1, Math.min(20, Number(e.target.value))))}
            placeholder="Character level"
          />
        </div>
        <div className="row">
          <button onClick={() => void loadCatalogData(baseRules)} disabled={!session}>
            Refresh content catalogs
          </button>
        </div>
        <p>
          Total level: {totalCharacterLevel} | Proficiency bonus: +{proficiencyBonus}
        </p>
        <div className="grid">
          <select value={selectedRaceModuleId} onChange={(e) => setSelectedRaceModuleId(e.target.value)}>
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
          <select value={selectedBackgroundModuleId} onChange={(e) => setSelectedBackgroundModuleId(e.target.value)}>
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
          <select value={secondaryClassModuleId} onChange={(e) => setSecondaryClassModuleId(e.target.value)}>
            <option value="">Add multiclass option...</option>
            {classOptions
              .filter((x) => x.moduleId !== selectedClassModuleId)
              .map((item) => (
                <option key={item.moduleId} value={item.moduleId}>
                  {item.displayName} ({item.sourceCode})
                </option>
              ))}
          </select>
          <button onClick={addMultiClassSelection} disabled={!selectedClassModuleId || !secondaryClassModuleId}>
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
                  <input
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
              <button onClick={handleRollPoolGenerate}>Roll 4d6 drop lowest (6 stats)</button>
              <p>{isRollAssignmentComplete ? 'All rolls assigned.' : 'Drag each roll to an ability to assign.'}</p>
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
          <select value={selectedClassModuleId} onChange={(e) => setSelectedClassModuleId(e.target.value)}>
            {classCatalog.length === 0 && classOptions.length === 0 ? (
              <option value="">No class modules found in DB</option>
            ) : (
              <>
                {classCatalog.map((item) => (
                  <option key={item.moduleId} value={item.moduleId}>
                    {item.className} ({item.sourceCode})
                  </option>
                ))}
                {classOptions
                  .filter((item) => !classCatalog.some((legacy) => legacy.moduleId === item.moduleId))
                  .map((item) => (
                    <option key={item.moduleId} value={item.moduleId}>
                      {item.displayName} ({item.sourceCode})
                    </option>
                  ))}
              </>
            )}
          </select>
          <button
            onClick={handleApplySelectedClassToWizard}
            disabled={!activeDraft?.draft || (classCatalog.length === 0 && classOptions.length === 0)}
          >
            Apply selected class to wizard
          </button>
          <button
            onClick={handleSaveBuildToDb}
            disabled={
              !currentCharacterId ||
              (classCatalog.length === 0 && classOptions.length === 0) ||
              (buildMethod === 'Roll' && !isRollAssignmentComplete)
            }
          >
            Save build to persistent model
          </button>
        </div>
        <div className="grid">
          <input value={selectionStepName} onChange={(e) => setSelectionStepName(e.target.value)} placeholder="Step slot" />
          <input value={selectionModuleId} onChange={(e) => setSelectionModuleId(e.target.value)} placeholder="Module id" />
          <input value={selectionSourceCode} onChange={(e) => setSelectionSourceCode(e.target.value)} placeholder="Source code" />
        </div>
        <div className="row">
          <button onClick={handleSubmitStep} disabled={!activeDraft?.draft}>
            Submit Step
          </button>
          <button onClick={handleFinalizeWizard} disabled={!activeDraft?.draft}>
            Finalize
          </button>
        </div>
        {classCatalogResult && <p>{classCatalogResult}</p>}
        {savedBuild && <pre>{JSON.stringify(savedBuild, null, 2)}</pre>}
        {!savedBuild && buildResult && <p>{buildResult}</p>}
        {activeDraft && <pre>{JSON.stringify(activeDraft, null, 2)}</pre>}
      </section>

      <section className="card">
        <h2>4. Your characters</h2>
        <div className="row">
          <button onClick={refreshCharacters} disabled={!session}>
            Refresh
          </button>
          <button onClick={handleCopyRuleset} disabled={!selectedCharacterId}>
            Copy to other ruleset
          </button>
        </div>
        <ul className="list">
          {characters.map((c) => (
            <li key={c.characterId} className={selectedCharacterId === c.characterId ? 'selected' : ''}>
                <button onClick={() => void handleSelectCharacter(c.characterId)}>{c.characterName}</button>
                <span className="ruleset-badge">{c.baseRuleSystem}</span>
                <button onClick={() => void handleArchive(c.characterId)}>Archive</button>
              <button onClick={() => void handleDuplicate(c.characterId)}>Duplicate</button>
              <button onClick={() => void handleLoadHistory(c.characterId)}>History</button>
            </li>
          ))}
        </ul>
        {history.length > 0 && <pre>{JSON.stringify(history, null, 2)}</pre>}
      </section>

      <section className="card">
        <h2>5. Skills menu and persisted checks</h2>
        <div className="row">
          <select value={selectedSkill} onChange={(e) => setSelectedSkill(e.target.value as SkillName)}>
            {ALL_SKILLS.map((skill) => (
              <option key={skill} value={skill}>
                {skill}
              </option>
            ))}
          </select>
          <select value={advantageState} onChange={(e) => setAdvantageState(e.target.value as AdvantageState)}>
            <option value="None">None</option>
            <option value="Advantage">Advantage</option>
            <option value="Disadvantage">Disadvantage</option>
          </select>
          <label>
            <input type="checkbox" checked={hasExpertise} onChange={(e) => setHasExpertise(e.target.checked)} /> Expertise
          </label>
          <button onClick={handleRollSkillCheck} disabled={!currentCharacterId}>
            Roll persisted check
          </button>
        </div>
        <div className="skills-grid">
          {ALL_SKILLS.map((skill) => (
            <label key={skill}>
              <input
                type="checkbox"
                checked={proficientSkills.includes(skill)}
                onChange={() => toggleProficientSkill(skill)}
              />
              {skill} ({SKILL_ABILITY[skill].slice(0, 3)}) mod {skillModifier(skill) >= 0 ? '+' : ''}
              {skillModifier(skill)}
            </label>
          ))}
        </div>
        {rollResult && <pre>{rollResult}</pre>}
      </section>

      <section className="card">
        <h2>6. Inventory from database (persisted)</h2>
        <div className="row">
          <select value={selectedCatalogItemId} onChange={(e) => setSelectedCatalogItemId(e.target.value)}>
            {itemCatalog.length === 0 ? (
              <option value="">No item definitions found in DB</option>
            ) : (
              itemCatalog.map((item) => (
                <option key={item.itemId} value={item.itemId}>
                  {item.itemName} ({item.sourceCode}) {item.requiresAttunement ? '[attunement]' : ''}
                </option>
              ))
            )}
          </select>
          <button onClick={() => void handleAddItemFromCatalog()} disabled={!selectedCatalogItemId || !currentCharacterId}>
            Add to persisted inventory
          </button>
        </div>
        <ul className="inventory-list">
          {(inventoryState?.items ?? []).map((item) => (
            <li key={item.inventoryItemId}>
              <strong>{item.itemName}</strong>
              <span>{item.requiresAttunement ? 'Requires attunement' : 'No attunement'}</span>
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
                <button onClick={() => void handleRemoveItem(item.inventoryItemId)}>Remove</button>
              </div>
            </li>
          ))}
        </ul>
        {attunementGuidance && <pre>{attunementGuidance}</pre>}
        {inventoryResult && <pre>{inventoryResult}</pre>}
      </section>

      <section className="card">
        <h2>7. Custom builder previews</h2>
        <div className="row">
          <button onClick={handlePreviewOrigin}>Preview Origin</button>
          <button onClick={handlePreviewSpecies}>Preview Species</button>
        </div>
        {originPreview && <pre>{originPreview}</pre>}
        {speciesPreview && <pre>{speciesPreview}</pre>}
      </section>
        </>
      )}
    </main>
  )
}

export default App
