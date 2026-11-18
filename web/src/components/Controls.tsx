import type { AbilityInfo, Direction, ItemInfo } from '../api/types'
import { abilityNames } from '../game/items'
import { ItemIcon } from './ItemIcon'

interface ControlsProps {
  disabled: boolean
  quickSlot?: ItemInfo
  quickSlotQuantity: number
  ability?: AbilityInfo
  onMove: (direction: Direction) => void
  onAttack: () => void
  onUseQuickSlot: () => void
  onUseAbility: () => void
}

export function Controls({
  disabled,
  quickSlot,
  quickSlotQuantity,
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
        <button className="use" aria-label="Use quick slot item" disabled={disabled} onClick={onUseQuickSlot}>
          {quickSlot ? <ItemIcon item={quickSlot} /> : 'Use'}
          {quickSlot && <span className="slot-quantity">{quickSlotQuantity}</span>}
        </button>
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
