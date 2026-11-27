import { describe, expect, it } from 'vitest'
import { hitsWall, Predictor, slide } from './prediction'

const room = ['#####', '#...#', '#...#', '#####']

describe('slide', () => {
  it('moves freely across the floor', () => {
    expect(slide(room, { x: 1, y: 1 }, 0.5, 0.5, 0.3)).toEqual({ x: 1.5, y: 1.5 })
  })

  it('stops at walls but keeps sliding along them', () => {
    const moved = slide(room, { x: 1, y: 1 }, -0.5, 0.4, 0.3)
    expect(moved.x).toBe(1)
    expect(moved.y).toBeCloseTo(1.4)
  })

  it('treats crystals and obstacles as solid', () => {
    expect(hitsWall(['C.'], 0.6, 0, 0.3)).toBe(true)
    expect(hitsWall(['o.'], 0.6, 0, 0.3)).toBe(true)
    expect(hitsWall(['L.'], 0.6, 0, 0.3)).toBe(false)
  })
})

const hall = ['##############################', '#............................#', '##############################']
const walk = (x: number) => ({ move: { x, y: 0 }, speed: 4, radius: 0.3, currentSpeed: 0 })

function simulate(roundTripMs: number, movingMs: number, totalMs: number) {
  const predictor = new Predictor()
  predictor.reset(1, 1)
  const frameMs = 1000 / 60
  const tickMs = 50
  const inputs: { at: number; moving: boolean }[] = []
  let serverX = 1
  let lastTick = 0
  let lowest = Infinity
  let stoppedAt: number | null = null
  for (let now = 0; now <= totalMs; now += frameMs) {
    const moving = now < movingMs
    inputs.push({ at: now, moving })
    predictor.advance(hall, walk(moving ? 1 : 0), frameMs / 1000, now)
    if (!moving && stoppedAt === null) stoppedAt = predictor.x
    if (stoppedAt !== null) lowest = Math.min(lowest, predictor.x)
    while (now - lastTick >= tickMs) {
      lastTick += tickMs
      const serverTime = lastTick - roundTripMs / 2
      const heard = [...inputs].reverse().find(input => input.at <= serverTime - roundTripMs / 2)
      if (heard?.moving) serverX += (4 * tickMs) / 1000
      predictor.reconcile({ x: serverX, y: 1 }, lastTick, roundTripMs)
    }
  }
  return { predictor, serverX, stoppedAt: stoppedAt ?? 0, lowest }
}

describe('corners', () => {
  const doorway = ['#####', '#...#', '##.##', '#...#', '#####']

  it('slides the hero around a corner into the opening', () => {
    let position = { x: 2.35, y: 1 }
    for (let step = 0; step < 40; step++) position = slide(doorway, position, 0, 0.05, 0.3, [], true)
    expect(position.x).toBeLessThan(2.2)
    expect(position.y).toBeGreaterThan(2)
  })

  it('stays put against a plain wall', () => {
    const position = slide(doorway, { x: 1.2, y: 0.8 }, 0, -0.05, 0.3, [], true)
    expect(position).toEqual({ x: 1.2, y: 0.8 })
  })
})

describe('solids', () => {
  it('stops at a fixture or a monster but lets the hero step away', () => {
    const solids = [{ x: 3, y: 1, radius: 0.35 }]
    const blocked = slide(hall, { x: 2.4, y: 1 }, 0.1, 0, 0.3, solids)
    expect(blocked.x).toBe(2.4)
    const away = slide(hall, { x: 2.4, y: 1 }, -0.1, 0, 0.3, solids)
    expect(away.x).toBeCloseTo(2.3)
  })

  it('keeps the predicted hero from walking into the waystone', () => {
    const predictor = new Predictor()
    predictor.reset(1, 1)
    for (let frame = 0; frame < 60; frame++)
      predictor.advance(hall, { ...walk(1), solids: [{ x: 3, y: 1, radius: 0.35 }] }, 1 / 60, frame * 16)
    expect(predictor.x).toBeLessThan(3 - 0.6)
  })
})

describe('Predictor', () => {
  it('moves the hero right away with the input', () => {
    const predictor = new Predictor()
    predictor.reset(1, 1)
    for (let frame = 0; frame < 30; frame++) predictor.advance(room, walk(1), 0.25 / 30, frame * 8)
    expect(predictor.x).toBeGreaterThan(1.8)
  })

  it('does not pull the hero back after stopping', () => {
    const { stoppedAt, lowest, predictor, serverX } = simulate(120, 1000, 2500)
    expect(lowest).toBeGreaterThan(stoppedAt - 0.08)
    expect(Math.abs(predictor.x - serverX)).toBeLessThan(0.1)
  })

  it('follows a server that disagrees, and snaps when far off', () => {
    const predictor = new Predictor()
    predictor.reset(1, 1)
    predictor.advance(room, walk(0), 0.016, 0)
    predictor.reconcile({ x: 1.5, y: 1 }, 10, 0)
    for (let frame = 1; frame < 30; frame++) predictor.advance(room, walk(0), 0.016, frame * 16)
    expect(predictor.x).toBeGreaterThan(1.1)
    expect(predictor.x).toBeLessThan(1.5)
    predictor.reconcile({ x: 3, y: 2 }, 500, 0)
    expect(predictor.x).toBe(3)
    expect(predictor.y).toBe(2)
  })

  it('drifts on currents like the server', () => {
    const river = ['#######', '#>>>..#', '#######']
    const predictor = new Predictor()
    predictor.reset(1, 1)
    for (let frame = 0; frame < 30; frame++) predictor.advance(river, { move: { x: 0, y: 0 }, speed: 4, radius: 0.3, currentSpeed: 3 }, 1 / 60, frame * 16)
    expect(predictor.x).toBeGreaterThan(1.5)
  })

  it('stays still before it knows where the hero is', () => {
    const predictor = new Predictor()
    predictor.advance(room, walk(1), 1, 0)
    expect(predictor.isReady).toBe(false)
  })
})
