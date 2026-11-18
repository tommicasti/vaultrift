import type { Direction, Element, EnemyState, GameEvent, PlayerState, TilePosition } from '../api/types'

export type EffectKind = 'fire' | 'ice' | 'lightning' | 'wind' | 'arrow' | 'sweep'

export interface Effect {
  key: string
  x: number
  y: number
  kind: EffectKind
  facing: Direction
  delayMs: number
}

export const effectDurationMs = 450
export const arrowStepMs = 45
const chainDelayMs = 150
const burnDelayMs = 300

const elementEffects: Record<Element, EffectKind> = {
  Fire: 'fire',
  Ice: 'ice',
  Lightning: 'lightning',
  Wind: 'wind',
}

export function effectsForTurn(
  events: GameEvent[],
  enemiesBefore: EnemyState[],
  enemiesAfter: EnemyState[],
  player: PlayerState,
  turn: number,
): Effect[] {
  const positionsBefore = new Map<number, TilePosition>(enemiesBefore.map(enemy => [enemy.id, enemy]))
  const positionsAfter = new Map<number, TilePosition>(enemiesAfter.map(enemy => [enemy.id, enemy]))
  const effects: Effect[] = []
  let impactDelayMs = 0

  function add(position: TilePosition | undefined, kind: EffectKind, delayMs: number) {
    if (!position) return

    effects.push({
      key: `${turn}-${effects.length}`,
      x: position.x,
      y: position.y,
      kind,
      facing: player.facing,
      delayMs,
    })
  }

  for (const event of events) {
    const landed = !event.isDodged && event.element !== undefined

    switch (event.type) {
      case 'ArrowFlew':
        event.tiles?.forEach((tile, index) => add(tile, 'arrow', index * arrowStepMs))
        impactDelayMs = (event.tiles?.length ?? 0) * arrowStepMs
        break
      case 'PlayerSwept':
        event.tiles?.forEach(tile => add(tile, 'sweep', 0))
        break
      case 'PlayerAttackedEnemy':
        if (landed && event.enemyId !== undefined)
          add(positionsBefore.get(event.enemyId), elementEffects[event.element!], impactDelayMs)
        break
      case 'EnemyShocked':
        if (event.enemyId !== undefined)
          add(positionsBefore.get(event.enemyId), 'lightning', impactDelayMs + chainDelayMs)
        break
      case 'EnemyBurned':
        if (event.enemyId !== undefined)
          add(positionsAfter.get(event.enemyId) ?? positionsBefore.get(event.enemyId), 'fire', impactDelayMs + burnDelayMs)
        break
      case 'EnemyAttackedPlayer':
        if (landed)
          add(player, elementEffects[event.element!], impactDelayMs + burnDelayMs)
        break
    }
  }

  return effects
}

export function effectsDuration(effects: Effect[]): number {
  return Math.max(0, ...effects.map(effect => effect.delayMs)) + effectDurationMs
}