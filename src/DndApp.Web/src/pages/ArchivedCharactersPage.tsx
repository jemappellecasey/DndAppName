import { useState, useMemo } from 'react'
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
  const [draggedIndex, setDraggedIndex] = useState<number | null>(null)
  const [orderedCharacters, setOrderedCharacters] = useState<CharacterSummary[]>([])

  // Sort archived characters by updated date (most recent/archive date first) - memoized to sync with prop changes
  useMemo(() => {
    const sorted = [...props.archivedCharacters].sort(
      (a, b) => new Date(b.updatedAtUtc).getTime() - new Date(a.updatedAtUtc).getTime()
    )
    setOrderedCharacters(sorted)
  }, [props.archivedCharacters])

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
      <h2>Archived characters</h2>
      <div className="row">
        <button onClick={props.onRefreshArchived} disabled={!props.sessionReady}>
          Refresh archived
        </button>
        <button onClick={props.onBackToCharacters}>Back to active characters</button>
      </div>
      <small style={{ color: '#666', marginBottom: '8px', display: 'block' }}>
        Characters are sorted by archive date (most recent first). Drag and drop to reorder them.
      </small>
      <ul className="list">
        {orderedCharacters.map((character, index) => (
          <li
            key={character.characterId}
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
            <small>{new Date(character.updatedAtUtc).toLocaleString()}</small>
            <button onClick={() => props.onViewCharacter(character.characterId)}>View</button>
            <button onClick={() => props.onRestoreCharacter(character.characterId)}>Restore</button>
            <button onClick={() => props.onDeleteCharacter(character.characterId)}>Delete permanently</button>
          </li>
        ))}
      </ul>
    </section>
  )
}
