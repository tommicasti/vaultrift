import type { AbilityInfo, Direction, QuickSlotInfo } from '../api/types'
import { abilityNames, quickSlotKeys } from '../game/items'
import { ItemIcon } from './ItemIcon'

interface ControlsProps {
  disabled: boolean
  quickSlots: QuickSlotInfo[]
  ability?: AbilityInfo
  onMove: (direction: Direction) => void
  onAttack: () => void
  onUseQuickSlot: (slot: number) => void
  onUseAbility: () => void
}

export function Controls({
  disabled,
  quickSlots,
  ability,
  onMove,
  onAttack,
  onUseQuickSlot,
  onUseAbility,
}: ControlsProps) {
  return (
    <section className="controls">
      <div className="dpad">
        <button className="up" aria-label="Move up" disabled={disabled} onClick={() => onMove('Up')}>
          ▲
        </button>
        <button className="left" aria-label="Move left" disabled={disabled} onClick={() => onMove('Left')}>
          ◀
        </button>
        <button className="right" aria-label="Move right" disabled={disabled} onClick={() => onMove('Right')}>
          ▶
        </button>
        <button className="down" aria-label="Move down" disabled={disabled} onClick={() => onMove('Down')}>
          ▼
        </button>
      </div>
      <div className="actions">
        <div className="quick-slots">
          {quickSlots.map((quickSlot, slot) => (
          <button
            key={slot}
            className="use"
            aria-label={`Use quick slot ${quickSlotKeys[slot]}`}
            disabled={disabled}
            onClick={() => onUseQuickSlot(slot)}
          >
            {quickSlot.item ? <ItemIcon item={quickSlot.item} /> : quickSlotKeys[slot]}
            {quickSlot.item && <span className="slot-quantity">{quickSlot.quantity}</span>}
          </button>
          ))}
        </div>
        {ability && (
          <button
            className={ability.activeTurns > 0 ? 'ability active' : 'ability'}
            aria-label={`Use ${abilityNames[ability.kind]}`}
            disabled={disabled || ability.cooldown > 0}
            onClick={onUseAbility}
          >
            {ability.cooldown > 0 ? ability.cooldown : abilityNames[ability.kind]}
          </button>
        )}
        <button className="attack" disabled={disabled} onClick={onAttack}>
          Attack
        </button>
      </div>
    </section>
  )
}
