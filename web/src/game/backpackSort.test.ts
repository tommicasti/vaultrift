import { describe, expect, it } from 'vitest'
import type { Rarity } from '../api/types'
import { byRarityThenName } from './backpackSort'

const stored = (name: string, rarity: Rarity) => ({ item: { name, rarity } })

describe('byRarityThenName', () => {
  it('puts the rarest first and orders each rarity by name', () => {
    const sorted = byRarityThenName([stored('Worn Boots', 'Common'), stored('Zanne del Drago', 'Platinum'), stored('Amber Ring', 'Common'), stored('Gale Ring', 'Rare')])
    expect(sorted.map(entry => entry.item.name)).toEqual(['Zanne del Drago', 'Gale Ring', 'Amber Ring', 'Worn Boots'])
  })
})
