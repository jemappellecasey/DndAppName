import { useEffect, useMemo, useState } from 'react'
import './App.css'
import {
  archiveCharacter,
  computeCheck,
  copyToRuleset,
  duplicateCharacter,
  finalizeWizard,
  getAttunementGuidance,
  getCharacterHistory,
  getCharacters,
  getClassCatalog,
  getItemCatalog,
  health,
  loginLocal,
  previewOrigin,
  previewSpecies,
  startWizard,
  submitWizardStep,
  updateInventoryItemState,
} from './api'
import type {
  AbilityName,
  AdvantageState,
  BuildMethod,
  CharacterHistoryEntry,
  CharacterItemState,
  CharacterSummary,
  CharacterWizardResult,
  ClassCatalogItem,
  ItemCatalogItem,
  ItemEffectInput,
  LocalSession,
  RuleSystemMode,
  SkillName,
} from './types'

const ABILITIES: AbilityName[] = [
  'Strength',
  'Dexterity',
  'Constitution',
  'Intelligence',
  'Wisdom',
  'Charisma',
]

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

const POINT_BUY_COST: Record<number, number> = {
  8: 0,
  9: 1,
  10: 2,
  11: 3,
  12: 4,
  13: 5,
  14: 7,
  15: 9,
}

const SAFE_FALLBACK_CHARACTER_ID = '00000000-0000-0000-0000-000000000001'

function abilityModifier(score: number) {
  return Math.floor((score - 10) / 2)
}

function parseJsonObject(value: string): Record<string, unknown> | null {
  try {
    const parsed = JSON.parse(value)
    if (typeof parsed !== 'object' || parsed === null) {
      return null
    }
    return parsed as Record<string, unknown>
  } catch {
    return null
  }
}

function normalizeEffectType(rawType: string): ItemEffectInput['type'] {
  const normalized = rawType.trim().toLowerCase()
  if (normalized.includes('ac')) return 'AcBonus'
  if (normalized.includes('move') || normalized.includes('speed')) return 'MoveSpeedBonus'
  if (normalized.includes('saving')) return 'SavingThrowBonus'
  if (normalized.includes('spell')) return 'GrantSpell'
  return 'AbilityCheckBonus'
}

function mapCatalogEffects(item: ItemCatalogItem): ItemEffectInput[] {
  return item.effects.map((effect) => {
    const payload = parseJsonObject(effect.effectPayloadJson)
    const numericValue =
      typeof payload?.numericValue === 'number'
        ? payload.numericValue
        : typeof payload?.value === 'number'
          ? payload.value
          : typeof payload?.bonus === 'number'
            ? payload.bonus
            : typeof payload?.modifier === 'number'
              ? payload.modifier
              : 0
    const target =
      typeof payload?.target === 'string'
        ? payload.target
        : typeof payload?.ability === 'string'
          ? payload.ability
          : typeof payload?.skill === 'string'
            ? payload.skill
            : null
    const grantedSpell =
      typeof payload?.grantedSpell === 'string'
        ? payload.grantedSpell
        : typeof payload?.spellName === 'string'
          ? payload.spellName
          : null
    const description =
      typeof payload?.description === 'string' ? payload.description : `${effect.effectType} (${item.itemName})`

    return {
      type: normalizeEffectType(effect.effectType),
      target,
      numericValue,
      grantedSpell,
      description,
    }
  })
}

function rollAbilitySet(): Record<AbilityName, number> {
  const scores = ABILITIES.map(() => {
    const rolls = Array.from({ length: 4 }, () => Math.floor(Math.random() * 6) + 1).sort((a, b) => a - b)
    return rolls[1] + rolls[2] + rolls[3]
  }).sort((a, b) => b - a)

  return {
    Strength: scores[0],
    Dexterity: scores[1],
    Constitution: scores[2],
    Intelligence: scores[3],
    Wisdom: scores[4],
    Charisma: scores[5],
  }
}

