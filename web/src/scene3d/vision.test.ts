import { describe, expect, it } from 'vitest'
import { visibleTiles } from './vision'

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
