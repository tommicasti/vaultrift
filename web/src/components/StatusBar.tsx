import type { PlayerState } from '../api/types'
import { describeModifier, elementNames } from '../game/items'
import { ElementIcon } from './ElementIcon'
import { ItemIcon } from './ItemIcon'

interface StatusBarProps {
  player: PlayerState
  deaths: number
  floor: number
  floorCount: number
}

export function StatusBar({ player, deaths, floor, floorCount }: StatusBarProps) {
  const healthPercent = (player.health / player.maxHealth) * 100
  const hasExtras =
    player.quickSlot !== undefined || player.boosts.length > 0 || player.resistance !== undefined || player.freeSteps > 0

  return (
    <section className="status-bar">
      <div className="health">
        <div className="label">
          <strong className="hero-name">{player.name}</strong> · Health {player.health} / {player.maxHealth}
        </div>
        <div className="health-track">
          <div className="health-fill" style={{ width: `${healthPercent}%` }} />
        </div>
      </div>
      <div className="label stats">
        ATK {player.attack} · DEF {player.defense} · EVA {player.evasion}% · CRIT {player.critical}%
      </div>
      <div className="label">
        Floor {floor} / {floorCount} · Deaths: {deaths}
      </div>
      {hasExtras && (
        <div className="status-extras">
          {player.quickSlot && (
            <span className="label quick-slot-info">
              Quick slot <ItemIcon item={player.quickSlot} /> ×{player.quickSlotQuantity}
            </span>
          )}
          {player.resistance && (
            <span className="label resistance-info">
              Resists <ElementIcon element={player.resistance} /> {elementNames[player.resistance]}
            </span>
          )}
          {player.freeSteps > 0 && <span className="boost-tag free-step-tag">Free step</span>}
          {player.boosts.map(boost => (
            <span key={boost.item.id} className="boost-tag">
              {describeModifier(boost.modifier)} · {boost.remainingTurns}
            </span>
          ))}
        </div>
      )}
    </section>
  )
}