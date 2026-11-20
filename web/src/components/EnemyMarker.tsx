import type { EnemyState } from '../api/types'
import { crownSprite, enemySprite, rarityStars, sheets, starSprites } from '../game/sprites'
import { ElementIcon } from './ElementIcon'
import { LevelBadge } from './LevelBadge'
import { Sprite } from './Sprite'

interface EnemyMarkerProps {
  enemy: EnemyState
}

export function EnemyMarker({ enemy }: EnemyMarkerProps) {
  const healthPercent = (enemy.health / enemy.maxHealth) * 100
  const stars = enemy.isBoss ? 0 : rarityStars[enemy.rarity]
  const sprite = enemySprite(enemy.kind)
  const actorClasses = ['actor', `kind-${enemy.kind}`, enemy.isBoss ? 'boss' : '', enemy.isAwake ? '' : 'sleeping', enemy.isFrozen ? 'frozen' : '']
    .filter(Boolean)
    .join(' ')
  const label = enemy.isBoss ? `${enemy.name}, guardian of this floor` : `${enemy.rarity} ${enemy.name}, level ${enemy.level}`
  const healthClass = enemy.weakness ? `enemy-health weak-${enemy.weakness.toLowerCase()}` : 'enemy-health'

  return (
    <>
      <Sprite sheet={sprite.sheet} index={sprite.index} className={actorClasses} label={label} />
      {enemy.isBoss && (
        <span className="rarity-stars" aria-hidden="true">
          <Sprite sheet={sheets.items} index={crownSprite} className="crown" />
        </span>
      )}
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
      {!enemy.isBoss && <LevelBadge level={enemy.level} />}
      <span className={healthClass} aria-hidden="true">
        <span className="enemy-health-fill" style={{ width: `${healthPercent}%` }} />
      </span>
    </>
  )
}
