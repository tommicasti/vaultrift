import type { Direction, ItemInfo } from '../api/types'
import { ItemIcon } from './ItemIcon'

interface ControlsProps {
  disabled: boolean
  quickSlot?: ItemInfo
  quickSlotQuantity: number
  onMove: (direction: Direction) => void
  onAttack: () => void
  onUseQuickSlot: () => void
}

export function Controls({ disabled, quickSlot, quickSlotQuantity, onMove, onAttack, onUseQuickSlot }: ControlsProps) {
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
        <button className="attack" disabled={disabled} onClick={onAttack}>
          Attack
        </button>
      </div>
    </section>
  )
}
