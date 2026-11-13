import { useState } from 'react'
import type { EquipmentSlot, InventoryEntryState, PlayerState } from '../api/types'
import { describeItem, slotLabels } from '../game/items'
import { ItemIcon } from './ItemIcon'

const slotOrder: EquipmentSlot[] = ['Head', 'Weapon', 'Body', 'Ring1', 'Feet', 'Ring2']

interface InventoryScreenProps {
  player: PlayerState
  entries: InventoryEntryState[]
  capacity: number
  disabled: boolean
  onEquip: (entryId: number) => void
  onUse: (entryId: number) => void
  onAssignQuickSlot: (entryId: number) => void
  onClose: () => void
}

export function InventoryScreen({
  player,
  entries,
  capacity,
  disabled,
  onEquip,
  onUse,
  onAssignQuickSlot,
  onClose,
}: InventoryScreenProps) {
  const [selectedId, setSelectedId] = useState<number | null>(null)

  const entriesById = new Map(entries.map(entry => [entry.id, entry]))
  const equippedBySlot = new Map(player.equipment.map(item => [item.slot, item.entryId]))
  const equippedIds = new Set(player.equipment.map(item => item.entryId))
  const selected = selectedId === null ? undefined : entriesById.get(selectedId)

  return (
    <div className="overlay" role="dialog" aria-modal="true" aria-label="Inventory">
      <section className="inventory-screen">
        <header className="inventory-header">
          <h2>Inventory</h2>
          <button aria-label="Close inventory" onClick={onClose}>
            ✕
          </button>
        </header>

        <div className="inventory-body">
          <div className="character-panel">
            <div className="character-portrait" aria-hidden="true">
              🧝
            </div>
            <div className="equipment-slots">
              {slotOrder.map(slot => {
                const entryId = equippedBySlot.get(slot)
                const entry = entryId === undefined ? undefined : entriesById.get(entryId)

                return (
                  <button
                    key={slot}
                    className={entry ? `slot rarity-${entry.item.rarity.toLowerCase()}` : 'slot'}
                    disabled={!entry}
                    aria-label={entry ? `${slotLabels[slot]}: ${entry.item.name}` : `${slotLabels[slot]}: empty`}
                    onClick={() => entry && setSelectedId(entry.id)}
                  >
                    {entry ? <ItemIcon item={entry.item} /> : <span className="slot-label">{slotLabels[slot]}</span>}
                  </button>
                )
              })}
            </div>
            <dl className="combat-stats">
              <dt>Health</dt>
              <dd>
                {player.health} / {player.maxHealth}
              </dd>
              <dt>Attack</dt>
              <dd>{player.attack}</dd>
              <dt>Defense</dt>
              <dd>{player.defense}</dd>
              <dt>Evasion</dt>
              <dd>{player.evasion}%</dd>
            </dl>
          </div>

          <div className="backpack">
            <div className="label">
              Backpack {entries.length} / {capacity}
            </div>
            <div className="backpack-grid">
              {Array.from({ length: capacity }, (_, index) => {
                const entry = entries[index]

                if (!entry) return <div key={`empty-${index}`} className="slot" />

                const classes = [
                  'slot',
                  `rarity-${entry.item.rarity.toLowerCase()}`,
                  entry.id === selectedId ? 'selected' : '',
                ].join(' ')

                return (
                  <button key={entry.id} className={classes} aria-label={entry.item.name} onClick={() => setSelectedId(entry.id)}>
                    <ItemIcon item={entry.item} />
                    {entry.quantity > 1 && <span className="slot-quantity">{entry.quantity}</span>}
                    {equippedIds.has(entry.id) && <span className="slot-equipped">E</span>}
                  </button>
                )
              })}
            </div>
          </div>
        </div>

        <ItemDetails
          entry={selected}
          equipped={selected !== undefined && equippedIds.has(selected.id)}
          inQuickSlot={selected !== undefined && player.quickSlot?.id === selected.item.id}
          disabled={disabled}
          onEquip={onEquip}
          onUse={onUse}
          onAssignQuickSlot={onAssignQuickSlot}
        />
      </section>
    </div>
  )
}

interface ItemDetailsProps {
  entry?: InventoryEntryState
  equipped: boolean
  inQuickSlot: boolean
  disabled: boolean
  onEquip: (entryId: number) => void
  onUse: (entryId: number) => void
  onAssignQuickSlot: (entryId: number) => void
}

function ItemDetails({ entry, equipped, inQuickSlot, disabled, onEquip, onUse, onAssignQuickSlot }: ItemDetailsProps) {
  if (!entry) {
    return (
      <div className="item-details">
        <span className="label">Select an item to see its details.</span>
      </div>
    )
  }

  const { item } = entry
  const isPotion = item.category === 'Potion'

  return (
    <div className={`item-details rarity-${item.rarity.toLowerCase()}`}>
      <ItemIcon item={item} />
      <div className="item-details-text">
        <div className="item-name">{item.name}</div>
        <div className="label">
          {item.rarity} · {describeItem(item)}
        </div>
        {item.description && <p className="item-description">{item.description}</p>}
      </div>
      <div className="item-actions">
        {isPotion && (
          <button disabled={disabled} onClick={() => onUse(entry.id)}>
            Use
          </button>
        )}
        {isPotion &&
          (inQuickSlot ? (
            <span className="equipped-tag">In quick slot</span>
          ) : (
            <button disabled={disabled} onClick={() => onAssignQuickSlot(entry.id)}>
              Set quick slot
            </button>
          ))}
        {!isPotion &&
          (equipped ? (
            <span className="equipped-tag">Equipped</span>
          ) : (
            <button disabled={disabled} onClick={() => onEquip(entry.id)}>
              Equip
            </button>
          ))}
      </div>
    </div>
  )
}
