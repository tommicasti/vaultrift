import type { EnemyState } from '../api/types'
import { enemySprite, rarityStars, sheets, starSprites } from '../game/sprites'
import { LevelBadge } from './LevelBadge'
import { Sprite } from './Sprite'

interface EnemyMarkerProps {
  enemy: EnemyState
}

export function EnemyMarker({ enemy }: EnemyMarkerProps) {
  const healthPercent = (enemy.health / enemy.maxHealth) * 100
  const stars = rarityStars[enemy.rarity]

  return (
    <>
      <Sprite
        sheet={sheets.dungeon}
        index={enemySprite(enemy.kind)}
        className={enemy.isAwake ? 'actor' : 'actor sleeping'}
        label={`${enemy.rarity} ${enemy.name}, level ${enemy.level}`}
      />
      {stars > 0 && (
        <span className="rarity-stars" aria-hidden="true">
          {Array.from({ length: stars }, (_, index) => (
            <Sprite key={index} sheet={sheets.items} index={starSprites[enemy.rarity]} className="star" />
          ))}
        </span>
      )}
      <LevelBadge level={enemy.level} />
      <span className="enemy-health" aria-hidden="true">
        <span className="enemy-health-fill" style={{ width: `${healthPercent}%` }} />
      </span>
    </>
  )
}