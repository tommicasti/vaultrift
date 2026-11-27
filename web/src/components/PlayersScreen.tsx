import { useEffect, useState } from 'react'
import { getPlayers, type PlayerReport, type PlayersReport, type ReportItem } from '../api/adminApi'
import { slotLabels } from '../game/items'

interface PlayersScreenProps {
  onClose: () => void
}

function hours(seconds: number) {
  const minutes = Math.floor(seconds / 60)
  if (minutes < 60) return `${minutes} min`
  return `${Math.floor(minutes / 60)} h ${minutes % 60} min`
}

function when(value: string | null | undefined) {
  if (!value) return 'never'
  return new Date(value).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' })
}

function Items({ items, empty }: { items: ReportItem[]; empty: string }) {
  if (items.length === 0) return <p className="label">{empty}</p>
  return (
    <ul className="players-items">
      {items.map((item, index) => (
        <li key={`${item.name}-${index}`} className={`rarity-${item.rarity.toLowerCase()}`}>
          {item.slot && <span className="label">{slotLabels[item.slot]}: </span>}
          <span className="players-item-name">{item.name}</span>
          {item.quantity > 1 && ` ×${item.quantity}`}
          <span className="label"> · {item.rarity}</span>
        </li>
      ))}
    </ul>
  )
}

function Player({ player }: { player: PlayerReport }) {
  return (
    <article className="players-card">
      <header className="players-head">
        <h3>
          {player.online && <span className="players-online" title="Playing now" />}
          {player.userName}
        </h3>
        <span className="label">
          Last seen {player.online ? 'now' : when(player.lastSeen)} · joined {when(player.joinedAt)} · {hours(player.playSeconds)} played
        </span>
      </header>

      <section>
        <h4>Trophies ({player.trophies.length})</h4>
        {player.trophies.length === 0 ? (
          <p className="label">None yet.</p>
        ) : (
          <ul className="players-chips">
            {player.trophies.map(trophy => (
              <li key={trophy.name} title={`${trophy.description} · ${when(trophy.unlockedAt)}`}>
                {trophy.name}
              </li>
            ))}
          </ul>
        )}
      </section>

      <section>
        <h4>Heroes ({player.heroes.length})</h4>
        {player.heroes.length === 0 && <p className="label">No heroes yet.</p>}
        {player.heroes.map(hero => (
          <div key={hero.name + hero.updatedAt} className="players-hero">
            <p>
              <strong>{hero.name}</strong> · {hero.className} level {hero.level} · {hero.coins} coins · {hours(hero.playSeconds)} · {hero.falls} falls
              {hero.online && <span className="players-badge">in a world</span>}
            </p>
            <p className="label">
              Last saved {when(hero.updatedAt)} · {hero.backpackItems} items in the backpack
            </p>
            <Items items={hero.equipment} empty="Nothing worn." />
          </div>
        ))}
      </section>

      <section>
        <h4>Worlds ({player.worlds.length})</h4>
        {player.worlds.length === 0 ? (
          <p className="label">No worlds yet.</p>
        ) : (
          <ul className="players-list">
            {player.worlds.map(world => (
              <li key={world.updatedAt + world.difficulty}>
                {world.difficulty} {world.className && `· ${world.className}`} · floor {world.floor} (deepest {world.deepestFloor}){world.biome && ` · ${world.biome}`}
                {world.conquered && ' · conquered'} · {hours(world.playSeconds)}
                {world.open && <span className="players-badge">open</span>}
                <span className="label"> · {when(world.updatedAt)}</span>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section>
        <h4>Deposit ({player.deposit.items} items)</h4>
        <Items items={player.deposit.treasures} empty="No legendary, exotic or platinum items." />
      </section>

      {player.turnHeroes.length > 0 && (
        <section>
          <h4>Turn-based heroes ({player.turnHeroes.length})</h4>
          <ul className="players-list">
            {player.turnHeroes.map(hero => (
              <li key={hero.name + hero.updatedAt}>
                <strong>{hero.name}</strong> · {hero.className} level {hero.level} · {hero.difficulty} floor {hero.floor} · {hero.turns} turns · {hero.coins} coins · {hero.deaths} deaths ·{' '}
                {hours(hero.playSeconds)}
                {hero.conquered && ' · conquered'}
                {hero.lost && ' · lost'}
                <span className="label"> · {when(hero.updatedAt)}</span>
              </li>
            ))}
          </ul>
        </section>
      )}
    </article>
  )
}

export function PlayersScreen({ onClose }: PlayersScreenProps) {
  const [report, setReport] = useState<PlayersReport | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [reload, setReload] = useState(0)

  useEffect(() => {
    let active = true
    getPlayers()
      .then(found => {
        if (active) {
          setReport(found)
          setError(null)
        }
      })
      .catch((failure: unknown) => {
        if (active) setError(failure instanceof Error ? failure.message : 'The vault did not answer.')
      })
    return () => {
      active = false
    }
  }, [reload])

  return (
    <div className="home-worlds" role="dialog" aria-label="Players">
      <div className="home-card home-worlds-card players-screen">
        <div className="home-create-head">
          <h2>Players</h2>
          <div className="players-tools">
            <button onClick={() => setReload(count => count + 1)}>Refresh</button>
            <button onClick={onClose}>Close</button>
          </div>
        </div>
        {report && (
          <p className="label">
            {report.players.length} accounts · {report.players.filter(player => player.online).length} playing now · updated {when(report.generatedAt)}
          </p>
        )}
        {error && <p className="error">{error}</p>}
        {!report && !error && <p className="label">Reading the vault…</p>}
        <div className="players-body">{report?.players.map(player => <Player key={player.userName} player={player} />)}</div>
      </div>
    </div>
  )
}
