import type { InventoryEntryState } from '../api/types'
import { ItemIcon } from './ItemIcon'

interface InventoryProps {
  entries: InventoryEntryState[]
  capacity: number
}

export function Inventory({ entries, capacity }: InventoryProps) {
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
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}