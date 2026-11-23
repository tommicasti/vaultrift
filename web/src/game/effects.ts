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
  | 'shards'
  | 'spores'
  | 'overload'
  | 'coins'
  | 'levelup'

export interface Effect {
  key: string
  x: number
  y: number
  kind: EffectKind
  facing: Direction
  delayMs: number
  label?: string
}

export const effectDurationMs = 450
export const coinDurationMs = 900
export const levelUpDurationMs = 1400
export const arrowStepMs = 45
const chainDelayMs = 150
const burnDelayMs = 300

const abilityEffects: Record<ClassAbility, EffectKind | null> = {
  Bulwark: 'bulwark',
  Fury: 'fury',
  PowerShot: null,
  Invisibility: 'smoke',
  Overload: 'overload',
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
  let lootSpot: TilePosition | undefined

  function add(position: TilePosition | undefined, kind: EffectKind, delayMs: number, label?: string) {
    if (!position) return

    effects.push({
      key: `${turn}-${effects.length}`,
      x: position.x,
      y: position.y,
      kind,
      facing: player.facing,
      delayMs,
      label,
    })
  }

  for (const event of events) {
    const landed = !event.isDodged && event.element !== undefined

    switch (event.type) {
      case 'ArrowFlew': {
        const start = impactDelayMs
        event.tiles?.forEach((tile, index) => add(tile, event.powered ? 'power' : 'arrow', start + index * arrowStepMs))
        impactDelayMs = start + (event.tiles?.length ?? 0) * arrowStepMs
        break
      }
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
      case 'SpellCast': {
        const kind = event.element ? elementEffects[event.element] : 'impact'
        const start = impactDelayMs
        event.path?.forEach((tile, index) => add(tile, kind, start + index * arrowStepMs))
        impactDelayMs = start + (event.path?.length ?? 0) * arrowStepMs
        event.tiles?.forEach(tile => add(tile, kind, impactDelayMs))
        break
      }
      case 'EnemyPulled':
        if (event.enemyId !== undefined) add(positionsAfter.get(event.enemyId), 'wind', impactDelayMs + chainDelayMs)
        break
      case 'PlayerPulled':
      case 'PlayerBlownAway':
        add(player, 'wind', 0)
        break
      case 'CrystalShattered':
        add(event.x !== undefined && event.y !== undefined ? { x: event.x, y: event.y } : undefined, 'shards', impactDelayMs)
        break
      case 'ObstacleSmashed':
        add(event.x !== undefined && event.y !== undefined ? { x: event.x, y: event.y } : undefined, 'impact', impactDelayMs)
        break
      case 'CrystalsGrew':
      case 'CrystalsBurst':
        event.tiles?.forEach(tile => add(tile, 'shards', 0))
        break
      case 'EnemyShardHit':
        if (event.enemyId !== undefined)
          add(positionsAfter.get(event.enemyId) ?? positionsBefore.get(event.enemyId), 'impact', impactDelayMs + chainDelayMs)
        break
      case 'PlayerShardHit':
        add(player, 'impact', chainDelayMs)
        break
      case 'PlayerPoisoned':
      case 'PlayerPoisonDamaged':
        add(player, 'spores', burnDelayMs)
        break
      case 'EnemySummoned':
        add(event.x !== undefined && event.y !== undefined ? { x: event.x, y: event.y } : undefined, 'spores', 0)
        break
      case 'EnemyWithered':
        if (event.enemyId !== undefined) add(positionsBefore.get(event.enemyId), 'spores', impactDelayMs + chainDelayMs)
        break
      case 'PlayerScorched':
        add(player, 'fire', impactDelayMs + burnDelayMs)
        break
      case 'EnemyScorched':
        if (event.enemyId !== undefined)
          add(positionsAfter.get(event.enemyId) ?? positionsBefore.get(event.enemyId), 'fire', impactDelayMs + burnDelayMs)
        break
      case 'ChestOpened':
        lootSpot = event.x !== undefined && event.y !== undefined ? { x: event.x, y: event.y } : undefined
        add(lootSpot, 'impact', 0)
        break
      case 'EnemyDefeated':
        if (event.enemyId !== undefined) lootSpot = positionsBefore.get(event.enemyId) ?? positionsAfter.get(event.enemyId)
        break
      case 'CoinsGained':
        if (event.amount) add(lootSpot ?? player, 'coins', impactDelayMs + chainDelayMs, `+${event.amount}`)
        break
      case 'LeveledUp':
        add(player, 'levelup', impactDelayMs + chainDelayMs)
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
  return Math.max(
    effectDurationMs,
    ...effects.map(effect => effect.delayMs + (effect.kind === 'coins' ? coinDurationMs : effect.kind === 'levelup' ? levelUpDurationMs : effectDurationMs)),
  )
}
