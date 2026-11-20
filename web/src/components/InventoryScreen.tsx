import { useEffect, useState } from 'react'
import type { EquipmentSlot, InventoryEntryState, ItemInfo, ItemMark, PlayerState, SpellInfo, StatPoint, StatsPreview } from '../api/types'
import { coinSprite } from '../game/sprites'
import { backpackSorts, loadBackpackSort, saveBackpackSort, sortBackpack, type BackpackSort } from '../game/backpackSort'
import {
  classNames,
  describeEffect,
  describeElement,
  describeItem,
  describeKind,
  describeSpell,
  elementNames,
  learnedSpellKeys,
  quickSlotKeys,
  scrollSlot,
  slotLabels,
} from '../game/items'
import { HeroSprite } from './HeroSprite'
import { ItemIcon } from './ItemIcon'
import { MarkBadge, MarkIcon } from './MarkBadge'
import { Sprite } from './Sprite'

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
  onAssignQuickSlot: (entryId: number, slot: number) => void
  onLearn: (entryId: number, key: number) => void
  onSpendPoint: (stat: StatPoint) => void
  onDiscard: (entryId: number) => void
  onDiscardJunk: () => void
  onMark: (entryId: number, mark: ItemMark) => void
  onPreview: (entryId: number, hand?: EquipmentSlot) => Promise<StatsPreview>
  onClose: () => void
}

const notWearable = new Set(['Potion', 'Scroll', 'Rune'])

