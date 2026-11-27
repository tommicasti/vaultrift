import { describe, expect, it } from 'vitest'
import { burstFor } from './liveEffects'
import type { LiveEvent } from './types'

const event = (fields: Partial<LiveEvent>) => ({ critical: false, missed: false, ...fields }) as LiveEvent
const monster = () => ({ x: 3, y: 4 })
const hero = () => ({ x: 7, y: 8 })

describe('burstFor', () => {
  it('shows the element of a landed hit on the monster or on the hero', () => {
    expect(burstFor(event({ type: 'MonsterHit', monsterId: 1, element: 'Fire' }), monster, hero)).toEqual({ x: 3, y: 4, kind: 'fire' })
    expect(burstFor(event({ type: 'HeroHit', heroId: 1, element: 'Ice' }), monster, hero)).toEqual({ x: 7, y: 8, kind: 'ice' })
  })

  it('shows nothing for a miss or a plain hit', () => {
    expect(burstFor(event({ type: 'MonsterHit', monsterId: 1, element: 'Fire', missed: true }), monster, hero)).toBeUndefined()
    expect(burstFor(event({ type: 'MonsterHit', monsterId: 1 }), monster, hero)).toBeUndefined()
  })

  it('marks burning, freezing, poison and abilities', () => {
    expect(burstFor(event({ type: 'MonsterFrozen', monsterId: 1 }), monster, hero)?.kind).toBe('ice')
    expect(burstFor(event({ type: 'HeroPoisoned', heroId: 1 }), monster, hero)?.kind).toBe('spores')
    expect(burstFor(event({ type: 'AbilityActivated', heroId: 1, ability: 'Fury' }), monster, hero)?.kind).toBe('fury')
    expect(burstFor(event({ type: 'AbilityActivated', heroId: 1, ability: 'PowerShot' }), monster, hero)).toBeUndefined()
  })
})
