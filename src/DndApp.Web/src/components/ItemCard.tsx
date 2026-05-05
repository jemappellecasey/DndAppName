import type { CharacterInventoryItemData } from '../types'

interface ItemCardProps {
  item: CharacterInventoryItemData
  onEquip?: (item: CharacterInventoryItemData) => void
  onAttune?: (item: CharacterInventoryItemData) => void
  onRemove?: (inventoryItemId: string) => void
  compact?: boolean
}

export default function ItemCard({
  item,
  onEquip,
  onAttune,
  onRemove,
  compact = false,
}: ItemCardProps) {
  if (compact) {
    return (
      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        padding: '8px',
        border: '1px solid #ddd',
        borderRadius: '4px',
        backgroundColor: item.isEquipped ? '#e3f2fd' : '#f9f9f9',
      }}>
        <div style={{ flex: 1 }}>
          <div style={{ fontWeight: 'bold' }}>{item.itemName}</div>
          <small style={{ color: '#666' }}>
            {item.goldValue} gp | {item.weight} lb × {item.quantity}
            {item.requiresAttunement && (
              <>
                {' '}
                | <span style={{ color: '#ff9800' }}>Attuned: {item.isAttuned ? '✓' : '✗'}</span>
              </>
            )}
          </small>
        </div>
        <div style={{ display: 'flex', gap: '4px' }}>
          {onEquip && (
            <button
              onClick={() => onEquip(item)}
              title={item.isEquipped ? 'Unequip' : 'Equip'}
              style={{
                padding: '4px 8px',
                backgroundColor: item.isEquipped ? '#4CAF50' : '#ddd',
                color: item.isEquipped ? 'white' : 'black',
                border: 'none',
                borderRadius: '4px',
                cursor: 'pointer',
              }}
            >
              {item.isEquipped ? '✓' : '○'}
            </button>
          )}
          {item.requiresAttunement && onAttune && (
            <button
              onClick={() => onAttune(item)}
              title={item.isAttuned ? 'Unattune' : 'Attune'}
              style={{
                padding: '4px 8px',
                backgroundColor: item.isAttuned ? '#ff9800' : '#ddd',
                color: item.isAttuned ? 'white' : 'black',
                border: 'none',
                borderRadius: '4px',
                cursor: 'pointer',
              }}
            >
              {item.isAttuned ? '✦' : '◇'}
            </button>
          )}
          {onRemove && (
            <button
              onClick={() => onRemove(item.inventoryItemId)}
              title="Remove"
              style={{
                padding: '4px 8px',
                backgroundColor: '#f44336',
                color: 'white',
                border: 'none',
                borderRadius: '4px',
                cursor: 'pointer',
              }}
            >
              ✕
            </button>
          )}
        </div>
      </div>
    )
  }

  // Full card view
  return (
    <div
      style={{
        border: '1px solid #ddd',
        borderRadius: '8px',
        padding: '16px',
        backgroundColor: item.isEquipped ? '#e3f2fd' : '#f9f9f9',
        boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
      }}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start', marginBottom: '12px' }}>
        <div>
          <h3 style={{ margin: '0 0 4px 0' }}>{item.itemName}</h3>
          <small style={{ color: '#666' }}>{item.itemType}</small>
        </div>
        <div style={{ display: 'flex', gap: '8px' }}>
          {item.isEquipped && (
            <span style={{ backgroundColor: '#4CAF50', color: 'white', padding: '4px 8px', borderRadius: '4px', fontSize: '12px' }}>
              Equipped
            </span>
          )}
          {item.isAttuned && (
            <span style={{ backgroundColor: '#ff9800', color: 'white', padding: '4px 8px', borderRadius: '4px', fontSize: '12px' }}>
              Attuned
            </span>
          )}
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px', marginBottom: '12px', fontSize: '14px' }}>
        <div>
          <strong>Value:</strong> {item.goldValue} gp
        </div>
        <div>
          <strong>Weight:</strong> {item.weight} lb × {item.quantity} = {item.weight * item.quantity} lb
        </div>
        {item.isWeapon && (
          <>
            <div>
              <strong>Damage:</strong> {item.damageDice}
            </div>
            <div>
              <strong>Ability:</strong> {item.weaponAbility}
            </div>
          </>
        )}
        {item.requiresAttunement && (
          <div style={{ color: '#ff9800', fontWeight: 'bold' }}>
            Requires Attunement
          </div>
        )}
      </div>

      {(onEquip || onAttune || onRemove) && (
        <div style={{ display: 'flex', gap: '8px', marginTop: '12px' }}>
          {onEquip && (
            <button
              onClick={() => onEquip(item)}
              style={{
                flex: 1,
                padding: '8px',
                backgroundColor: item.isEquipped ? '#4CAF50' : '#ddd',
                color: item.isEquipped ? 'white' : 'black',
                border: 'none',
                borderRadius: '4px',
                cursor: 'pointer',
              }}
            >
              {item.isEquipped ? '✓ Equipped' : 'Equip'}
            </button>
          )}
          {item.requiresAttunement && onAttune && (
            <button
              onClick={() => onAttune(item)}
              style={{
                flex: 1,
                padding: '8px',
                backgroundColor: item.isAttuned ? '#ff9800' : '#ddd',
                color: item.isAttuned ? 'white' : 'black',
                border: 'none',
                borderRadius: '4px',
                cursor: 'pointer',
              }}
            >
              {item.isAttuned ? '✦ Attuned' : 'Attune'}
            </button>
          )}
          {onRemove && (
            <button
              onClick={() => onRemove(item.inventoryItemId)}
              style={{
                flex: 1,
                padding: '8px',
                backgroundColor: '#f44336',
                color: 'white',
                border: 'none',
                borderRadius: '4px',
                cursor: 'pointer',
              }}
            >
              Remove
            </button>
          )}
        </div>
      )}
    </div>
  )
}
