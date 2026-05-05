import { useState } from 'react'
import type { CharacterInventoryState, CharacterInventoryItemData } from '../types'
import ItemCard from './ItemCard'

interface InventoryPanelProps {
  inventory: CharacterInventoryState | null
  onEquip?: (item: CharacterInventoryItemData) => Promise<void>
  onAttune?: (item: CharacterInventoryItemData) => Promise<void>
  onRemove?: (inventoryItemId: string) => Promise<void>
  loading?: boolean
}

export default function InventoryPanel({
  inventory,
  onEquip,
  onAttune,
  onRemove,
  loading = false,
}: InventoryPanelProps) {
  const [sortBy, setSortBy] = useState<'name' | 'value' | 'weight' | 'type'>('name')
  const [filterEquipped, setFilterEquipped] = useState<'all' | 'equipped' | 'unequipped'>('all')
  const [filterMagical, setFilterMagical] = useState<'all' | 'magical' | 'mundane'>('all')
  const [viewMode, setViewMode] = useState<'list' | 'grid'>('list')

  if (!inventory) {
    return <div style={{ padding: '16px', color: '#999' }}>No inventory data available</div>
  }

  // Filter items
  let filtered = [...inventory.items]

  if (filterEquipped !== 'all') {
    filtered = filtered.filter((item) => (filterEquipped === 'equipped' ? item.isEquipped : !item.isEquipped))
  }

  if (filterMagical !== 'all') {
    filtered = filtered.filter((item) =>
      filterMagical === 'magical' ? item.requiresAttunement || item.itemType === 'Wondrous Item' : !item.requiresAttunement && item.itemType !== 'Wondrous Item'
    )
  }

  // Sort items
  filtered.sort((a, b) => {
    switch (sortBy) {
      case 'name':
        return a.itemName.localeCompare(b.itemName)
      case 'value':
        return b.goldValue - a.goldValue
      case 'weight':
        return b.weight - a.weight
      case 'type':
        return a.itemType.localeCompare(b.itemType)
      default:
        return 0
    }
  })

  const totalWeight = filtered.reduce((sum, item) => sum + item.weight * item.quantity, 0)
  const totalValue = filtered.reduce((sum, item) => sum + item.goldValue * item.quantity, 0)
  const attuneCount = filtered.filter((item) => item.isAttuned).length

  return (
    <div style={{ padding: '16px', backgroundColor: '#f5f5f5', borderRadius: '8px' }}>
      <div style={{ marginBottom: '16px' }}>
        <h2 style={{ marginTop: 0 }}>Character Inventory</h2>

        {/* Summary Stats */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '12px', marginBottom: '16px' }}>
          <div style={{ backgroundColor: 'white', padding: '12px', borderRadius: '4px', border: '1px solid #ddd' }}>
            <small style={{ color: '#666' }}>Items</small>
            <div style={{ fontSize: '24px', fontWeight: 'bold' }}>{filtered.length}</div>
          </div>
          <div style={{ backgroundColor: 'white', padding: '12px', borderRadius: '4px', border: '1px solid #ddd' }}>
            <small style={{ color: '#666' }}>Total Weight</small>
            <div style={{ fontSize: '24px', fontWeight: 'bold' }}>{totalWeight} lbs</div>
          </div>
          <div style={{ backgroundColor: 'white', padding: '12px', borderRadius: '4px', border: '1px solid #ddd' }}>
            <small style={{ color: '#666' }}>Total Value</small>
            <div style={{ fontSize: '24px', fontWeight: 'bold' }}>{totalValue} gp</div>
          </div>
          <div style={{ backgroundColor: 'white', padding: '12px', borderRadius: '4px', border: '1px solid #ddd' }}>
            <small style={{ color: '#666' }}>Attunement</small>
            <div style={{ fontSize: '24px', fontWeight: 'bold', color: attuneCount >= inventory.attunementCap ? '#f44336' : '#666' }}>
              {attuneCount} / {inventory.attunementCap}
            </div>
          </div>
        </div>

        {/* Errors and Warnings */}
        {(inventory.errors.length > 0 || inventory.warnings.length > 0) && (
          <div style={{ marginBottom: '16px' }}>
            {inventory.errors.map((error, idx) => (
              <div
                key={idx}
                style={{
                  padding: '8px 12px',
                  backgroundColor: '#ffebee',
                  color: '#c62828',
                  borderRadius: '4px',
                  marginBottom: '4px',
                  fontSize: '14px',
                }}
              >
                ✕ {error}
              </div>
            ))}
            {inventory.warnings.map((warning, idx) => (
              <div
                key={idx}
                style={{
                  padding: '8px 12px',
                  backgroundColor: '#fff3e0',
                  color: '#e65100',
                  borderRadius: '4px',
                  marginBottom: '4px',
                  fontSize: '14px',
                }}
              >
                ⚠ {warning}
              </div>
            ))}
          </div>
        )}

        {/* Controls */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))', gap: '12px', marginBottom: '16px' }}>
          <div>
            <label style={{ display: 'block', marginBottom: '4px', fontSize: '12px', fontWeight: 'bold' }}>Sort By</label>
            <select value={sortBy} onChange={(e) => setSortBy(e.target.value as any)} style={{ width: '100%' }}>
              <option value="name">Name</option>
              <option value="type">Type</option>
              <option value="value">Value</option>
              <option value="weight">Weight</option>
            </select>
          </div>
          <div>
            <label style={{ display: 'block', marginBottom: '4px', fontSize: '12px', fontWeight: 'bold' }}>Equipment Status</label>
            <select value={filterEquipped} onChange={(e) => setFilterEquipped(e.target.value as any)} style={{ width: '100%' }}>
              <option value="all">All items</option>
              <option value="equipped">Equipped only</option>
              <option value="unequipped">Unequipped only</option>
            </select>
          </div>
          <div>
            <label style={{ display: 'block', marginBottom: '4px', fontSize: '12px', fontWeight: 'bold' }}>Item Quality</label>
            <select value={filterMagical} onChange={(e) => setFilterMagical(e.target.value as any)} style={{ width: '100%' }}>
              <option value="all">All items</option>
              <option value="magical">Magical only</option>
              <option value="mundane">Mundane only</option>
            </select>
          </div>
          <div>
            <label style={{ display: 'block', marginBottom: '4px', fontSize: '12px', fontWeight: 'bold' }}>View</label>
            <div style={{ display: 'flex', gap: '4px' }}>
              <button
                onClick={() => setViewMode('list')}
                style={{
                  flex: 1,
                  padding: '6px',
                  backgroundColor: viewMode === 'list' ? '#2196F3' : '#ddd',
                  color: viewMode === 'list' ? 'white' : 'black',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: 'pointer',
                  fontSize: '12px',
                }}
              >
                ☰ List
              </button>
              <button
                onClick={() => setViewMode('grid')}
                style={{
                  flex: 1,
                  padding: '6px',
                  backgroundColor: viewMode === 'grid' ? '#2196F3' : '#ddd',
                  color: viewMode === 'grid' ? 'white' : 'black',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: 'pointer',
                  fontSize: '12px',
                }}
              >
                ⊞ Grid
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Items Display */}
      {loading ? (
        <div style={{ textAlign: 'center', padding: '32px', color: '#999' }}>Loading inventory...</div>
      ) : filtered.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '32px', color: '#999' }}>No items in inventory</div>
      ) : viewMode === 'list' ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
          {filtered.map((item) => (
            <ItemCard
              key={item.inventoryItemId}
              item={item}
              onEquip={onEquip}
              onAttune={onAttune}
              onRemove={onRemove}
              compact={true}
            />
          ))}
        </div>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(250px, 1fr))', gap: '12px' }}>
          {filtered.map((item) => (
            <ItemCard
              key={item.inventoryItemId}
              item={item}
              onEquip={onEquip}
              onAttune={onAttune}
              onRemove={onRemove}
              compact={false}
            />
          ))}
        </div>
      )}
    </div>
  )
}
