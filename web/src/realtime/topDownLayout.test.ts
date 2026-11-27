import { describe, expect, it } from 'vitest'
import { explore, exploredKey, meetBosses, sightFrom, tileSize } from './topDownLayout'

describe('tileSize', () => {
  it('fits the whole floor in the map view', () => {
    const size = tileSize('map', 1000, 600, 50, 30, 1)
    expect(size * 50).toBeLessThanOrEqual(1000)
    expect(size * 30).toBeLessThanOrEqual(600)
  })

  it('keeps tiles readable on small screens while playing', () => {
    expect(tileSize('play', 380, 800, 50, 30, 1)).toBeGreaterThanOrEqual(30)
  })
})

describe('explore', () => {
  const map = ['#########', '#...#...#', '#...#...#', '#########']

  it('reveals what the hero can see and stops at walls', () => {
    const explored = new Set<string>()
    expect(explore(explored, map, sightFrom(map, 2, 1))).toBe(true)
    expect(explored.has(exploredKey(3, 2))).toBe(true)
    expect(explored.has(exploredKey(4, 1))).toBe(true)
    expect(explored.has(exploredKey(6, 1))).toBe(false)
    expect(explore(explored, map, sightFrom(map, 2, 1))).toBe(false)
  })

  it('reveals the whole room the hero stands in', () => {
    const room = ['#'.repeat(32), '#' + '.'.repeat(30) + '#', '#' + '.'.repeat(30) + '#', '#'.repeat(32)]
    const explored = new Set<string>()
    explore(explored, room, sightFrom(room, 1, 1, 3))
    expect(explored.has(exploredKey(29, 2))).toBe(true)
  })
})

describe('meetBosses', () => {
  const map = ['###########', '#...#.....#', '#...#.....#', '#.........#', '###########']

  it('does not meet a boss behind a wall, even when it is close', () => {
    const met = new Set<number>()
    meetBosses(met, map, 2, 1, [{ id: 9, isBoss: true, x: 6, y: 1 }])
    expect(met.has(9)).toBe(false)
  })

  it('does not meet a boss in the same open area when it is far away', () => {
    const open = Array.from({ length: 5 }, () => '.'.repeat(40))
    const met = new Set<number>()
    meetBosses(met, open, 1, 2, [{ id: 9, isBoss: true, x: 30, y: 2 }])
    expect(met.has(9)).toBe(false)
  })

  it('does not meet a boss that is in sight but still off screen', () => {
    const open = Array.from({ length: 5 }, () => '.'.repeat(40))
    const met = new Set<number>()
    meetBosses(met, open, 1, 2, [{ id: 9, isBoss: true, x: 7, y: 2 }])
    expect(met.has(9)).toBe(false)
    meetBosses(met, open, 4, 2, [{ id: 9, isBoss: true, x: 7, y: 2 }])
    expect(met.has(9)).toBe(true)
  })

  it('meets a boss in sight and remembers it after it walks away', () => {
    const met = new Set<number>()
    meetBosses(met, map, 5, 3, [{ id: 9, isBoss: true, x: 6, y: 1 }, { id: 2, isBoss: false, x: 6, y: 2 }])
    meetBosses(met, map, 1, 1, [{ id: 9, isBoss: true, x: 8, y: 1 }])
    expect([...met]).toEqual([9])
  })
})
