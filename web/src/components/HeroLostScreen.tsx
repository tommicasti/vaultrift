import { graveSprite, sheets } from '../game/sprites'
import { Sprite } from './Sprite'

interface HeroLostScreenProps {
  heroName: string
  floor: number
  level: number
  onLookAround: () => void
  onNewGame: () => void
}

export function HeroLostScreen({ heroName, floor, level, onLookAround, onNewGame }: HeroLostScreenProps) {
  return (
    <div className="overlay" role="dialog" aria-modal="true" aria-label="Hero lost">
      <section className="victory-screen lost-screen">
        <Sprite sheet={sheets.dungeon} index={graveSprite} className="victory-crown" />
        <h2>{heroName} has fallen</h2>
        <p>
          A level {level} hero, lost on floor {floor}. In Hardcore there is no second chance: the vault keeps what it takes.
        </p>
        <div className="victory-actions">
          <button onClick={onLookAround}>Look around</button>
          <button className="primary" onClick={onNewGame}>
            New hero
          </button>
        </div>
      </section>
    </div>
  )
}
