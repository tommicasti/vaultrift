import { describe, expect, it } from 'vitest'
import { patchMap } from './mapPatch'

describe('patchMap', () => {
  it('changes only the listed tiles', () => {
    expect(patchMap(['#####', '#...#', '#####'], [[1, 1], [3, 1]], 'L')).toEqual(['#####', '#L.L#', '#####'])
  })

  it('ignores tiles outside the map', () => {
    expect(patchMap(['...'], [[5, 0], [0, 3]], 'C')).toEqual(['...'])
  })

  it('leaves the original rows untouched', () => {
    const rows = ['...']
    patchMap(rows, [[0, 0]], 'C')
    expect(rows).toEqual(['...'])
  })
})
