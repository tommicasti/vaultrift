import type { ClassAbility, Direction, Element, EnemyState, GameEvent, PlayerState, TilePosition } from '../api/types'

export type EffectKind =
  | 'fire'
  | 'ice'
  | 'lightning'
  | 'wind'
  | 'arrow'
  | 'sweep'
  | 'thrust'
  | 'power'
  | 'impact'
  | 'block'
  | 'fury'
  | 'bulwark'
  | 'smoke'

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

const abilityEffects: Record<ClassAbility, EffectKind | null> = {
  Bulwark: 'bulwark',
  Fury: 'fury',
  PowerShot: null,
  Invisibility: 'smoke',
}

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
        event.tiles?.forEach((tile, index) => add(tile, event.powered ? 'power' : 'arrow', index * arrowStepMs))
        impactDelayMs = (event.tiles?.length ?? 0) * arrowStepMs
        break
      case 'PlayerSwept':
        event.tiles?.forEach(tile => add(tile, 'sweep', 0))
        break
      case 'PlayerThrust':
        event.tiles?.forEach(tile => add(tile, 'thrust', 0))
        break
      case 'EnemySlammed':
      case 'EnemyPricked':
        if (event.enemyId !== undefined)
          add(positionsAfter.get(event.enemyId) ?? positionsBefore.get(event.enemyId), 'impact', impactDelayMs + chainDelayMs)
        break
      case 'LavaSpread':
        event.tiles?.forEach(tile => add(tile, 'fire', 0))
        break
      case 'PlayerPulled':
        add(player, 'wind', 0)
        break
      case 'PlayerScorched':
        add(player, 'fire', impactDelayMs + burnDelayMs)
        break
      case 'EnemyScorched':
        if (event.enemyId !== undefined)
          add(positionsAfter.get(event.enemyId) ?? positionsBefore.get(event.enemyId), 'fire', impactDelayMs + burnDelayMs)
        break
      case 'ChestOpened':
        add(event.x !== undefined && event.y !== undefined ? { x: event.x, y: event.y } : undefined, 'impact', 0)
        break
      case 'MimicRevealed':
        add(event.x !== undefined && event.y !== undefined ? { x: event.x, y: event.y } : undefined, 'smoke', 0)
        break
      case 'AbilityUsed': {
        const kind = event.ability ? abilityEffects[event.ability] : null
        if (kind) add(player, kind, 0)
        break
      }
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
        if (event.isBlocked) add(player, 'block', impactDelayMs + burnDelayMs)
        else if (landed) add(player, elementEffects[event.element!], impactDelayMs + burnDelayMs)
        break
    }
  }

  return effects
}

export function effectsDuration(effects: Effect[]): number {
  return Math.max(0, ...effects.map(effect => effect.delayMs)) + effectDurationMs
}
