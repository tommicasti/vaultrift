import type { InventoryEntryState } from '../api/types'
import { ItemIcon } from './ItemIcon'

interface InventoryProps {
  entries: InventoryEntryState[]
  capacity: number
  equippedIds: number[]
  disabled: boolean
  onEquip: (entryId: number) => void
}

export function Inventory({ entries, capacity, equippedIds, disabled, onEquip }: InventoryProps) {
  return (
    <section className="inventory">
      <h2 className="label">
        Backpack {entries.length} / {capacity}
      </h2>
      {entries.length === 0 ? (
        <p className="label">Empty</p>
      ) : (
        <ul>
          {entries.map(entry => (
            <li key={entry.id} className={`rarity-${entry.item.rarity.toLowerCase()}`}>
              <ItemIcon item={entry.item} />
              <span className="item-name">{entry.item.name}</span>
              {entry.quantity > 1 && <span className="label">×{entry.quantity}</span>}
              {entry.item.category !== 'Potion' &&
                (equippedIds.includes(entry.id) ? (
                  <span className="equipped-tag">Equipped</span>
                ) : (
                  <button disabled={disabled} onClick={() => onEquip(entry.id)}>
                    Equip
                  </button>
                ))}
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}