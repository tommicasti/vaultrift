import { useRef } from 'react'
import type { AbilityInfo, Direction, QuickSlotInfo } from '../api/types'
import { tap } from '../game/haptics'
import { abilityNames, quickSlotKeys } from '../game/items'
import { ItemIcon } from './ItemIcon'

const turnHoldMs = 300

const directions: { direction: Direction; className: string; symbol: string }[] = [
  { direction: 'Up', className: 'up', symbol: '▲' },
  { direction: 'Left', className: 'left', symbol: '◀' },
  { direction: 'Right', className: 'right', symbol: '▶' },
  { direction: 'Down', className: 'down', symbol: '▼' },
]

interface ControlsProps {
  disabled: boolean
  quickSlots: QuickSlotInfo[]
  ability?: AbilityInfo
  onMove: (direction: Direction) => void
  onFace: (direction: Direction) => void
  onAttack: () => void
  onUseQuickSlot: (slot: number) => void
  onUseAbility: () => void
}

export function Controls({
  disabled,
  quickSlots,
  ability,
  onMove,
  onFace,
  onAttack,
  onUseQuickSlot,
  onUseAbility,
}: ControlsProps) {
  const holdTimer = useRef<number | null>(null)
  const turned = useRef(false)

  function startHold(direction: Direction) {
    turned.current = false
    cancelHold()
    holdTimer.current = window.setTimeout(() => {
      turned.current = true
      tap()
      onFace(direction)
    }, turnHoldMs)
  }

  function cancelHold() {
    if (holdTimer.current !== null) window.clearTimeout(holdTimer.current)
    holdTimer.current = null
  }

  function release(direction: Direction) {
    cancelHold()

    if (turned.current) {
      turned.current = false
      return
    }

    tap()
    onMove(direction)
  }

  return (
    <section className="controls">
      <div className="dpad">
        {directions.map(({ direction, className, symbol }) => (
          <button
            key={direction}
            className={className}
            aria-label={`Move ${direction.toLowerCase()}, hold to turn`}
            disabled={disabled}
            onPointerDown={() => startHold(direction)}
            onPointerLeave={cancelHold}
            onPointerCancel={cancelHold}
            onContextMenu={event => event.preventDefault()}
            onClick={() => release(direction)}
          >
            {symbol}
          </button>
        ))}
      </div>
      <div className="actions">
        <div className="quick-slots">
          {quickSlots.map((quickSlot, slot) => (
            <button
              key={slot}
              className="use"
              aria-label={`Use quick slot ${quickSlotKeys[slot]}`}
              disabled={disabled}
              onClick={() => {
                tap()
                onUseQuickSlot(slot)
              }}
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
            onClick={() => {
              tap()
              onUseAbility()
            }}
          >
            {ability.cooldown > 0 ? ability.cooldown : abilityNames[ability.kind]}
          </button>
        )}
        <button
          className="attack"
          disabled={disabled}
          onClick={() => {
            tap()
            onAttack()
          }}
        >
          Attack
        </button>
      </div>
    </section>
  )
}