const sortLabels: Record<BackpackSort, string> = {
  Recent: 'Recent',
  Rarity: 'Rarity',
  Type: 'Type',
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
  onLearn,
  onSpendPoint,
  onDiscard,
  onDiscardJunk,
  onMark,
  onPreview,
  onClose,
}: InventoryScreenProps) {
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [sort, setSort] = useState<BackpackSort>(loadBackpackSort)
  const [confirmingJunk, setConfirmingJunk] = useState(false)
  const [junkMode, setJunkMode] = useState(false)
  const [previewHand, setPreviewHand] = useState<EquipmentSlot | undefined>(undefined)
  const [loadedPreview, setLoadedPreview] = useState<{ key: string; stats: StatsPreview | null } | null>(null)

  const equippedBySlot = new Map(player.equipment.map(equipped => [equipped.slot, equipped]))
  const slotByEntryId = new Map(player.equipment.map(equipped => [equipped.entryId, equipped.slot]))
  const allEntries = new Map<number, InventoryEntryState>([
    ...entries.map(entry => [entry.id, entry] as const),
    ...player.equipment.map(
      equipped => [equipped.entryId, { id: equipped.entryId, item: equipped.item, quantity: 1, mark: equipped.mark, sellPrice: equipped.sellPrice }] as const,
    ),
  ])
  const sortedEntries = sortBackpack(entries, sort)
  const junkCount = entries.filter(entry => entry.mark === 'Junk').reduce((total, entry) => total + entry.quantity, 0)

  function changeSort(next: BackpackSort) {
    setSort(next)
    saveBackpackSort(next)
  }

  function handleDiscardJunk() {
    if (!confirmingJunk) {
      setConfirmingJunk(true)
      return
    }

    setConfirmingJunk(false)
    setSelectedId(null)
    onDiscardJunk()
  }
  const copies = new Map<string, number>()

  for (const entry of allEntries.values()) {
    if (entry.item.category === 'Potion' || entry.item.category === 'Rune') continue

    const key = duplicateKey(entry.item)
    copies.set(key, (copies.get(key) ?? 0) + 1)
  }

  const selected = selectedId === null ? undefined : allEntries.get(selectedId)
  const selectedSlot = selectedId === null ? undefined : slotByEntryId.get(selectedId)
  const previewable = selected !== undefined && selectedSlot === undefined && !notWearable.has(selected.item.category)
  const previewKey = previewable ? `${selected.id}|${previewHand ?? ''}|${player.equipment.map(equipped => equipped.entryId).join(',')}|${player.level}` : null

  const preview = previewKey !== null && loadedPreview?.key === previewKey ? loadedPreview.stats : null

  useEffect(() => {
    if (previewKey === null || selectedId === null) return

    let current = true

    onPreview(selectedId, previewHand)
      .then(result => {
        if (current) setLoadedPreview({ key: previewKey, stats: result.result === 'Equipped' ? result : null })
      })
      .catch(() => {
        if (current) setLoadedPreview({ key: previewKey, stats: null })
      })

    return () => {
      current = false
    }
  }, [previewKey, selectedId, previewHand, onPreview])
  const mainHand = equippedBySlot.get('MainHand')?.item
  const offHand = equippedBySlot.get('OffHand')?.item

  function handleBackpackDoubleClick(entry: InventoryEntryState) {
    if (disabled) return

    if (entry.item.category === 'Potion' || entry.item.category === 'Rune') onUse(entry.id)
    else if (entry.item.category !== 'Scroll') onEquip(entry.id)
  }

  function handleBackpackClick(entry: InventoryEntryState, shiftKey: boolean) {
    if (junkMode) {
      if (!disabled && entry.mark !== 'Favorite') onMark(entry.id, entry.mark === 'Junk' ? 'None' : 'Junk')
      return
    }

    if (shiftKey) handleBackpackDoubleClick(entry)
    else setSelectedId(entry.id)
  }

  function handleSlotDoubleClick(slot: EquipmentSlot) {
    if (!disabled) onUnequip(slot)
  }

  function handleSlotClick(slot: EquipmentSlot, entryId: number, shiftKey: boolean) {
    if (shiftKey) handleSlotDoubleClick(slot)
    else setSelectedId(entryId)
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

  function statRow(stat: StatPoint, label: string, value: string, now?: number, next?: number, suffix = '') {
    return (
      <>
        <dt>{label}</dt>
        <dd>
          {value}
          <StatChange now={now} next={next} suffix={suffix} />
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
                    onClick={event => equipped && handleSlotClick(slot, equipped.entryId, event.shiftKey)}
                    onDoubleClick={() => equipped && handleSlotDoubleClick(slot)}
                  >
                    {equipped ? <ItemIcon item={equipped.item} /> : <span className="slot-label">{slotLabels[slot]}</span>}
                    {equipped && <MarkBadge mark={equipped.mark} />}
                  </button>
                )
              })}
            </div>
            <dl className="combat-stats">
              {statRow('Health', 'Health', `${player.health} / ${player.maxHealth}`, player.maxHealth, preview?.maxHealth)}
              {statRow('Mana', 'Mana', `${player.mana} / ${player.maxMana}`, player.maxMana, preview?.maxMana)}
              {statRow('Attack', 'Attack', `${player.attack}`, player.attack, preview?.attack)}
              {statRow('Defense', 'Defense', `${player.defense}`, player.defense, preview?.defense)}
              {statRow('Evasion', 'Evasion', `${player.evasion}%`, player.evasion, preview?.evasion, '%')}
              {statRow('Critical', 'Critical', `${player.critical}%`, player.critical, preview?.critical, '%')}
              {(player.block > 0 || (preview?.block ?? 0) > 0) && (
                <>
                  <dt>Block</dt>
                  <dd>
                    {player.block}%
                    <StatChange now={player.block} next={preview?.block} suffix="%" />
                  </dd>
                </>
              )}
              <dt>Resists</dt>
              <dd>
                {player.resistance ? elementNames[player.resistance] : '—'}
                {preview && preview.resistance !== player.resistance && (
                  <span className={preview.resistance ? 'stat-change up' : 'stat-change down'}>
                    {preview.resistance ? `→ ${elementNames[preview.resistance]}` : '→ —'}
                  </span>
                )}
              </dd>
            </dl>
          </div>

          <div className="backpack">
            <div className="backpack-toolbar">
              <span className="label">
                Backpack {entries.length} / {capacity}
              </span>
              <div className="sort-control" role="group" aria-label="Sort backpack">
                {backpackSorts.map(option => (
                  <button
                    key={option}
                    className={option === sort ? 'sort-option active' : 'sort-option'}
                    aria-pressed={option === sort}
                    onClick={() => changeSort(option)}
                  >
                    {sortLabels[option]}
                  </button>
                ))}
              </div>
              <button
                className={junkMode ? 'junk-mode active' : 'junk-mode'}
                aria-pressed={junkMode}
                title="While on, every click marks or unmarks an item as junk"
                onClick={() => setJunkMode(on => !on)}
              >
                <MarkIcon mark="Junk" />
                {junkMode ? 'Marking junk: on' : 'Mark junk'}
              </button>
              {junkCount > 0 && (
                <button
                  className={confirmingJunk ? 'junk-button confirming' : 'junk-button'}
                  disabled={disabled}
                  onClick={handleDiscardJunk}
                  onBlur={() => setConfirmingJunk(false)}
                >
                  <MarkIcon mark="Junk" />
                  {confirmingJunk ? `Throw away ${junkCount}?` : `Throw away junk (${junkCount})`}
                </button>
              )}
            </div>
            <div className={junkMode ? 'backpack-grid junk-mode-on' : 'backpack-grid'}>
              {Array.from({ length: capacity }, (_, index) => {
                const entry = sortedEntries[index]

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
                    onClick={event => handleBackpackClick(entry, event.shiftKey)}
                    onDoubleClick={() => !junkMode && handleBackpackDoubleClick(entry)}
                  >
                    <ItemIcon item={entry.item} />
                    {entry.quantity > 1 && <span className="slot-quantity">{entry.quantity}</span>}
                    {isDuplicate && <span className="slot-duplicate">D</span>}
                    <MarkBadge mark={entry.mark} />
                  </button>
                )
              })}
            </div>
          </div>
        </div>

        <ItemDetails
          key={selected?.id ?? 'none'}
          onPreviewHand={setPreviewHand}
          entry={selected}
          equippedSlot={selectedSlot}
          playerClassId={player.classId}
          quickSlotsHolding={player.quickSlots.flatMap((quickSlot, slot) =>
            selected !== undefined && quickSlot.item?.id === selected.item.id ? [slot] : [],
          )}
          holdsStaff={player.spells.length > 0}
          learnedSpells={player.spells.slice(1)}
          disabled={disabled}
          onEquip={onEquip}
          onUnequip={onUnequip}
          onUse={onUse}
          onAssignQuickSlot={onAssignQuickSlot}
          onLearn={onLearn}
          onDiscard={entryId => {
            setSelectedId(null)
            onDiscard(entryId)
          }}
          onMark={onMark}
        />
      </section>
    </div>
  )
}

