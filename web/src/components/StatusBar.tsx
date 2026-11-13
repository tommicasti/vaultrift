import type { PlayerState } from '../api/types'
import { describeModifier } from '../game/items'
import { ItemIcon } from './ItemIcon'

interface StatusBarProps {
  player: PlayerState
  deaths: number
}

export function StatusBar({ player, deaths }: StatusBarProps) {
  const healthPercent = (player.health / player.maxHealth) * 100
  const hasExtras = player.quickSlot !== undefined || player.boosts.length > 0

  return (
    <section className="status-bar">
      <div className="health">
        <div className="label">
          Health {player.health} / {player.maxHealth}
        </div>
        <div className="health-track">
          <div className="health-fill" style={{ width: `${healthPercent}%` }} />
        </div>
      </div>
      <div className="label stats">
        ATK {player.attack} · DEF {player.defense} · EVA {player.evasion}%
      </div>
      <div className="label">Deaths: {deaths}</div>
      {hasExtras && (
        <div className="status-extras">
          {player.quickSlot && (
            <span className="label quick-slot-info">
              Quick slot <ItemIcon item={player.quickSlot} /> ×{player.quickSlotQuantity}
            </span>
          )}
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
