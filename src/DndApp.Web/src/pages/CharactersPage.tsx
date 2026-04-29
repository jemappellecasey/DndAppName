import { useState } from 'react'
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
  const [draggedIndex, setDraggedIndex] = useState<number | null>(null)
  const [orderedCharacters, setOrderedCharacters] = useState<CharacterSummary[]>(props.characters)

  function mixedModeLabel(character: CharacterSummary) {
    if (!character.mixedModeEnabled) {
      return 'Single ruleset'
    }

    return character.baseRuleSystem === 'Rules2024' ? 'Plus 2014 content' : 'Plus 2024 content'
  }

  function handleDragStart(index: number) {
    setDraggedIndex(index)
  }

  function handleDragOver(e: React.DragEvent) {
    e.preventDefault()
  }

  function handleDrop(targetIndex: number) {
    if (draggedIndex === null || draggedIndex === targetIndex) {
      setDraggedIndex(null)
      return
    }

    const newOrder = [...orderedCharacters]
    const [draggedChar] = newOrder.splice(draggedIndex, 1)
    newOrder.splice(targetIndex, 0, draggedChar)
    setOrderedCharacters(newOrder)
    setDraggedIndex(null)
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
      <small style={{ color: '#666', marginBottom: '8px', display: 'block' }}>
        Drag and drop characters to reorder them
      </small>
      <ul className="list">
        {orderedCharacters.map((character, index) => (
          <li
            key={character.characterId}
            className={props.selectedCharacterId === character.characterId ? 'selected' : ''}
            draggable
            onDragStart={() => handleDragStart(index)}
            onDragOver={handleDragOver}
            onDrop={() => handleDrop(index)}
            style={{
              cursor: 'grab',
              opacity: draggedIndex === index ? 0.5 : 1,
              transition: 'opacity 0.2s',
            }}
          >
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
