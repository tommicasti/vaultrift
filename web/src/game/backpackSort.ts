import type { InventoryEntryState, ItemCategory, Rarity } from '../api/types'

export type BackpackSort = 'Recent' | 'Rarity' | 'Type'

export const backpackSorts: BackpackSort[] = ['Recent', 'Rarity', 'Type']

const rarityRank: Record<Rarity, number> = {
  Platinum: -2,
  Exotic: -1,
  Legendary: 0,
  Epic: 1,
  Rare: 2,
  Common: 3,
}

const categoryRank: Record<ItemCategory, number> = {
  Sword: 0,
  Dagger: 1,
  Axe: 2,
  Spear: 3,
  Bow: 4,
  Shield: 6,
  Head: 7,
  Body: 8,
  Feet: 9,
  Ring: 10,
  Cloak: 11,
  Staff: 5,
  ClassMod: 12,
  Potion: 13,
  Scroll: 14,
  Rune: 15,
}

const storageKey = 'vaultrift.backpackSort'

function byRarity(a: InventoryEntryState, b: InventoryEntryState): number {
  return rarityRank[a.item.rarity] - rarityRank[b.item.rarity]
}

function byCategory(a: InventoryEntryState, b: InventoryEntryState): number {
  return categoryRank[a.item.category] - categoryRank[b.item.category]
}

function byNewest(a: InventoryEntryState, b: InventoryEntryState): number {
  return b.id - a.id
}

const comparers: Record<BackpackSort, (a: InventoryEntryState, b: InventoryEntryState) => number> = {
  Recent: byNewest,
  Rarity: (a, b) => byRarity(a, b) || byCategory(a, b) || byNewest(a, b),
  Type: (a, b) => byCategory(a, b) || byRarity(a, b) || byNewest(a, b),
}

export function sortBackpack(entries: readonly InventoryEntryState[], sort: BackpackSort): InventoryEntryState[] {
  const favoritesFirst = (a: InventoryEntryState, b: InventoryEntryState) =>
    Number(b.mark === 'Favorite') - Number(a.mark === 'Favorite')

  return [...entries].sort((a, b) => favoritesFirst(a, b) || comparers[sort](a, b))
}

export function sortForSale(entries: readonly InventoryEntryState[]): InventoryEntryState[] {
  const markRank = (entry: InventoryEntryState) => (entry.mark === 'Junk' ? 0 : entry.mark === 'Favorite' ? 2 : 1)

  return [...entries].sort(
    (a, b) => markRank(a) - markRank(b) || byRarity(b, a) || byCategory(a, b) || byNewest(a, b),
  )
}

export function loadBackpackSort(): BackpackSort {
  try {
    const saved = localStorage.getItem(storageKey)
    return backpackSorts.find(sort => sort === saved) ?? 'Recent'
  } catch {
    return 'Recent'
  }
}

export function saveBackpackSort(sort: BackpackSort): void {
  try {
    localStorage.setItem(storageKey, sort)
  } catch {
    return
  }
}
