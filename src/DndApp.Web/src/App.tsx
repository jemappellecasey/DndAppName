import { useEffect, useMemo, useState } from 'react'
import './App.css'
import {
  archiveCharacter,
  computeCheck,
  copyToRuleset,
  duplicateCharacter,
  finalizeWizard,
  getCharacterHistory,
  getCharacters,
  health,
  loginLocal,
  previewOrigin,
  previewSpecies,
  startWizard,
  submitWizardStep,
  updateInventoryItemState,
} from './api'
import type {
  AdvantageState,
  CharacterHistoryEntry,
  CharacterSummary,
  CharacterWizardResult,
  LocalSession,
  RuleSystemMode,
} from './types'

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
  const [selectionStepName, setSelectionStepName] = useState('origin')
  const [selectionModuleId, setSelectionModuleId] = useState('origin-custom-1')
  const [selectionSourceCode, setSelectionSourceCode] = useState('PHB2024')
  const [originPreview, setOriginPreview] = useState<string>('')
  const [speciesPreview, setSpeciesPreview] = useState<string>('')
  const [inventoryResult, setInventoryResult] = useState<string>('')
  const [rollResult, setRollResult] = useState<string>('')
  const [advantageState, setAdvantageState] = useState<AdvantageState>('None')

  const safeCharacterId = useMemo(
    () => selectedCharacterId || '00000000-0000-0000-0000-000000000001',
    [selectedCharacterId],
  )

  useEffect(() => {
    health()
      .then((r) => setStatus(`API online (${r.status})`))
      .catch(() => setStatus('API unreachable (start DndApp.Api on localhost:5080)'))
  }, [])

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

  async function handleInventoryUpdate() {
    const result = await updateInventoryItemState(safeCharacterId)
    setInventoryResult(JSON.stringify(result, null, 2))
  }

  async function handleRoll() {
    const result = await computeCheck(safeCharacterId, advantageState)
    setRollResult(JSON.stringify(result, null, 2))
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
        {session && <p>Session: {session.userName} ({session.sessionToken.slice(0, 10)}...)</p>}
      </section>

      <section className="card">
        <h2>2. Character wizard</h2>
        <div className="grid">
          <input value={wizardName} onChange={(e) => setWizardName(e.target.value)} placeholder="Character name" />
          <select value={baseRules} onChange={(e) => setBaseRules(e.target.value as RuleSystemMode)}>
            <option value="Rules2024">Rules2024</option>
            <option value="Rules2014">Rules2014</option>
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
        </div>
        <button onClick={handleStartWizard} disabled={!session}>Start Wizard Draft</button>
        <div className="grid">
          <input value={selectionStepName} onChange={(e) => setSelectionStepName(e.target.value)} placeholder="Step slot" />
          <input value={selectionModuleId} onChange={(e) => setSelectionModuleId(e.target.value)} placeholder="Module id" />
          <input value={selectionSourceCode} onChange={(e) => setSelectionSourceCode(e.target.value)} placeholder="Source code" />
        </div>
        <div className="row">
          <button onClick={handleSubmitStep} disabled={!activeDraft?.draft}>Submit Step</button>
          <button onClick={handleFinalizeWizard} disabled={!activeDraft?.draft}>Finalize</button>
        </div>
        {activeDraft && <pre>{JSON.stringify(activeDraft, null, 2)}</pre>}
      </section>

      <section className="card">
        <h2>3. Character library</h2>
        <div className="row">
          <button onClick={refreshCharacters} disabled={!session}>Refresh</button>
          <button onClick={handleCopyRuleset} disabled={!selectedCharacterId}>Copy to other ruleset</button>
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
        <h2>4. Custom builder previews</h2>
        <div className="row">
          <button onClick={handlePreviewOrigin}>Preview Origin</button>
          <button onClick={handlePreviewSpecies}>Preview Species</button>
        </div>
        {originPreview && <pre>{originPreview}</pre>}
        {speciesPreview && <pre>{speciesPreview}</pre>}
      </section>

      <section className="card">
        <h2>5. Inventory and attunement</h2>
        <button onClick={handleInventoryUpdate}>Apply item state update</button>
        {inventoryResult && <pre>{inventoryResult}</pre>}
      </section>

      <section className="card">
        <h2>6. Roll checks</h2>
        <select value={advantageState} onChange={(e) => setAdvantageState(e.target.value as AdvantageState)}>
          <option value="None">None</option>
          <option value="Advantage">Advantage</option>
          <option value="Disadvantage">Disadvantage</option>
        </select>
        <button onClick={handleRoll}>Roll Stealth Check</button>
        {rollResult && <pre>{rollResult}</pre>}
      </section>
    </main>
  )
}

export default App