interface ItemDetailsProps {
  entry?: InventoryEntryState
  equippedSlot?: EquipmentSlot
  playerClassId?: string
  quickSlotsHolding: number[]
  holdsStaff: boolean
  learnedSpells: (SpellInfo | null)[]
  disabled: boolean
  onEquip: (entryId: number, hand?: EquipmentSlot) => void
  onUnequip: (slot: EquipmentSlot) => void
  onUse: (entryId: number) => void
  onAssignQuickSlot: (entryId: number, slot: number) => void
  onLearn: (entryId: number, key: number) => void
  onDiscard: (entryId: number) => void
  onMark: (entryId: number, mark: ItemMark) => void
  onPreviewHand: (hand?: EquipmentSlot) => void
}

function ItemDetails({
  entry,
  equippedSlot,
  playerClassId,
  quickSlotsHolding,
  holdsStaff,
  learnedSpells,
  disabled,
  onEquip,
  onUnequip,
  onUse,
  onAssignQuickSlot,
  onLearn,
  onDiscard,
  onMark,
  onPreviewHand,
}: ItemDetailsProps) {
  const [confirmingDiscard, setConfirmingDiscard] = useState(false)

  if (!entry) {
    return (
      <div className="item-details">
        <span className="label">Select an item to see its details. Shift+click or double-click to equip, use or take off.</span>
      </div>
    )
  }

  const { item } = entry
  const isScroll = item.category === 'Scroll'
  const isRune = item.category === 'Rune'
  const isPotion = item.category === 'Potion' || isScroll || isRune
  const isOneHanded = oneHandedCategories.has(item.category)
  const isForeignMod = item.classId !== undefined && item.classId !== playerClassId
  const element = describeElement(item)
  const kind = describeKind(item)

  return (
    <div className={`item-details rarity-${item.rarity.toLowerCase()}`}>
      <ItemIcon item={item} />
      <div className="item-details-text">
        <div className="item-title">
          <span className="item-name">{item.name}</span>
          <MarkToggle entry={entry} mark="Favorite" label="Favorite" disabled={disabled} onMark={onMark} />
          <MarkToggle entry={entry} mark="Junk" label="Junk" disabled={disabled} onMark={onMark} />
          <span className="item-value" title={entry.quantity > 1 ? `${entry.sellPrice * entry.quantity} for all ${entry.quantity}` : undefined}>
            <span className="label">Sells for</span>
            <span className="coins">
              <Sprite sheet={coinSprite.sheet} index={coinSprite.index} className="coin-icon" />
              {entry.sellPrice}
              {entry.quantity > 1 && <span className="label"> each</span>}
            </span>
          </span>
        </div>
        <div className="item-kind">{kind.name}</div>
        <div className="label">{[item.rarity, describeItem(item)].filter(Boolean).join(' · ')}</div>
        {item.classId && (
          <div className={isForeignMod ? 'label class-only foreign' : 'label class-only'}>
            {classNames[item.classId] ?? item.classId} only
          </div>
        )}
        <div className="label">{kind.hint}</div>
        {item.spell && <div className={`item-element element-${item.spell.element.toLowerCase()}`}>{describeSpell(item.spell)}</div>}
        {element && !item.spell && <div className={`item-element element-${item.element?.toLowerCase()}`}>{element}</div>}
        {item.effects.map(effect => (
          <div key={effect.kind} className="item-effect">
            {describeEffect(effect)}
          </div>
        ))}
        {item.description && <p className="item-description">{item.description}</p>}
      </div>
      <div className="item-actions">
        {isPotion && !isScroll && (
          <button disabled={disabled} onClick={() => onUse(entry.id)}>
            {isRune ? 'Use on weapon' : 'Use'}
          </button>
        )}
        {isScroll && (
          <div className="quick-slot-picker" role="group" aria-label="Learn on a key">
            <span className="label">Learn</span>
            {learnedSpellKeys.map((key, index) => (
              <button
                key={key}
                disabled={disabled || !holdsStaff}
                aria-label={`Learn on key ${key}`}
                title={
                  !holdsStaff
                    ? 'Hold a staff to learn it'
                    : learnedSpells[index]
                      ? `Replaces ${learnedSpells[index]?.name}, which goes back to its scroll`
                      : `Key ${key} is free`
                }
                onClick={() => onLearn(entry.id, key)}
              >
                {key}
                {learnedSpells[index] && <span className="taken-dot" aria-hidden="true" />}
              </button>
            ))}
          </div>
        )}
        {isPotion && !isRune && (
          <div className="quick-slot-picker" role="group" aria-label="Quick slot">
            <span className="label">Quick</span>
            {quickSlotKeys.map((key, slot) => ({ key, slot })).filter(({ slot }) => (slot === scrollSlot) === isScroll).map(({ key, slot }) => (
              <button
                key={key}
                className={quickSlotsHolding.includes(slot) ? 'active' : ''}
                aria-pressed={quickSlotsHolding.includes(slot)}
                aria-label={`Put in quick slot ${key}`}
                title={`Quick slot ${key}`}
                disabled={disabled}
                onClick={() => onAssignQuickSlot(entry.id, slot)}
              >
                {key}
              </button>
            ))}
          </div>
        )}
        {!isPotion && equippedSlot && (
          <button disabled={disabled} onClick={() => onUnequip(equippedSlot)}>
            Unequip
          </button>
        )}
        {!isPotion && !equippedSlot && isOneHanded && (
          <>
            <button
              disabled={disabled}
              onPointerEnter={() => onPreviewHand('MainHand')}
              onFocus={() => onPreviewHand('MainHand')}
              onPointerLeave={() => onPreviewHand(undefined)}
              onClick={() => onEquip(entry.id, 'MainHand')}
            >
              Main hand
            </button>
            <button
              disabled={disabled}
              onPointerEnter={() => onPreviewHand('OffHand')}
              onFocus={() => onPreviewHand('OffHand')}
              onPointerLeave={() => onPreviewHand(undefined)}
              onClick={() => onEquip(entry.id, 'OffHand')}
            >
              Off hand
            </button>
          </>
        )}
        {!isPotion && !equippedSlot && !isOneHanded && (
          <button disabled={disabled || isForeignMod} onClick={() => onEquip(entry.id)}>
            Equip
          </button>
        )}
        {!equippedSlot && entry.mark !== 'Favorite' && (
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

interface MarkToggleProps {
  entry: InventoryEntryState
  mark: Exclude<ItemMark, 'None'>
  label: string
  disabled: boolean
  onMark: (entryId: number, mark: ItemMark) => void
}

function MarkToggle({ entry, mark, label, disabled, onMark }: MarkToggleProps) {
  const active = entry.mark === mark

  return (
    <button
      className={`mark-toggle mark-${mark.toLowerCase()}${active ? ' active' : ''}`}
      disabled={disabled}
      aria-pressed={active}
      title={active ? `Remove ${label.toLowerCase()} mark` : `Mark as ${label.toLowerCase()}`}
      onClick={() => onMark(entry.id, active ? 'None' : mark)}
    >
      <MarkIcon mark={mark} />
      <span>{label}</span>
    </button>
  )
}

interface StatChangeProps {
  now?: number
  next?: number
  suffix?: string
}

function StatChange({ now, next, suffix = '' }: StatChangeProps) {
  if (now === undefined || next === undefined || now === next) return null

  const up = next > now

  return (
    <span className={up ? 'stat-change up' : 'stat-change down'} title={`${up ? '+' : ''}${next - now}${suffix} if you equip it`}>
      {up ? '▲' : '▼'} {next}
      {suffix}
    </span>
  )
}
