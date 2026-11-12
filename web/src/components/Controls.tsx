import type { Direction } from '../api/types'

interface ControlsProps {
  disabled: boolean
  onMove: (direction: Direction) => void
  onAttack: () => void
}

export function Controls({ disabled, onMove, onAttack }: ControlsProps) {
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
      <button className="attack" disabled={disabled} onClick={onAttack}>
        Attack
      </button>
    </section>
  )
}