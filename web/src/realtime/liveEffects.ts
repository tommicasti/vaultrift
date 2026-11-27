import type { Element } from '../api/types'
import type { LiveEvent } from './types'

export type BurstKind =
  | 'rubble'
  | 'crystal'
  | 'claw'
  | 'fire'
  | 'ice'
  | 'lightning'
  | 'wind'
  | 'impact'
  | 'fury'
  | 'bulwark'
  | 'smoke'
  | 'spores'
  | 'overload'

export interface BurstSpot {
  x: number
  y: number
  kind: BurstKind
}

type Locate = (id: number | undefined) => { x: number; y: number } | undefined

const elementBursts: Record<Element, BurstKind> = {
  Fire: 'fire',
  Ice: 'ice',
  Lightning: 'lightning',
  Wind: 'wind',
}

const abilityBursts: Record<string, BurstKind> = {
  Bulwark: 'bulwark',
  Fury: 'fury',
  Invisibility: 'smoke',
  Overload: 'overload',
}

function at(spot: { x: number; y: number } | undefined, kind: BurstKind): BurstSpot | undefined {
  return spot ? { x: spot.x, y: spot.y, kind } : undefined
}

export function burstFor(event: LiveEvent, monster: Locate, hero: Locate): BurstSpot | undefined {
  switch (event.type) {
    case 'MonsterHit':
      return !event.missed && event.element ? at(monster(event.monsterId), elementBursts[event.element]) : undefined
    case 'HeroHit':
      return !event.missed && event.element ? at(hero(event.heroId), elementBursts[event.element]) : undefined
    case 'MonsterIgnited':
    case 'MonsterScorched':
    case 'MonsterBurnedByLava':
      return at(monster(event.monsterId), 'fire')
    case 'MonsterFrozen':
      return at(monster(event.monsterId), 'ice')
    case 'MonsterEnvenomed':
    case 'MonsterWithered':
    case 'MonsterSummoned':
      return at(monster(event.monsterId), 'spores')
    case 'MonsterPushed':
      return at(monster(event.monsterId), 'wind')
    case 'MonsterShardHit':
    case 'MonsterPricked':
      return at(monster(event.monsterId), 'impact')
    case 'MimicSprang':
      return at(monster(event.monsterId), 'smoke')
    case 'HeroScorched':
      return at(hero(event.heroId), 'fire')
    case 'HeroPoisoned':
    case 'HeroPoisonDamaged':
      return at(hero(event.heroId), 'spores')
    case 'HeroPulled':
    case 'HeroBlownAway':
      return at(hero(event.heroId), 'wind')
    case 'HeroShardHit':
      return at(hero(event.heroId), 'impact')
    case 'AbilityActivated':
      return event.ability && abilityBursts[event.ability] ? at(hero(event.heroId), abilityBursts[event.ability]) : undefined
    case 'SpellUnleashed':
      return event.element && event.x !== undefined && event.y !== undefined ? { x: event.x, y: event.y, kind: elementBursts[event.element] } : undefined
    default:
      return undefined
  }
}
