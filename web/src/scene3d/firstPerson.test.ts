import { describe, expect, it } from 'vitest'
import { relativeIntent, towardFacing, turned } from './firstPerson'

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
})
