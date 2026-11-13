import type { EquipmentSlot, ItemCategory, ItemInfo, StatModifierInfo, StatType } from '../api/types'

export const categoryIcons: Record<ItemCategory, string> = {
  Sword: '🗡️',
  Axe: '🪓',
  Bow: '🏹',
  Head: '🪖',
  Body: '🛡️',
  Feet: '🥾',
  Ring: '💍',
  Potion: '🧪',
}

export const slotLabels: Record<EquipmentSlot, string> = {
  Weapon: 'Weapon',
  Head: 'Head',
  Body: 'Body',
  Feet: 'Feet',
  Ring1: 'Ring',
  Ring2: 'Ring',
}

const statLabels: Record<StatType, string> = {
  Attack: 'attack',
  Defense: 'defense',
  Evasion: 'evasion',
}

export function describeModifier(modifier: StatModifierInfo): string {
  const suffix = modifier.stat === 'Evasion' ? '%' : ''
  return `+${modifier.amount}${suffix} ${statLabels[modifier.stat]}`
}

export function describeItem(item: ItemInfo): string {
  if (item.healing) return `Restores ${item.healing} health`
  if (item.boost) return `${describeModifier(item.boost)} for ${item.boostTurns} turns`
  return item.modifiers.map(describeModifier).join(' · ')
}