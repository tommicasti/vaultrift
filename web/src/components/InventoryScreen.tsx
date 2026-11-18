import { useState } from 'react'
import type { EquipmentSlot, InventoryEntryState, ItemInfo, PlayerState } from '../api/types'
import { describeElement, describeItem, describeWeaponTrait, elementNames, slotLabels } from '../game/items'
import { HeroSprite } from './HeroSprite'
import { ItemIcon } from './ItemIcon'

const slotOrder: EquipmentSlot[] = ['Head', 'Weapon', 'Body', 'Ring1', 'Feet', 'Ring2', 'Cloak']

interface InventoryScreenProps {
  player: PlayerState
  entries: InventoryEntryState[]
  capacity: number
  disabled: boolean
  onEquip: (entryId: number) => void
  onUnequip: (slot: EquipmentSlot) => void
  onUse: (entryId: number) => void
  onAssignQuickSlot: (entryId: number) => void
  onClose: () => void
}

function duplicateKey(item: ItemInfo): string {
  return `${item.id}|${item.element ?? ''}`
}

export function InventoryScreen({
  player,
  entries,
  capacity,
  disabled,
  onEquip,
  onUnequip,
  onUse,
  onAssignQuickSlot,
  onClose,
}: InventoryScreenProps) {
  const [selectedId, setSelectedId] = useState<number | null>(null)

  const equippedBySlot = new Map(player.equipment.map(equipped => [equipped.slot, equipped]))
  const slotByEntryId = new Map(player.equipment.map(equipped => [equipped.entryId, equipped.slot]))
  const allEntries = new Map<number, InventoryEntryState>([
    ...entries.map(entry => [entry.id, entry] as const),
    ...player.equipment.map(equipped => [equipped.entryId, { id: equipped.entryId, item: equipped.item, quantity: 1 }] as const),
  ])
  const copies = new Map<string, number>()

  for (const entry of allEntries.values()) {
    if (entry.item.category === 'Potion') continue

    const key = duplicateKey(entry.item)
    copies.set(key, (copies.get(key) ?? 0) + 1)
  }

  const selected = selectedId === null ? undefined : allEntries.get(selectedId)
  const selectedSlot = selectedId === null ? undefined : slotByEntryId.get(selectedId)
  const weapon = equippedBySlot.get('Weapon')?.item

  function handleBackpackDoubleClick(entry: InventoryEntryState) {
    if (disabled) return

    if (entry.item.category === 'Potion') onUse(entry.id)
    else onEquip(entry.id)
  }

  function handleSlotDoubleClick(slot: EquipmentSlot) {
    if (!disabled) onUnequip(slot)
  }

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
            <div className="character-portrait">
              <HeroSprite player={player} className="portrait-sprite" />
              {weapon && <ItemIcon item={weapon} className="held-weapon" />}
            </div>
            <div className="hero-name">{player.name}</div>
            <div className="equipment-slots">
              {slotOrder.map(slot => {
                const equipped = equippedBySlot.get(slot)

                return (
                  <button
                    key={slot}
                    className={[
                      'slot',
                      `slot-${slot.toLowerCase()}`,
                      equipped ? `rarity-${equipped.item.rarity.toLowerCase()}` : '',
                      equipped && equipped.entryId === selectedId ? 'selected' : '',
                    ].join(' ')}
                    disabled={!equipped}
                    aria-label={equipped ? `${slotLabels[slot]}: ${equipped.item.name}` : `${slotLabels[slot]}: empty`}
                    onClick={() => equipped && setSelectedId(equipped.entryId)}
                    onDoubleClick={() => equipped && handleSlotDoubleClick(slot)}
                  >
                    {equipped ? <ItemIcon item={equipped.item} /> : <span className="slot-label">{slotLabels[slot]}</span>}
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
              <dt>Critical</dt>
              <dd>{player.critical}%</dd>
              <dt>Resists</dt>
              <dd>{player.resistance ? elementNames[player.resistance] : '—'}</dd>
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

                const isDuplicate = (copies.get(duplicateKey(entry.item)) ?? 0) > 1
                const classes = [
                  'slot',
                  `rarity-${entry.item.rarity.toLowerCase()}`,
                  entry.id === selectedId ? 'selected' : '',
                ].join(' ')

                return (
                  <button
                    key={entry.id}
                    className={classes}
                    aria-label={isDuplicate ? `${entry.item.name}, duplicate` : entry.item.name}
                    onClick={() => setSelectedId(entry.id)}
                    onDoubleClick={() => handleBackpackDoubleClick(entry)}
                  >
                    <ItemIcon item={entry.item} />
                    {entry.quantity > 1 && <span className="slot-quantity">{entry.quantity}</span>}
                    {isDuplicate && <span className="slot-duplicate">D</span>}
                  </button>
                )
              })}
            </div>
          </div>
        </div>

        <ItemDetails
          entry={selected}
          equippedSlot={selectedSlot}
          inQuickSlot={selected !== undefined && player.quickSlot?.id === selected.item.id}
          disabled={disabled}
          onEquip={onEquip}
          onUnequip={onUnequip}
          onUse={onUse}
          onAssignQuickSlot={onAssignQuickSlot}
        />
      </section>
    </div>
  )
}

interface ItemDetailsProps {
  entry?: InventoryEntryState
  equippedSlot?: EquipmentSlot
  inQuickSlot: boolean
  disabled: boolean
  onEquip: (entryId: number) => void
  onUnequip: (slot: EquipmentSlot) => void
  onUse: (entryId: number) => void
  onAssignQuickSlot: (entryId: number) => void
}

function ItemDetails({
  entry,
  equippedSlot,
  inQuickSlot,
  disabled,
  onEquip,
  onUnequip,
  onUse,
  onAssignQuickSlot,
}: ItemDetailsProps) {
  if (!entry) {
    return (
      <div className="item-details">
        <span className="label">Select an item to see its details. Double-click to equip, use or take off.</span>
      </div>
    )
  }

  const { item } = entry
  const isPotion = item.category === 'Potion'
  const element = describeElement(item)
  const trait = describeWeaponTrait(item)

  return (
    <div className={`item-details rarity-${item.rarity.toLowerCase()}`}>
      <ItemIcon item={item} />
      <div className="item-details-text">
        <div className="item-name">{item.name}</div>
        <div className="label">
          {item.rarity} · {describeItem(item)}
        </div>
        {trait && <div className="label">{trait}</div>}
        {element && <div className={`item-element element-${item.element?.toLowerCase()}`}>{element}</div>}
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
          (equippedSlot ? (
            <button disabled={disabled} onClick={() => onUnequip(equippedSlot)}>
              Unequip
            </button>
          ) : (
            <button disabled={disabled} onClick={() => onEquip(entry.id)}>
              Equip
            </button>
          ))}
      </div>
    </div>
  )
}