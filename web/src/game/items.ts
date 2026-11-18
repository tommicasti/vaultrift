import type { Element, EquipmentSlot, ItemCategory, ItemInfo, StatModifierInfo, StatType } from '../api/types'
import { sheets, type SpriteRef } from './sprites'

const categorySprites: Record<ItemCategory, SpriteRef> = {
  Sword: { sheet: sheets.dungeon, index: 104 },
  Axe: { sheet: sheets.dungeon, index: 119 },
  Bow: { sheet: sheets.town, index: 118 },
  Dagger: { sheet: sheets.dungeon, index: 103 },
  Head: { sheet: sheets.items, index: 0 },
  Body: { sheet: sheets.items, index: 1 },
  Feet: { sheet: sheets.items, index: 2 },
  Ring: { sheet: sheets.items, index: 3 },
  Cloak: { sheet: sheets.items, index: 30 },
  Potion: { sheet: sheets.dungeon, index: 115 },
}

const potionSprites: Record<string, number> = {
  'potion-healing': 127,
  'potion-greater-healing': 115,
  'potion-strength': 114,
  'potion-protection': 116,
}

const legendarySprites: Record<string, number> = {
  'sword-vaultbreaker': 5,
  'sword-twinfang': 6,
  'sword-ember-oath': 7,
  'axe-riftsplitter': 8,
  'axe-grudgebearer': 9,
  'axe-stormmaw': 10,
  'bow-starshot': 11,
  'bow-nightglass': 12,
  'bow-galecaller': 13,
  'head-crown-of-echoes': 14,
  'head-mask-of-the-hollow': 15,
  'head-seers-visor': 16,
  'body-aegis-of-the-rift': 17,
  'body-bramblehide': 18,
  'body-lifewell-plate': 19,
  'feet-riftwalkers': 20,
  'feet-ashstriders': 21,
  'feet-galeheels': 22,
  'ring-band-of-second-dawn': 23,
  'ring-ouroboros': 24,
  'ring-gilded-grasp': 25,
  'cloak-cinderweave': 31,
  'cloak-rimeveil': 32,
  'cloak-stormshroud': 33,
  'dagger-whisperthorn': 38,
  'dagger-last-breath': 39,
  'dagger-gutterstar': 40,
}

export function itemSprite(item: ItemInfo): SpriteRef {
  if (item.id in legendarySprites) return { sheet: sheets.items, index: legendarySprites[item.id] }
  if (item.id in potionSprites) return { sheet: sheets.dungeon, index: potionSprites[item.id] }
  return categorySprites[item.category]
}

export const slotLabels: Record<EquipmentSlot, string> = {
  Weapon: 'Weapon',
  Head: 'Head',
  Body: 'Body',
  Feet: 'Feet',
  Ring1: 'Ring',
  Ring2: 'Ring',
  Cloak: 'Cloak',
}

const statLabels: Record<StatType, string> = {
  Attack: 'attack',
  Defense: 'defense',
  Evasion: 'evasion',
  Critical: 'critical',
}

export function describeModifier(modifier: StatModifierInfo): string {
  const suffix = modifier.stat === 'Evasion' || modifier.stat === 'Critical' ? '%' : ''
  return `+${modifier.amount}${suffix} ${statLabels[modifier.stat]}`
}

export const elementNames: Record<Element, string> = {
  Fire: 'fire',
  Ice: 'ice',
  Lightning: 'lightning',
  Wind: 'wind',
}

const elementEffects: Record<Element, string> = {
  Fire: 'burn',
  Ice: 'freeze',
  Lightning: 'chain',
  Wind: 'knockback',
}
const weaponTraits: Partial<Record<ItemCategory, string>> = {
  Axe: 'Sweeps the target and both tiles beside it',
  Bow: 'Shoots up to 6 tiles · half damage up close',
}

export function describeWeaponTrait(item: ItemInfo): string | null {
  return weaponTraits[item.category] ?? null
}
export function describeElement(item: ItemInfo): string | null {
  if (!item.element) return null
  if (item.category === 'Cloak') return `Resists ${elementNames[item.element]}`
  return `${capitalize(elementNames[item.element])} · ${item.effectChance}% ${elementEffects[item.element]}`
}

export function describeItem(item: ItemInfo): string {
  if (item.healing) return `Restores ${item.healing} health`
  if (item.boost) return `${describeModifier(item.boost)} for ${item.boostTurns} turns`
  return item.modifiers.map(describeModifier).join(' · ')
}

export function capitalize(text: string): string {
  return text.charAt(0).toUpperCase() + text.slice(1)
}