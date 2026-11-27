import { describe, expect, it } from 'vitest'
import { compassSpot } from './compass'

describe('compassSpot', () => {
  it('hides the marker when the hero is on screen', () => {
    expect(compassSpot(0.2, -0.4, false, 800, 600).visible).toBe(false)
  })

  it('pins a hero off to the right on the right edge', () => {
    const spot = compassSpot(3, 0, false, 800, 600)
    expect(spot.visible).toBe(true)
    expect(spot.x).toBeCloseTo(760)
    expect(spot.y).toBeCloseTo(300)
    expect(spot.angle).toBeCloseTo(0)
  })

  it('points down for a hero below the view', () => {
    const spot = compassSpot(0, -5, false, 800, 600)
    expect(spot.y).toBeGreaterThan(500)
    expect(spot.angle).toBeCloseTo(Math.PI / 2)
  })

  it('flips the direction of a hero behind the camera', () => {
    const spot = compassSpot(0.5, 0, true, 800, 600)
    expect(spot.visible).toBe(true)
    expect(spot.x).toBeLessThan(400)
  })
})
