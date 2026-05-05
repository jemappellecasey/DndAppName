import { useEffect, useState } from 'react'
import type { ItemCatalogItem, CharacterInventoryItemData } from '../types'

interface EquipmentSelectionModalProps {
  isOpen: boolean
  onClose: () => void
  onConfirm: (selectedItems: CharacterInventoryItemData[]) => void
  availableItems: ItemCatalogItem[]
  startingGold: number
  equipmentMode: 'package' | 'gold-only'
}

interface SelectedItem {
  itemId: string
  quantity: number
}

export default function EquipmentSelectionModal({
  isOpen,
  onClose,
  onConfirm,
  availableItems,
  startingGold,
  equipmentMode,
}: EquipmentSelectionModalProps) {
  const [selectedItems, setSelectedItems] = useState<SelectedItem[]>([])
  const [remainingGold, setRemainingGold] = useState(startingGold)
  const [searchTerm, setSearchTerm] = useState('')
  const [filterType, setFilterType] = useState<string>('all')

  // Get unique item types
  const itemTypes = ['all', ...new Set(availableItems.map((item) => item.itemType))]

  // Filter items based on search and type
  const filteredItems = availableItems.filter((item) => {
    const matchesSearch = item.itemName.toLowerCase().includes(searchTerm.toLowerCase())
    const matchesType = filterType === 'all' || item.itemType === filterType
    const isAffordable = item.goldValue <= remainingGold
    return matchesSearch && matchesType && isAffordable
  })

  // Calculate total weight and cost
  const totalWeight = selectedItems.reduce((sum, selected) => {
    const item = availableItems.find((i) => i.itemId === selected.itemId)
    return sum + (item?.weight ?? 0) * selected.quantity
  }, 0)

  const totalCost = selectedItems.reduce((sum, selected) => {
    const item = availableItems.find((i) => i.itemId === selected.itemId)
    return sum + (item?.goldValue ?? 0) * selected.quantity
  }, 0)

  // Update remaining gold when selected items change
  useEffect(() => {
    setRemainingGold(startingGold - totalCost)
  }, [startingGold, totalCost])

  const handleAddItem = (item: ItemCatalogItem) => {
    const existing = selectedItems.find((s) => s.itemId === item.itemId)
    if (existing) {
      setSelectedItems(
        selectedItems.map((s) =>
          s.itemId === item.itemId ? { ...s, quantity: s.quantity + 1 } : s
        )
      )
    } else {
      setSelectedItems([...selectedItems, { itemId: item.itemId, quantity: 1 }])
    }
  }

  const handleRemoveItem = (itemId: string) => {
    setSelectedItems(selectedItems.filter((s) => s.itemId !== itemId))
  }

  const handleQuantityChange = (itemId: string, quantity: number) => {
    if (quantity <= 0) {
      handleRemoveItem(itemId)
    } else {
      setSelectedItems(
        selectedItems.map((s) => (s.itemId === itemId ? { ...s, quantity } : s))
      )
    }
  }

  const handleConfirm = () => {
    const items: CharacterInventoryItemData[] = selectedItems.map((selected) => {
      const item = availableItems.find((i) => i.itemId === selected.itemId)!
      return {
        inventoryItemId: `inv-${Date.now()}-${Math.random()}`,
        itemDefinitionId: item.itemId,
        itemName: item.itemName,
        itemType: item.itemType,
        goldValue: item.goldValue,
        weight: item.weight,
        quantity: selected.quantity,
        requiresAttunement: item.requiresAttunement,
        isEquipped: false,
        isAttuned: false,
        isWeapon: item.isWeapon,
        damageDice: item.damageDice,
        weaponAbility: item.weaponAbility,
        attackBonus: item.attackBonus,
        damageBonus: item.damageBonus,
      }
    })
    onConfirm(items)
    onClose()
  }

  if (!isOpen) return null

  return (
    <div className="modal-overlay">
      <div className="modal-content" style={{ maxWidth: '900px', maxHeight: '90vh' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
          <h2>Select Starting Equipment</h2>
          <button onClick={onClose} style={{ background: 'none', border: 'none', fontSize: '24px', cursor: 'pointer' }}>
            ✕
          </button>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px', marginBottom: '16px' }}>
          <div>
            <label>Search items</label>
            <input
              type="text"
              placeholder="Search by name..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              style={{ width: '100%' }}
            />
          </div>
          <div>
            <label>Filter by type</label>
            <select value={filterType} onChange={(e) => setFilterType(e.target.value)} style={{ width: '100%' }}>
              {itemTypes.map((type) => (
                <option key={type} value={type}>
                  {type === 'all' ? 'All types' : type}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px', marginBottom: '16px', minHeight: '300px' }}>
          {/* Available Items */}
          <div style={{ border: '1px solid #ccc', borderRadius: '4px', padding: '12px', overflow: 'auto', maxHeight: '400px' }}>
            <h3>Available Items</h3>
            {filteredItems.length === 0 ? (
              <p style={{ color: '#666' }}>No items available</p>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                {filteredItems.map((item) => (
                  <div
                    key={item.itemId}
                    style={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'center',
                      padding: '8px',
                      border: '1px solid #ddd',
                      borderRadius: '4px',
                      backgroundColor: '#f9f9f9',
                    }}
                  >
                    <div style={{ flex: 1 }}>
                      <div style={{ fontWeight: 'bold' }}>{item.itemName}</div>
                      <small style={{ color: '#666' }}>
                        {item.goldValue} gp | {item.weight} lb
                      </small>
                    </div>
                    <button onClick={() => handleAddItem(item)} style={{ padding: '4px 8px', cursor: 'pointer' }}>
                      +
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Selected Items */}
          <div style={{ border: '1px solid #ccc', borderRadius: '4px', padding: '12px', overflow: 'auto', maxHeight: '400px' }}>
            <h3>Selected Items ({selectedItems.length})</h3>
            {selectedItems.length === 0 ? (
              <p style={{ color: '#666' }}>No items selected</p>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                {selectedItems.map((selected) => {
                  const item = availableItems.find((i) => i.itemId === selected.itemId)
                  if (!item) return null
                  return (
                    <div
                      key={item.itemId}
                      style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                        padding: '8px',
                        border: '1px solid #ddd',
                        borderRadius: '4px',
                        backgroundColor: '#e8f5e9',
                      }}
                    >
                      <div style={{ flex: 1 }}>
                        <div style={{ fontWeight: 'bold' }}>{item.itemName}</div>
                        <small style={{ color: '#666' }}>
                          {item.goldValue} gp × {selected.quantity}
                        </small>
                      </div>
                      <div style={{ display: 'flex', gap: '4px', alignItems: 'center' }}>
                        <input
                          type="number"
                          min="1"
                          value={selected.quantity}
                          onChange={(e) => handleQuantityChange(item.itemId, parseInt(e.target.value) || 1)}
                          style={{ width: '40px' }}
                        />
                        <button onClick={() => handleRemoveItem(item.itemId)} style={{ padding: '4px 8px', cursor: 'pointer' }}>
                          ✕
                        </button>
                      </div>
                    </div>
                  )
                })}
              </div>
            )}
          </div>
        </div>

        {/* Summary */}
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px', marginBottom: '16px', padding: '12px', backgroundColor: '#f5f5f5', borderRadius: '4px' }}>
          <div>
            <strong>Equipment Summary</strong>
            <div style={{ marginTop: '8px', fontSize: '14px' }}>
              <div>Items selected: {selectedItems.length}</div>
              <div>Total weight: {totalWeight} lbs</div>
              <div>Total cost: {totalCost} gp</div>
            </div>
          </div>
          <div>
            <strong>Budget</strong>
            <div style={{ marginTop: '8px', fontSize: '14px' }}>
              <div>Starting gold: {startingGold} gp</div>
              <div style={{ color: remainingGold >= 0 ? 'green' : 'red', fontWeight: 'bold' }}>
                Remaining: {remainingGold} gp
              </div>
              {equipmentMode === 'gold-only' && (
                <small style={{ color: '#666', display: 'block', marginTop: '4px' }}>
                  Spend what you want from your starting gold
                </small>
              )}
            </div>
          </div>
        </div>

        {/* Action Buttons */}
        <div style={{ display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
          <button onClick={onClose} style={{ padding: '8px 16px' }}>
            Cancel
          </button>
          <button
            onClick={() => setSelectedItems([])}
            style={{ padding: '8px 16px', backgroundColor: '#fff3cd', border: '1px solid #ffc107', cursor: 'pointer' }}
          >
            Clear
          </button>
          <button
            onClick={handleConfirm}
            disabled={selectedItems.length === 0 || remainingGold < 0}
            style={{
              padding: '8px 16px',
              backgroundColor: '#4CAF50',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: selectedItems.length === 0 || remainingGold < 0 ? 'not-allowed' : 'pointer',
              opacity: selectedItems.length === 0 || remainingGold < 0 ? 0.6 : 1,
            }}
          >
            Confirm Selection
          </button>
        </div>
      </div>
    </div>
  )
}
