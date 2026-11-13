import type { PlayerState } from '../api/types'

interface StatusBarProps {
  player: PlayerState
  deaths: number
}

export function StatusBar({ player, deaths }: StatusBarProps) {
  const healthPercent = (player.health / player.maxHealth) * 100

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
    </section>
  )
}