import { describe, expect, it } from 'vitest'
import type { EnemyState, GameEvent, ItemInfo, PlayerState } from '../api/types'
import { cuesForTurn } from './turnCues'

const player = { x: 5, y: 5 } as PlayerState
const spider = { id: 7, x: 5, y: 4, kind: 'spider' } as EnemyState
const sword = { category: 'Sword', twoHanded: false } as ItemInfo

function event(type: string, fields: Partial<GameEvent> = {}): GameEvent {
  return { type, ...fields } as GameEvent
}

describe('cuesForTurn', () => {
  it('returns nothing without a turn', () => {
    const cues = cuesForTurn(null, [spider], player, sword, undefined)

    expect(cues.hero).toBeUndefined()
    expect(cues.popups).toHaveLength(0)
  })

  it('plays the weapon attack on the hero and the hit on the enemy', () => {
    const turn = { id: 1, enemiesBefore: [spider], events: [event('PlayerAttackedEnemy', { enemyId: 7, damage: 4 })] }
    const cues = cuesForTurn(turn, [spider], player, sword, undefined)

    expect(cues.hero?.clip).toBe('1H_Melee_Attack_Chop')
    expect(cues.enemies.get(7)?.clip).toBe('Hit')
    expect(cues.popups.map(popup => popup.text)).toEqual(['4'])
  })

  it('keeps a defeated enemy on screen to play its death', () => {
    const turn = {
      id: 2,
      enemiesBefore: [spider],
      events: [event('PlayerAttackedEnemy', { enemyId: 7, damage: 9 }), event('EnemyDefeated', { enemyId: 7 })],
    }
    const cues = cuesForTurn(turn, [], player, sword, undefined)

    expect(cues.dying).toEqual([spider])
    expect(cues.enemies.get(7)).toMatchObject({ clip: 'Death', hold: true })
  })

  it('shows a blocked enemy strike on the hero', () => {
    const turn = { id: 3, enemiesBefore: [spider], events: [event('EnemyAttackedPlayer', { enemyId: 7, damage: 0, isBlocked: true })] }
    const cues = cuesForTurn(turn, [spider], player, sword, undefined)

    expect(cues.enemies.get(7)?.clip).toBe('Attack')
    expect(cues.hero?.clip).toBe('Block_Hit')
    expect(cues.popups[0]).toMatchObject({ text: 'Block', tone: 'miss' })
  })

  it('turns arrows and spells into projectiles without trailing bursts', () => {
    const path = [{ x: 5, y: 4 }, { x: 5, y: 3 }, { x: 5, y: 2 }]
    const turn = {
      id: 4,
      enemiesBefore: [],
      events: [event('ArrowFlew', { tiles: path }), event('SpellCast', { element: 'Fire', path, tiles: [{ x: 5, y: 2 }] })],
    }
    const cues = cuesForTurn(turn, [], player, sword, undefined)

    expect(cues.projectiles.map(projectile => projectile.kind)).toEqual(['arrow', 'fire'])
    expect([...cues.trail]).toEqual(['5,4', '5,3'])
  })

  it('lets the hero get up again when the same turn respawns him', () => {
    const turn = { id: 5, enemiesBefore: [spider], events: [event('PlayerDefeated'), event('PlayerRespawned')] }
    const cues = cuesForTurn(turn, [spider], player, sword, undefined)

    expect(cues.hero).toMatchObject({ clip: 'Death_A', hold: false })
  })

  it('shows a gust pushing the hero away instead of hurting the enemy', () => {
    const turn = { id: 9, enemiesBefore: [spider], events: [event('PlayerBlownAway', { enemyId: 7, damage: 3 })] }
    const cues = cuesForTurn(turn, [spider], player, sword, undefined)

    expect(cues.bursts.map(burst => burst.kind)).toEqual(['wind'])
    expect(cues.hero?.clip).toBe('Hit_A')
    expect(cues.popups).toMatchObject([{ text: '3', tone: 'player' }])
    expect(cues.enemies.has(7)).toBe(false)
  })

  it('flags weaknesses and resistances next to the damage', () => {
    const turn = {
      id: 10,
      enemiesBefore: [spider],
      events: [event('PlayerAttackedEnemy', { enemyId: 7, damage: 12, element: 'Fire', effectiveness: 'Effective' })],
    }
    const cues = cuesForTurn(turn, [spider], player, sword, undefined)

    expect(cues.popups.map(popup => [popup.text, popup.tone])).toEqual([
      ['12', 'enemy'],
      ['Weak!', 'weak'],
    ])
  })

  it('wraps the hero in a golden aura on level up', () => {
    const turn = { id: 11, enemiesBefore: [], events: [event('LeveledUp', { level: 2 })] }
    const cues = cuesForTurn(turn, [], player, sword, undefined)

    expect(cues.bursts).toMatchObject([{ kind: 'levelup', x: 5, y: 5 }])
  })
})
