import { describe, expect, it } from 'vitest'
import { aimToward, joystickVector, moveVector, sameInput } from './input'
import { ClockOffset, sampleFrames, type Frame, type Placed } from './interpolation'

describe('live input', () => {
  it('turns pressed keys into a direction', () => {
    expect(moveVector(new Set(['KeyD']))).toEqual({ x: 1, y: 0 })
    expect(moveVector(new Set(['ArrowUp']))).toEqual({ x: 0, y: -1 })
  })

  it('keeps diagonals as fast as straight lines', () => {
    const { x, y } = moveVector(new Set(['KeyW', 'KeyD']))
    expect(Math.hypot(x, y)).toBeCloseTo(1)
    expect(x).toBeGreaterThan(0)
    expect(y).toBeLessThan(0)
  })

  it('cancels opposite keys', () => {
    expect(moveVector(new Set(['KeyA', 'KeyD']))).toEqual({ x: 0, y: 0 })
  })

  it('notices only real changes', () => {
    const still = { x: 1, y: 0, attack: false, aimX: 0, aimY: 1 }
    expect(sameInput(still, { ...still })).toBe(true)
    expect(sameInput(still, { ...still, attack: true })).toBe(false)
    expect(sameInput(still, { ...still, aimX: 0.02 })).toBe(true)
    expect(sameInput(still, { ...still, aimX: 0.5, aimY: 0.86 })).toBe(false)
  })

  it('aims from the hero toward the pointer', () => {
    const { aimX, aimY } = aimToward({ x: 2, y: 2 }, { x: 5, y: 6 })
    expect(aimX).toBeCloseTo(0.6)
    expect(aimY).toBeCloseTo(0.8)
    expect(aimToward({ x: 2, y: 2 }, null)).toEqual({ aimX: 0, aimY: 0 })
    expect(aimToward({ x: 2, y: 2 }, { x: 2.05, y: 2 })).toEqual({ aimX: 0, aimY: 0 })
  })
})

describe('interpolation', () => {
  const frame = (time: number, x: number): Frame<Placed & { id: number }> => ({ time, items: new Map([[1, { id: 1, x, y: 2, moving: true }]]) })

  it('blends between the two frames around the render time', () => {
    const sample = sampleFrames([frame(1, 0), frame(1.05, 1)], 1, 1.025)
    expect(sample?.x).toBeCloseTo(0.5)
    expect(sample?.y).toBe(2)
  })

  it('keeps moving for a moment when a snapshot is late, then stops', () => {
    expect(sampleFrames([frame(1, 0), frame(1.05, 1)], 1, 1.1)?.x).toBeCloseTo(2)
    expect(sampleFrames([frame(1, 0), frame(1.05, 1)], 1, 9)?.x).toBeCloseTo(1 + 0.12 / 0.05)
  })

  it('knows nothing about missing entities', () => {
    expect(sampleFrames([frame(1, 0)], 7, 1)).toBeUndefined()
  })
})

describe('clock offset', () => {
  it('ignores packets that arrive late', () => {
    const clock = new ClockOffset()
    const jitter = [0, 0.015, 0.004, 0.03, 0.002, 0.02, 0.011, 0]

    for (let tick = 0; tick < 80; tick++) clock.observe(tick * 0.05, (10 + tick * 0.05 + jitter[tick % jitter.length]) * 1000)

    expect(clock.value).toBeCloseTo(10, 3)
  })

  it('moves gently when the network gets slower', () => {
    const clock = new ClockOffset()
    for (let tick = 0; tick < 40; tick++) clock.observe(tick * 0.05, (10 + tick * 0.05) * 1000)
    for (let tick = 40; tick < 41; tick++) clock.observe(tick * 0.05, (10.05 + tick * 0.05) * 1000)

    expect(clock.value).toBeCloseTo(10, 3)
  })
})

describe('joystick', () => {
  it('ignores tiny wobbles around the centre', () => {
    expect(joystickVector(3, -2, 56)).toMatchObject({ x: 0, y: 0 })
  })

  it('scales with the distance and stops at the rim', () => {
    expect(joystickVector(28, 0, 56).x).toBeCloseTo(0.5)
    const far = joystickVector(0, 200, 56)
    expect(far.y).toBeCloseTo(1)
    expect(far.offsetY).toBeCloseTo(56)
  })
})
