import type { EnemyState } from '../api/types'
import { enemySprite, rarityStars, sheets, starSprites } from '../game/sprites'
import { ElementIcon } from './ElementIcon'
import { LevelBadge } from './LevelBadge'
import { Sprite } from './Sprite'

interface EnemyMarkerProps {
  enemy: EnemyState
}

export function EnemyMarker({ enemy }: EnemyMarkerProps) {
  const healthPercent = (enemy.health / enemy.maxHealth) * 100
  const stars = rarityStars[enemy.rarity]
  const actorClasses = ['actor', enemy.isAwake ? '' : 'sleeping', enemy.isFrozen ? 'frozen' : ''].filter(Boolean).join(' ')
  const healthClass = enemy.weakness ? `enemy-health weak-${enemy.weakness.toLowerCase()}` : 'enemy-health'

  return (
    <>
      <Sprite
        sheet={sheets.dungeon}
        index={enemySprite(enemy.kind)}
        className={actorClasses}
        label={`${enemy.rarity} ${enemy.name}, level ${enemy.level}`}
      />
      {stars > 0 && (
        <span className="rarity-stars" aria-hidden="true">
          {Array.from({ length: stars }, (_, index) => (
            <Sprite key={index} sheet={sheets.items} index={starSprites[enemy.rarity]} className="star" />
          ))}
        </span>
      )}
      {(enemy.isBurning || enemy.isFrozen) && (
        <span className="enemy-statuses">
          {enemy.isBurning && <ElementIcon element="Fire" className="status-icon" />}
          {enemy.isFrozen && <ElementIcon element="Ice" className="status-icon" />}
        </span>
      )}
      <LevelBadge level={enemy.level} />
      <span className={healthClass} aria-hidden="true">
        <span className="enemy-health-fill" style={{ width: `${healthPercent}%` }} />
      </span>
    </>
  )
}