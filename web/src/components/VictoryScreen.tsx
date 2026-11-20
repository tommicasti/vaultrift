import { crownSprite, sheets } from '../game/sprites'
import { Sprite } from './Sprite'

interface VictoryScreenProps {
  heroName: string
  deaths: number
  onKeepExploring: () => void
  onNewGame: () => void
}

export function VictoryScreen({ heroName, deaths, onKeepExploring, onNewGame }: VictoryScreenProps) {
  return (
    <div className="overlay" role="dialog" aria-modal="true" aria-label="Victory">
      <section className="victory-screen">
        <Sprite sheet={sheets.items} index={crownSprite} className="victory-crown" />
        <h2>The vault is yours</h2>
        <p>
          {heroName} crossed twenty floors and slew the Custode in the rift
          {deaths === 0 ? ' without falling once.' : ` after falling ${deaths} ${deaths === 1 ? 'time' : 'times'}.`}
        </p>
        <div className="victory-actions">
          <button onClick={onKeepExploring}>Keep exploring</button>
          <button className="primary" onClick={onNewGame}>
            New game
          </button>
        </div>
      </section>
    </div>
  )
}
