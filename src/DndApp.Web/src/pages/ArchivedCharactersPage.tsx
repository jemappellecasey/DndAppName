import type { CharacterSummary } from '../types'

type Props = {
  sessionReady: boolean
  archivedCharacters: CharacterSummary[]
  rulesetLabel: (ruleSystem: CharacterSummary['baseRuleSystem']) => string
  onRefreshArchived: () => void
  onBackToCharacters: () => void
  onSelectCharacter: (characterId: string) => void
  onViewCharacter: (characterId: string) => void
  onRestoreCharacter: (characterId: string) => void
  onDeleteCharacter: (characterId: string) => void
}

export default function ArchivedCharactersPage(props: Props) {
  return (
    <section className="card">
      <h2>Archived characters</h2>
      <div className="row">
        <button onClick={props.onRefreshArchived} disabled={!props.sessionReady}>
          Refresh archived
        </button>
        <button onClick={props.onBackToCharacters}>Back to active characters</button>
      </div>
      <ul className="list">
        {props.archivedCharacters.map((character) => (
          <li key={character.characterId}>
            <button onClick={() => props.onSelectCharacter(character.characterId)}>{character.characterName}</button>
            <span className="ruleset-badge">{props.rulesetLabel(character.baseRuleSystem)}</span>
            <button onClick={() => props.onViewCharacter(character.characterId)}>View</button>
            <button onClick={() => props.onRestoreCharacter(character.characterId)}>Restore</button>
            <button onClick={() => props.onDeleteCharacter(character.characterId)}>Delete permanently</button>
          </li>
        ))}
      </ul>
    </section>
  )
}
