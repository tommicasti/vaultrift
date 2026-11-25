import { afterEach, describe, expect, it, vi } from 'vitest'
import { layoutKey, remember, visibleTiles } from './vision'

const room = [
  '##########',
  '#....#...#',
  '#....#...#',
  '#....+...#',
  '##.#######',
  '##.#######',
  '##.#######',
].map(row => row.replace('+', '.'))

describe('visibleTiles', () => {
  it('reveals the whole room the player stands in', () => {
    const visible = visibleTiles(room, 1, 1, 1)

    expect(visible(4, 3)).toBe(true)
    expect(visible(1, 3)).toBe(true)
  })

  it('does not see through walls', () => {
    const visible = visibleTiles(room, 1, 1, 10)

    expect(visible(7, 1)).toBe(false)
  })

  it('limits sight in a corridor to the radius', () => {
    const corridor = ['#####', '#...#', '#.###', '#.###', '#.###', '#.###', '#.###', '#####']
    const visible = visibleTiles(corridor, 1, 2, 3)

    expect(visible(1, 5)).toBe(true)
    expect(visible(1, 6)).toBe(false)
  })
})

describe('explored memory', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('keeps the same key when only obstacles or lava change', () => {
    const smashed = room.map(row => row.replace('....', '.o..'))

    expect(layoutKey('game', 3, smashed)).toBe(layoutKey('game', 3, room))
    expect(layoutKey('game', 4, room)).not.toBe(layoutKey('game', 3, room))
  })

  it('changes the key when the floor is generated again with other walls', () => {
    const rebuilt = room.map((row, y) => (y === 2 ? '#.##.#...#' : row))

    expect(layoutKey('game', 3, rebuilt)).not.toBe(layoutKey('game', 3, room))
  })

  it('remembers explored tiles across page loads', () => {
    const storage = new Map<string, string>()
    vi.stubGlobal('localStorage', {
      getItem: (key: string) => storage.get(key) ?? null,
      setItem: (key: string, value: string) => void storage.set(key, value),
      removeItem: (key: string) => void storage.delete(key),
      key: () => null,
      length: 0,
    })

    const key = layoutKey('saved', 1, room)
    remember(key, room, (x, y) => x === 2 && y === 1)
    for (let other = 0; other < 5; other++) remember(`other-${other}`, room, () => false)

    expect(remember(key, room, () => false).has('2,1')).toBe(true)
  })
})
