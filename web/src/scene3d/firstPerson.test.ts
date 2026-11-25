import { describe, expect, it } from 'vitest'
import { facingAngles, relativeIntent, steeredFacing, towardFacing, turned, wrapAngle } from './firstPerson'

describe('first person controls', () => {
  it('walks forward along the facing', () => {
    expect(relativeIntent('Up', 'Left')).toEqual({ kind: 'move', direction: 'Left' })
  })

  it('turns left, right and around without moving', () => {
    expect(relativeIntent('Left', 'Up')).toEqual({ kind: 'face', direction: 'Left' })
    expect(relativeIntent('Right', 'Up')).toEqual({ kind: 'face', direction: 'Right' })
    expect(relativeIntent('Down', 'Right')).toEqual({ kind: 'face', direction: 'Left' })
  })

  it('wraps around the compass', () => {
    expect(turned('Left', 1)).toBe('Up')
    expect(turned('Up', -1)).toBe('Left')
  })

  it('maps the facing direction to straight ahead on screen', () => {
    expect(towardFacing({ x: 3, y: 0 }, 'Right')).toEqual({ x: 0, y: -3 })
    expect(towardFacing({ x: 1, y: 2 }, 'Down')).toEqual({ x: -1, y: -2 })
    expect(towardFacing({ x: -1, y: 2 }, 'Left')).toEqual({ x: -2, y: -1 })
  })

  it('keeps the facing while the mouse looks only a little aside', () => {
    expect(steeredFacing(facingAngles.Up + 0.6, 'Up')).toBe('Up')
    expect(steeredFacing(facingAngles.Down - 0.8, 'Down')).toBe('Down')
  })

  it('turns toward the direction the mouse looks at once past the threshold', () => {
    expect(steeredFacing(facingAngles.Down + 0.95, 'Down')).toBe('Right')
    expect(steeredFacing(facingAngles.Down - 0.95, 'Down')).toBe('Left')
    expect(steeredFacing(facingAngles.Up + 0.1, 'Down')).toBe('Up')
  })

  it('turns across the back of the compass', () => {
    expect(steeredFacing(Math.PI * 1.5 + 0.1, 'Up')).toBe('Left')
    expect(steeredFacing(-Math.PI * 1.5 - 0.1, 'Up')).toBe('Right')
  })

  it('wraps angles into one turn', () => {
    expect(wrapAngle(Math.PI * 2 + 0.5)).toBeCloseTo(0.5)
    expect(wrapAngle(-Math.PI * 2 - 0.5)).toBeCloseTo(-0.5)
  })
})
