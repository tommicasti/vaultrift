import { useState } from 'react'
import type { EquipmentSlot, InventoryEntryState, ItemInfo, PlayerState, StatPoint } from '../api/types'
import {
  classNames,
  describeEffect,
  describeElement,
  describeItem,
  describeWeaponTrait,
  elementNames,
  slotLabels,
} from '../game/items'
import { HeroSprite } from './HeroSprite'
import { ItemIcon } from './ItemIcon'

const slotOrder: EquipmentSlot[] = ['Ring1', 'Head', 'ClassMod', 'MainHand', 'Body', 'OffHand', 'Ring2', 'Feet', 'Cloak']

const oneHandedCategories = new Set(['Sword', 'Dagger'])

interface InventoryScreenProps {
  player: PlayerState
  entries: InventoryEntryState[]
  capacity: number
  disabled: boolean
  onEquip: (entryId: number, hand?: EquipmentSlot) => void
  onUnequip: (slot: EquipmentSlot) => void
  onUse: (entryId: number) => void
  onAssignQuickSlot: (entryId: number) => void
  onSpendPoint: (stat: StatPoint) => void
  onDiscard: (entryId: number) => void
  onClose: () => void
}

const pointGains: Record<StatPoint, string> = {
  Health: '+5',
  Attack: '+1',
  Defense: '+1',
  Evasion: '+2%',
  Critical: '+2%',
  Mana: '+5',
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
  onSpendPoint,
  onDiscard,
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
  const mainHand = equippedBySlot.get('MainHand')?.item
  const offHand = equippedBySlot.get('OffHand')?.item

  function handleBackpackDoubleClick(entry: InventoryEntryState) {
    if (disabled) return

    if (entry.item.category === 'Potion') onUse(entry.id)
    else onEquip(entry.id)
  }

  function handleSlotDoubleClick(slot: EquipmentSlot) {
    if (!disabled) onUnequip(slot)
  }

  function spendButton(stat: StatPoint, label: string) {
    if (player.unspentPoints === 0) return null

    return (
      <button
        className="spend-point"
        disabled={disabled}
        aria-label={`Spend a point on ${label}`}
        title={`${pointGains[stat]} ${label.toLowerCase()}`}
        onClick={() => onSpendPoint(stat)}
      >
        +
      </button>
    )
  }

  function statRow(stat: StatPoint, label: string, value: string) {
    return (
      <>
        <dt>{label}</dt>
        <dd>
          {value}
          {spendButton(stat, label)}
        </dd>
      </>
    )
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
              {mainHand && <ItemIcon item={mainHand} className="held-weapon" />}
              {offHand && <ItemIcon item={offHand} className="held-off-hand" />}
            </div>
            <div className="hero-name">
              {player.name} · Lv {player.level}
            </div>
            <div className="label hero-experience">
              XP {player.experience} / {player.experienceToNextLevel}
              {player.unspentPoints > 0 && (
                <strong className="points-left">
                  {' '}
                  · {player.unspentPoints} {player.unspentPoints === 1 ? 'point' : 'points'} to spend
                </strong>
              )}
            </div>
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
              {statRow('Health', 'Health', `${player.health} / ${player.maxHealth}`)}
              {statRow('Mana', 'Mana', `${player.mana} / ${player.maxMana}`)}
              {statRow('Attack', 'Attack', `${player.attack}`)}
              {statRow('Defense', 'Defense', `${player.defense}`)}
              {statRow('Evasion', 'Evasion', `${player.evasion}%`)}
              {statRow('Critical', 'Critical', `${player.critical}%`)}
              {player.block > 0 && (
                <>
                  <dt>Block</dt>
                  <dd>{player.block}%</dd>
                </>
              )}
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
          key={selected?.id ?? 'none'}
          entry={selected}
          equippedSlot={selectedSlot}
          playerClassId={player.classId}
          inQuickSlot={selected !== undefined && player.quickSlot?.id === selected.item.id}
          disabled={disabled}
          onEquip={onEquip}
          onUnequip={onUnequip}
          onUse={onUse}
          onAssignQuickSlot={onAssignQuickSlot}
          onDiscard={entryId => {
            setSelectedId(null)
            onDiscard(entryId)
          }}
        />
      </section>
    </div>
  )
}

interface ItemDetailsProps {
  entry?: InventoryEntryState
  equippedSlot?: EquipmentSlot
  playerClassId?: string
  inQuickSlot: boolean
  disabled: boolean
  onEquip: (entryId: number, hand?: EquipmentSlot) => void
  onUnequip: (slot: EquipmentSlot) => void
  onUse: (entryId: number) => void
  onAssignQuickSlot: (entryId: number) => void
  onDiscard: (entryId: number) => void
}

function ItemDetails({
  entry,
  equippedSlot,
  playerClassId,
  inQuickSlot,
  disabled,
  onEquip,
  onUnequip,
  onUse,
  onAssignQuickSlot,
  onDiscard,
}: ItemDetailsProps) {
  const [confirmingDiscard, setConfirmingDiscard] = useState(false)

  if (!entry) {
    return (
      <div className="item-details">
        <span className="label">Select an item to see its details. Double-click to equip, use or take off.</span>
      </div>
    )
  }

  const { item } = entry
  const isPotion = item.category === 'Potion'
  const isOneHanded = oneHandedCategories.has(item.category)
  const isForeignMod = item.classId !== undefined && item.classId !== playerClassId
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
        {item.classId && (
          <div className={isForeignMod ? 'label class-only foreign' : 'label class-only'}>
            {classNames[item.classId] ?? item.classId} only
          </div>
        )}
        {trait && <div className="label">{trait}</div>}
        {element && <div className={`item-element element-${item.element?.toLowerCase()}`}>{element}</div>}
        {item.effects.map(effect => (
          <div key={effect.kind} className="item-effect">
            {describeEffect(effect)}
          </div>
        ))}
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
        {!isPotion && equippedSlot && (
          <button disabled={disabled} onClick={() => onUnequip(equippedSlot)}>
            Unequip
          </button>
        )}
        {!isPotion && !equippedSlot && isOneHanded && (
          <>
            <button disabled={disabled} onClick={() => onEquip(entry.id, 'MainHand')}>
              Main hand
            </button>
            <button disabled={disabled} onClick={() => onEquip(entry.id, 'OffHand')}>
              Off hand
            </button>
          </>
        )}
        {!isPotion && !equippedSlot && !isOneHanded && (
          <button disabled={disabled || isForeignMod} onClick={() => onEquip(entry.id)}>
            Equip
          </button>
        )}
        {!equippedSlot && (
          <button
            className={confirmingDiscard ? 'discard confirming' : 'discard'}
            disabled={disabled}
            onClick={() => (confirmingDiscard ? onDiscard(entry.id) : setConfirmingDiscard(true))}
          >
            {confirmingDiscard ? `Throw away${entry.quantity > 1 ? ` all ${entry.quantity}` : ''}?` : 'Throw away'}
          </button>
        )}
      </div>
    </div>
  )
}