function App() {
  const [status, setStatus] = useState<string>('Checking API...')
  const [session, setSession] = useState<LocalSession | null>(null)
  const [loginName, setLoginName] = useState('casey')
  const [characters, setCharacters] = useState<CharacterSummary[]>([])
  const [selectedCharacterId, setSelectedCharacterId] = useState<string>('')
  const [history, setHistory] = useState<CharacterHistoryEntry[]>([])
  const [error, setError] = useState<string>('')

  const [wizardName, setWizardName] = useState('New Adventurer')
  const [baseRules, setBaseRules] = useState<RuleSystemMode>('Rules2024')
  const [mixedMode, setMixedMode] = useState(false)
  const [overlaySources, setOverlaySources] = useState('PHB2014')
  const [activeDraft, setActiveDraft] = useState<CharacterWizardResult | null>(null)
  const [selectionStepName, setSelectionStepName] = useState('class')
  const [selectionModuleId, setSelectionModuleId] = useState('class-fighter')
  const [selectionSourceCode, setSelectionSourceCode] = useState('PHB2024')

  const [classCatalog, setClassCatalog] = useState<ClassCatalogItem[]>([])
  const [selectedClassModuleId, setSelectedClassModuleId] = useState<string>('')
  const [classCatalogResult, setClassCatalogResult] = useState<string>('')

  const [buildMethod, setBuildMethod] = useState<BuildMethod>('PointBuy')
  const [manualScores, setManualScores] = useState<Record<AbilityName, number>>({ ...DEFAULT_SCORES })
  const [pointBuyScores, setPointBuyScores] = useState<Record<AbilityName, number>>({ ...DEFAULT_SCORES })
  const [rolledScores, setRolledScores] = useState<Record<AbilityName, number> | null>(null)
  const [proficiencyBonus, setProficiencyBonus] = useState(2)
  const [baseArmorClass, setBaseArmorClass] = useState(10)
  const [baseMoveSpeed, setBaseMoveSpeed] = useState(30)

  const [itemCatalog, setItemCatalog] = useState<ItemCatalogItem[]>([])
  const [selectedCatalogItemId, setSelectedCatalogItemId] = useState<string>('')
  const [inventoryItems, setInventoryItems] = useState<CharacterItemState[]>([])
  const [inventoryResult, setInventoryResult] = useState<string>('')
  const [attunementGuidance, setAttunementGuidance] = useState<string>('')

  const [selectedSkill, setSelectedSkill] = useState<SkillName>('Stealth')
  const [advantageState, setAdvantageState] = useState<AdvantageState>('None')
  const [hasExpertise, setHasExpertise] = useState(false)
  const [proficientSkills, setProficientSkills] = useState<SkillName[]>([])
  const [rollResult, setRollResult] = useState<string>('')

  const [originPreview, setOriginPreview] = useState<string>('')
  const [speciesPreview, setSpeciesPreview] = useState<string>('')

  const safeCharacterId = useMemo(
    () => selectedCharacterId || activeDraft?.draft?.characterId || SAFE_FALLBACK_CHARACTER_ID,
    [selectedCharacterId, activeDraft?.draft?.characterId],
  )

  const activeAbilityScores = useMemo<Record<AbilityName, number>>(() => {
    if (buildMethod === 'Manual') return manualScores
    if (buildMethod === 'Roll') return rolledScores ?? DEFAULT_SCORES
    return pointBuyScores
  }, [buildMethod, manualScores, rolledScores, pointBuyScores])

  const pointBuySpent = useMemo(
    () => ABILITIES.reduce((sum, ability) => sum + POINT_BUY_COST[pointBuyScores[ability]], 0),
    [pointBuyScores],
  )

  useEffect(() => {
    health()
      .then((r) => setStatus(`API online (${r.status})`))
      .catch(() => setStatus('API unreachable (start DndApp.Api on localhost:5080)'))
  }, [])

  async function loadCatalogData(ruleSystem: RuleSystemMode) {
    try {
      const [classes, items, guidance] = await Promise.all([
        getClassCatalog(ruleSystem),
        getItemCatalog(),
        getAttunementGuidance(),
      ])
      setClassCatalog(classes)
      setItemCatalog(items)
      setAttunementGuidance(JSON.stringify(guidance, null, 2))

      if (classes.length > 0) {
        setSelectedClassModuleId((current) => current || classes[0].moduleId)
        setSelectionModuleId((current) => (current ? current : classes[0].moduleId))
        setSelectionSourceCode((current) => (current ? current : classes[0].sourceCode))
      }
      if (items.length > 0) {
        setSelectedCatalogItemId((current) => current || items[0].itemId)
      }
    } catch (e) {
      setClassCatalogResult(String(e))
    }
  }

  function skillModifier(skill: SkillName) {
    const ability = SKILL_ABILITY[skill]
    const abilityMod = abilityModifier(activeAbilityScores[ability])
    const prof = proficientSkills.includes(skill) ? proficiencyBonus : 0
    return abilityMod + prof
  }

  function buildBaseStatsPayload() {
    const savingThrows: Record<string, number> = {}
    for (const ability of ABILITIES) {
      savingThrows[ability] = abilityModifier(activeAbilityScores[ability])
    }

    const abilityChecks: Record<string, number> = {}
    for (const skill of ALL_SKILLS) {
      abilityChecks[skill] = skillModifier(skill)
    }

    return {
      armorClass: baseArmorClass,
      moveSpeed: baseMoveSpeed,
      savingThrows,
      abilityChecks,
      availableSpells: [] as string[],
    }
  }

  async function refreshCharacters() {
    const list = await getCharacters(true)
    setCharacters(list)
    if (!selectedCharacterId && list.length > 0) {
      setSelectedCharacterId(list[0].characterId)
    }
  }

  async function handleLogin() {
    try {
      setError('')
      const result = await loginLocal(loginName)
      setSession(result)
      await refreshCharacters()
      await loadCatalogData(baseRules)
    } catch (e) {
      setError(String(e))
    }
  }

  function handleBaseRulesChange(ruleSystem: RuleSystemMode) {
    setBaseRules(ruleSystem)
    void loadCatalogData(ruleSystem)
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
        overlaySources: mixedMode
          ? overlaySources
              .split(',')
              .map((x) => x.trim())
              .filter(Boolean)
          : [],
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
    if (!selectedClass) return

    setSelectionStepName('class')
    setSelectionModuleId(selectedClass.moduleId)
    setSelectionSourceCode(selectedClass.sourceCode)
    try {
      const result = await submitWizardStep(activeDraft.draft.characterId, 'class', [
        {
          slot: 'class',
          moduleId: selectedClass.moduleId,
          sourceCode: selectedClass.sourceCode,
          compatible2014: true,
          compatible2024: true,
        },
      ])
      setActiveDraft(result)
      setClassCatalogResult(`Submitted class selection: ${selectedClass.className}`)
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

  async function handlePreviewOrigin() {
    const result = await previewOrigin({ name: 'Wanderer-Born', mode: 'GuidedCustom' })
    setOriginPreview(JSON.stringify(result, null, 2))
  }

  async function handlePreviewSpecies() {
    const result = await previewSpecies({ name: 'Stormkin', mode: 'GuidedCustom' })
    setSpeciesPreview(JSON.stringify(result, null, 2))
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

  function handleAddItemFromCatalog() {
    const item = itemCatalog.find((x) => x.itemId === selectedCatalogItemId)
    if (!item) return
    if (inventoryItems.some((x) => x.itemId === item.itemId)) return

    const mapped: CharacterItemState = {
      itemId: item.itemId,
      itemName: item.itemName,
      requiresAttunement: item.requiresAttunement,
      isEquipped: false,
      isAttuned: false,
      effects: mapCatalogEffects(item),
    }
    setInventoryItems((prev) => [...prev, mapped])
  }

  function handleRemoveItem(itemId: string) {
    setInventoryItems((prev) => prev.filter((x) => x.itemId !== itemId))
  }

  async function handleUpdateInventoryItem(itemId: string, update: { isEquipped?: boolean; isAttuned?: boolean }) {
    try {
      const result = await updateInventoryItemState(safeCharacterId, {
        baseStats: buildBaseStatsPayload(),
        items: inventoryItems,
        itemId,
        ...update,
      })
      setInventoryItems(result.items)
      setInventoryResult(JSON.stringify(result, null, 2))
    } catch (e) {
      setError(String(e))
    }
  }

  async function handleRollSkillCheck() {
    try {
      const modifier = skillModifier(selectedSkill)
      const isProficient = proficientSkills.includes(selectedSkill)
      const result = await computeCheck(safeCharacterId, {
        skillName: selectedSkill,
        abilityModifier: modifier - (isProficient ? proficiencyBonus : 0),
        proficiencyBonus,
        isProficient,
        hasExpertise: isProficient && hasExpertise,
        additionalModifier: 0,
        advantageState,
        rollDice: true,
      })
      setRollResult(JSON.stringify(result, null, 2))
    } catch (e) {
      setError(String(e))
    }
  }

  return (
    <main className="layout">
      <header>
        <h1>DndAppName Frontend</h1>
        <p>{status}</p>
      </header>

      {error && <p className="error">{error}</p>}

      <section className="card">
        <h2>1. Local session</h2>
        <div className="row">
          <input value={loginName} onChange={(e) => setLoginName(e.target.value)} placeholder="Username" />
          <button onClick={handleLogin}>Start Session</button>
        </div>
        {session && (
          <p>
            Session: {session.userName} ({session.sessionToken.slice(0, 10)}...)
          </p>
        )}
      </section>

      <section className="card">
        <h2>2. Character build setup</h2>
        <div className="grid">
          <input value={wizardName} onChange={(e) => setWizardName(e.target.value)} placeholder="Character name" />
          <select value={baseRules} onChange={(e) => handleBaseRulesChange(e.target.value as RuleSystemMode)}>
            <option value="Rules2024">Rules2024</option>
            <option value="Rules2014">Rules2014</option>
          </select>
          <select value={buildMethod} onChange={(e) => setBuildMethod(e.target.value as BuildMethod)}>
            <option value="PointBuy">Point buy</option>
            <option value="Manual">Manual entry</option>
            <option value="Roll">Roll</option>
          </select>
          <label>
            <input type="checkbox" checked={mixedMode} onChange={(e) => setMixedMode(e.target.checked)} /> Mixed mode
          </label>
          <input
            value={overlaySources}
            onChange={(e) => setOverlaySources(e.target.value)}
            placeholder="Overlay sources (comma separated)"
            disabled={!mixedMode}
          />
          <input
            type="number"
            min={1}
            max={8}
            value={proficiencyBonus}
            onChange={(e) => setProficiencyBonus(Number(e.target.value))}
            placeholder="Proficiency bonus"
          />
          <input
            type="number"
            min={1}
            value={baseArmorClass}
            onChange={(e) => setBaseArmorClass(Number(e.target.value))}
            placeholder="Base AC"
          />
          <input
            type="number"
            min={0}
            value={baseMoveSpeed}
            onChange={(e) => setBaseMoveSpeed(Number(e.target.value))}
            placeholder="Base speed"
          />
        </div>

        {buildMethod === 'PointBuy' && (
          <p>
            Point-buy spent: {pointBuySpent}/27
          </p>
        )}
        {buildMethod === 'Roll' && (
          <button onClick={() => setRolledScores(rollAbilitySet())}>Roll 4d6 drop lowest (6 stats)</button>
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
                <strong>{(rolledScores ?? DEFAULT_SCORES)[ability]}</strong>
              )}
              <small>mod {abilityModifier(activeAbilityScores[ability]) >= 0 ? '+' : ''}{abilityModifier(activeAbilityScores[ability])}</small>
            </label>
          ))}
        </div>
      </section>

      <section className="card">
        <h2>3. Character wizard and class module</h2>
        <button onClick={handleStartWizard} disabled={!session}>
          Start Wizard Draft
        </button>
        <div className="row">
          <select value={selectedClassModuleId} onChange={(e) => setSelectedClassModuleId(e.target.value)}>
            {classCatalog.length === 0 ? (
              <option value="">No class modules found in DB</option>
            ) : (
              classCatalog.map((item) => (
                <option key={item.moduleId} value={item.moduleId}>
                  {item.className} ({item.sourceCode})
                </option>
              ))
            )}
          </select>
          <button onClick={handleApplySelectedClassToWizard} disabled={!activeDraft?.draft || classCatalog.length === 0}>
            Apply selected class to wizard
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
        {activeDraft && <pre>{JSON.stringify(activeDraft, null, 2)}</pre>}
      </section>

      <section className="card">
        <h2>4. Character library</h2>
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
              <button onClick={() => setSelectedCharacterId(c.characterId)}>{c.characterName}</button>
              <span>{c.baseRuleSystem}</span>
              <button onClick={() => handleArchive(c.characterId)}>Archive</button>
              <button onClick={() => handleDuplicate(c.characterId)}>Duplicate</button>
              <button onClick={() => handleLoadHistory(c.characterId)}>History</button>
            </li>
          ))}
        </ul>
        {history.length > 0 && <pre>{JSON.stringify(history, null, 2)}</pre>}
      </section>

      <section className="card">
        <h2>5. Skills menu and check rolling</h2>
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
            (selected skill)
          </label>
          <button onClick={handleRollSkillCheck}>Roll check</button>
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
        <h2>6. Inventory from database</h2>
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
          <button onClick={handleAddItemFromCatalog} disabled={!selectedCatalogItemId}>
            Add to inventory
          </button>
        </div>
        <ul className="inventory-list">
          {inventoryItems.map((item) => (
            <li key={item.itemId}>
              <strong>{item.itemName}</strong>
              <span>{item.requiresAttunement ? 'Requires attunement' : 'No attunement'}</span>
              <div className="row">
                <button onClick={() => handleUpdateInventoryItem(item.itemId, { isEquipped: !item.isEquipped })}>
                  {item.isEquipped ? 'Unequip' : 'Equip'}
                </button>
                {item.requiresAttunement && (
                  <button onClick={() => handleUpdateInventoryItem(item.itemId, { isAttuned: !item.isAttuned })}>
                    {item.isAttuned ? 'Unattune' : 'Attune'}
                  </button>
                )}
                <button onClick={() => handleRemoveItem(item.itemId)}>Remove</button>
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
    </main>
  )
}

export default App
