import type { CharacterHistoryEntry, CharacterSummary } from '../types'

type Props = {
  sessionReady: boolean
  selectedCharacterId: string
  characters: CharacterSummary[]
  classSummaryByCharacterId: Record<string, string>
  history: CharacterHistoryEntry[]
  rulesetLabel: (ruleSystem: CharacterSummary['baseRuleSystem']) => string
  onRefresh: () => void
  onCreateNew: () => void
  onOpenArchived: () => void
  onCopyRuleset: () => void
  onSelectCharacter: (characterId: string) => void
  onViewCharacter: (characterId: string) => void
  onArchiveCharacter: (characterId: string) => void
  onDuplicateCharacter: (characterId: string) => void
  onLoadHistory: (characterId: string) => void
}

export default function CharactersPage(props: Props) {
  function mixedModeLabel(character: CharacterSummary) {
    if (!character.mixedModeEnabled) {
      return 'Single ruleset'
    }

    return character.baseRuleSystem === 'Rules2024' ? 'Plus 2014 content' : 'Plus 2024 content'
  }

  return (
    <section className="card">
      <h2>1. Your characters</h2>
      <div className="row">
        <button onClick={props.onRefresh} disabled={!props.sessionReady}>
          Refresh
        </button>
        <button onClick={props.onCreateNew}>
          Create new character
        </button>
        <button onClick={props.onOpenArchived}>Archived characters</button>
        <button onClick={props.onCopyRuleset} disabled={!props.selectedCharacterId}>
          Copy to other ruleset
        </button>
      </div>
      <ul className="list">
        {props.characters.map((character) => (
          <li key={character.characterId} className={props.selectedCharacterId === character.characterId ? 'selected' : ''}>
            <button onClick={() => props.onSelectCharacter(character.characterId)}>{character.characterName}</button>
            <span className="ruleset-badge">{props.rulesetLabel(character.baseRuleSystem)}</span>
            <span className="ruleset-badge">{mixedModeLabel(character)}</span>
            <small>{props.classSummaryByCharacterId[character.characterId] ?? 'Loading class summary...'}</small>
            <button onClick={() => props.onViewCharacter(character.characterId)}>View</button>
            <button onClick={() => props.onArchiveCharacter(character.characterId)}>Archive</button>
            <button onClick={() => props.onDuplicateCharacter(character.characterId)}>Duplicate</button>
          </li>
        ))}
      </ul>
      {props.history.length > 0 && (
        <div className="inventory-list">
          {props.history.slice(0, 5).map((entry, index) => (
            <small key={`${entry.timestampUtc}-${entry.action}-${index}`}>
              {new Date(entry.timestampUtc).toLocaleString()}: {entry.action} - {entry.details}
            </small>
          ))}
        </div>
      )}
    </section>
  )
}
