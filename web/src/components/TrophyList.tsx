import type { TrophyInfo } from '../api/types'

interface TrophyListProps {
  trophies: TrophyInfo[]
  onClose: () => void
}

const dateFormat = new Intl.DateTimeFormat(undefined, { day: 'numeric', month: 'short', year: 'numeric' })

export function TrophyList({ trophies, onClose }: TrophyListProps) {
  const unlocked = trophies.filter(trophy => trophy.unlockedAt).length

  return (
    <div className="overlay" role="dialog" aria-modal="true" aria-label="Trophies">
      <section className="victory-screen trophy-screen">
        <h2>
          Trophies <span className="label">{unlocked} / {trophies.length}</span>
        </h2>
        <ul className="trophy-list">
          {trophies.map(trophy => (
            <li key={trophy.id} className={trophy.unlockedAt ? 'trophy unlocked' : 'trophy'}>
              <span className="trophy-icon" aria-hidden="true">
                {trophy.unlockedAt ? '🏆' : '🔒'}
              </span>
              <span className="trophy-details">
                <strong>{trophy.name}</strong>
                <span className="label">{trophy.description}</span>
                {!trophy.unlockedAt && trophy.goal ? (
                  <span className="trophy-progress" title={`${(trophy.progress ?? 0).toLocaleString()} / ${trophy.goal.toLocaleString()}`}>
                    <span style={{ width: `${Math.min(100, ((trophy.progress ?? 0) / trophy.goal) * 100)}%` }} />
                    <small>
                      {(trophy.progress ?? 0).toLocaleString()} / {trophy.goal.toLocaleString()}
                    </small>
                  </span>
                ) : null}
                {trophy.unlockedAt && <span className="label">Unlocked {dateFormat.format(new Date(trophy.unlockedAt))}</span>}
              </span>
            </li>
          ))}
        </ul>
        <div className="victory-actions">
          <button onClick={onClose}>Close</button>
        </div>
      </section>
    </div>
  )
}
