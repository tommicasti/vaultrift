import { describe, expect, it } from 'vitest'
import { enemyAhead } from './aim'

const hero = { x: 5, y: 5, reach: 1.2, arcDegrees: 90 }
const rat = (x: number, y: number, health = 10) => ({ x, y, radius: 0.35, health })

describe('enemyAhead', () => {
  it('sees a living enemy in front within reach', () => {
    expect(enemyAhead(hero, 0, [rat(5, 6)])).toBe(true)
  })

  it('ignores enemies behind, too far or already dead', () => {
    expect(enemyAhead(hero, 0, [rat(5, 4)])).toBe(false)
    expect(enemyAhead(hero, 0, [rat(5, 8)])).toBe(false)
    expect(enemyAhead(hero, 0, [rat(5, 6, 0)])).toBe(false)
  })

  it('turns with the gaze', () => {
    expect(enemyAhead(hero, Math.PI / 2, [rat(6, 5)])).toBe(true)
    expect(enemyAhead(hero, Math.PI / 2, [rat(5, 6)])).toBe(false)
  })

  it('keeps a narrow cone for archers but a long reach', () => {
    const archer = { x: 5, y: 5, reach: 6, arcDegrees: 0 }
    expect(enemyAhead(archer, 0, [rat(5.5, 10)])).toBe(true)
    expect(enemyAhead(archer, 0, [rat(8, 8)])).toBe(false)
  })
})
