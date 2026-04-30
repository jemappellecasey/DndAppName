import { useState, useEffect } from 'react'
import type { AbilityName } from '../types'

interface PendingChoice {
  level: number
  choiceType: 'ASI' | 'Feat'
}

interface LevelUpModalProps {
  isOpen: boolean
  pendingChoices: PendingChoice[]
  onSaveChoice: (level: number, choiceType: 'ASI' | 'Feat', chosenAbility?: string, chosenFeatId?: string) => Promise<void>
  onConfirmChoices: (level: number) => Promise<void>
  onClose: () => void
}

const ABILITIES: AbilityName[] = ['Strength', 'Dexterity', 'Constitution', 'Intelligence', 'Wisdom', 'Charisma']

export default function LevelUpModal({
  isOpen,
  pendingChoices,
  onSaveChoice,
  onConfirmChoices,
  onClose,
}: LevelUpModalProps) {
  const [currentChoiceIndex, setCurrentChoiceIndex] = useState(0)
  const [selectedAbility, setSelectedAbility] = useState<AbilityName | null>(null)
  const [selectedFeatId, setSelectedFeatId] = useState('')
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState('')

  const currentChoice = pendingChoices[currentChoiceIndex]

  useEffect(() => {
    if (isOpen) {
      setCurrentChoiceIndex(0)
      setSelectedAbility(null)
      setSelectedFeatId('')
      setError('')
    }
  }, [isOpen])

  if (!isOpen || !currentChoice) {
    return null
  }

  const handleSaveAndNext = async () => {
    if (currentChoice.choiceType === 'ASI' && !selectedAbility) {
      setError('Please select an ability')
      return
    }
    if (currentChoice.choiceType === 'Feat' && !selectedFeatId) {
      setError('Please select a feat')
      return
    }

    setIsSaving(true)
    setError('')
    try {
      await onSaveChoice(currentChoice.level, currentChoice.choiceType, selectedAbility || undefined, selectedFeatId || undefined)

      if (currentChoiceIndex < pendingChoices.length - 1) {
        setCurrentChoiceIndex(currentChoiceIndex + 1)
        setSelectedAbility(null)
        setSelectedFeatId('')
      } else {
        // All choices made for this level, confirm them
        await onConfirmChoices(currentChoice.level)
        onClose()
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save choice')
    } finally {
      setIsSaving(false)
    }
  }

  const handleSkip = async () => {
    if (currentChoiceIndex < pendingChoices.length - 1) {
      setCurrentChoiceIndex(currentChoiceIndex + 1)
      setSelectedAbility(null)
      setSelectedFeatId('')
      setError('')
    } else {
      onClose()
    }
  }

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl p-8 max-w-md w-full mx-4">
        <h2 className="text-2xl font-bold mb-2">Level Up!</h2>
        <p className="text-gray-600 mb-6">Level {currentChoice.level}</p>

        <div className="mb-6">
          <h3 className="text-lg font-semibold mb-4">
            {currentChoice.choiceType === 'ASI' ? 'Ability Score Improvement' : 'Choose a Feat'}
          </h3>

          {currentChoice.choiceType === 'ASI' ? (
            <div className="grid grid-cols-2 gap-3">
              {ABILITIES.map((ability) => (
                <button
                  key={ability}
                  onClick={() => setSelectedAbility(ability)}
                  className={`p-3 rounded-lg border-2 transition-colors ${
                    selectedAbility === ability
                      ? 'border-blue-500 bg-blue-50'
                      : 'border-gray-300 bg-white hover:border-blue-300'
                  }`}
                >
                  <div className="font-semibold text-sm">{ability}</div>
                  <div className="text-xs text-gray-600">+1</div>
                </button>
              ))}
            </div>
          ) : (
            <div className="space-y-2">
              <p className="text-sm text-gray-600 mb-3">Feat selection coming soon. This is a placeholder.</p>
              <input
                type="text"
                placeholder="Feat ID (placeholder)"
                value={selectedFeatId}
                onChange={(e) => setSelectedFeatId(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                disabled
              />
              <div className="flex flex-wrap gap-2">
                {['Alert', 'Athlete', 'Charger', 'Defensive Duelist'].map((feat) => (
                  <button
                    key={feat}
                    onClick={() => setSelectedFeatId(feat)}
                    className={`px-3 py-1 rounded text-sm transition-colors ${
                      selectedFeatId === feat
                        ? 'bg-blue-500 text-white'
                        : 'bg-gray-200 text-gray-800 hover:bg-gray-300'
                    }`}
                  >
                    {feat}
                  </button>
                ))}
              </div>
            </div>
          )}
        </div>

        {error && <div className="mb-4 p-3 bg-red-50 border border-red-200 text-red-700 rounded-lg text-sm">{error}</div>}

        <div className="flex gap-3">
          <button
            onClick={handleSkip}
            className="flex-1 px-4 py-2 bg-gray-300 text-gray-800 rounded-lg hover:bg-gray-400 transition-colors"
            disabled={isSaving}
          >
            Skip
          </button>
          <button
            onClick={handleSaveAndNext}
            className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:bg-blue-400"
            disabled={isSaving}
          >
            {isSaving ? 'Saving...' : currentChoiceIndex === pendingChoices.length - 1 ? 'Confirm' : 'Next'}
          </button>
        </div>

        <div className="mt-4 text-sm text-gray-500 text-center">
          {currentChoiceIndex + 1} of {pendingChoices.length} choices
        </div>
      </div>
    </div>
  )
}
