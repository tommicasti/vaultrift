import { useRef, useState } from 'react'
import type { AbilityInfo, QuickSlotInfo } from '../api/types'
import { ItemIcon } from '../components/ItemIcon'
import { tap } from '../game/haptics'
import { quickSlotKeys } from '../game/items'
import { joystickVector } from './input'

interface LiveTouchControlsProps {
  quickSlots: QuickSlotInfo[]
  ability?: AbilityInfo
  onAbility: () => void
  onMove: (x: number, y: number) => void
  onAttack: (down: boolean) => void
  onQuickSlot: (slot: number) => void
  onBackpack: () => void
  onLeave: () => void
  sprinting: boolean
  onSprint: (on: boolean) => void
}

const stickRadius = 56

export function LiveTouchControls({ quickSlots, ability, onAbility, onMove, onAttack, onQuickSlot, onBackpack, onLeave, sprinting, onSprint }: LiveTouchControlsProps) {
  const origin = useRef<{ id: number; x: number; y: number } | null>(null)
  const [knob, setKnob] = useState<{ x: number; y: number } | null>(null)

  const release = () => {
    origin.current = null
    setKnob(null)
    onMove(0, 0)
  }

  return (
    <div className="live-touch" onPointerDown={event => event.stopPropagation()}>
      <div
        className="live-stick"
        onPointerDown={event => {
          event.currentTarget.setPointerCapture(event.pointerId)
          origin.current = { id: event.pointerId, x: event.clientX, y: event.clientY }
          setKnob({ x: 0, y: 0 })
        }}
        onPointerMove={event => {
          const start = origin.current
          if (!start || start.id !== event.pointerId) return
          const { x, y, offsetX, offsetY } = joystickVector(event.clientX - start.x, event.clientY - start.y, stickRadius)
          setKnob({ x: offsetX, y: offsetY })
          onMove(x, y)
        }}
        onPointerUp={release}
        onPointerCancel={release}
      >
        <div className="live-stick-base">
          <div className="live-stick-knob" style={knob ? { transform: `translate(${knob.x}px, ${knob.y}px)` } : undefined} />
        </div>
      </div>
      <div className="live-buttons">
        <button
          type="button"
          className="live-attack"
          aria-label="Attack"
          onPointerDown={event => {
            event.currentTarget.setPointerCapture(event.pointerId)
            tap()
            onAttack(true)
          }}
          onPointerUp={() => onAttack(false)}
          onPointerCancel={() => onAttack(false)}
        >
          ⚔
        </button>
        <button
          type="button"
          className={sprinting ? 'live-slot live-sprint on' : 'live-slot live-sprint'}
          aria-label="Run"
          aria-pressed={sprinting}
          onClick={() => {
            tap()
            onSprint(!sprinting)
          }}
        >
          »
        </button>
        {ability && (
          <button
            type="button"
            className="live-slot live-ability"
            aria-label={ability.kind}
            disabled={ability.cooldown > 0}
            onClick={() => {
              tap()
              onAbility()
            }}
          >
            {ability.cooldown > 0 ? ability.cooldown : 'E'}
          </button>
        )}
        {quickSlots.map((slot, index) => (
          <button
            key={index}
            type="button"
            className="live-slot"
            aria-label={`Quick slot ${quickSlotKeys[index]}`}
            disabled={!slot.item}
            onClick={() => {
              tap()
              onQuickSlot(index)
            }}
          >
            {slot.item ? <ItemIcon item={slot.item} /> : quickSlotKeys[index]}
            {slot.item && slot.quantity > 1 && <span>{slot.quantity}</span>}
          </button>
        ))}
        <button type="button" className="live-slot" aria-label="Backpack" onClick={onBackpack}>
          🎒
        </button>
        <button type="button" className="live-slot" aria-label="Leave" onClick={onLeave}>
          ✕
        </button>
      </div>
    </div>
  )
}
