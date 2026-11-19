import type {
  ClassAbility,
  SpellInfo,
  SpellShape,
  EffectKind,
  Element,
  EquipmentSlot,
  ItemCategory,
  ItemEffectInfo,
  ItemInfo,
  StatModifierInfo,
  StatType,
} from '../api/types'
import { sheets, type SpriteRef } from './sprites'

const categorySprites: Record<ItemCategory, SpriteRef> = {
  Sword: { sheet: sheets.dungeon, index: 104 },
  Axe: { sheet: sheets.dungeon, index: 119 },
  Bow: { sheet: sheets.town, index: 118 },
  Dagger: { sheet: sheets.dungeon, index: 103 },
  Spear: { sheet: sheets.items, index: 41 },
  Shield: { sheet: sheets.items, index: 42 },
  Head: { sheet: sheets.items, index: 0 },
  Body: { sheet: sheets.items, index: 1 },
  Feet: { sheet: sheets.items, index: 2 },
  Ring: { sheet: sheets.items, index: 3 },
  Cloak: { sheet: sheets.items, index: 30 },
  ClassMod: { sheet: sheets.items, index: 43 },
  Potion: { sheet: sheets.dungeon, index: 115 },
  Staff: { sheet: sheets.items, index: 66 },
  Scroll: { sheet: sheets.items, index: 73 },
}

const elementStaffSprites: Record<Element, number> = {
  Fire: 66,
  Ice: 67,
  Lightning: 68,
  Wind: 69,
}

const elementScrollSprites: Record<Element, number> = {
  Fire: 73,
  Ice: 74,
  Lightning: 75,
  Wind: 76,
}

const potionSprites: Record<string, SpriteRef> = {
  'potion-healing': { sheet: sheets.dungeon, index: 127 },
  'potion-greater-healing': { sheet: sheets.dungeon, index: 115 },
  'potion-strength': { sheet: sheets.dungeon, index: 114 },
  'potion-protection': { sheet: sheets.dungeon, index: 116 },
  'potion-mana': { sheet: sheets.items, index: 47 },
  'potion-greater-mana': { sheet: sheets.items, index: 65 },
}

const classModSprites: Record<string, number> = {
  knight: 43,
  berserker: 44,
  ranger: 45,
  rogue: 46,
  mage: 77,
}

const uniqueSprites: Record<string, number> = {
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
  'ring-ember': 48,
  'ring-frost': 49,
  'ring-storm': 50,
  'ring-gale': 51,
  'ring-thorns': 52,
  'ring-hunter': 53,
  'ring-flow': 54,
  'ring-vampire': 55,
  'ring-fortune': 56,
  'ring-sage': 57,
  'ring-time': 58,
  'spear-skypiercer': 59,
  'spear-tidebreaker': 60,
  'spear-sunlance': 61,
  'shield-dawnwall': 62,
  'shield-mirrorwall': 63,
  'shield-stormguard': 64,
  'staff-riftheart': 70,
  'staff-everflame': 71,
  'staff-whirlwind': 72,
}

export function itemSprite(item: ItemInfo): SpriteRef {
  if (item.id in uniqueSprites) return { sheet: sheets.items, index: uniqueSprites[item.id] }
  if (item.id in potionSprites) return potionSprites[item.id]
  if (item.category === 'Staff' && item.spell) return { sheet: sheets.items, index: elementStaffSprites[item.spell.element] }
  if (item.category === 'Scroll' && item.spell) return { sheet: sheets.items, index: elementScrollSprites[item.spell.element] }
  if (item.classId !== undefined && item.classId in classModSprites)
    return { sheet: sheets.items, index: classModSprites[item.classId] }
  return categorySprites[item.category]
}

export const slotLabels: Record<EquipmentSlot, string> = {
  MainHand: 'Main hand',
  OffHand: 'Off hand',
  Head: 'Head',
  Body: 'Body',
  Feet: 'Feet',
  Ring1: 'Ring',
  Ring2: 'Ring',
  Cloak: 'Cloak',
  ClassMod: 'Class mod',
}

const statLabels: Record<StatType, string> = {
  Attack: 'attack',
  Defense: 'defense',
  Evasion: 'evasion',
  Critical: 'critical',
  Block: 'block',
  Mana: 'mana',
}

const percentStats: StatType[] = ['Evasion', 'Critical', 'Block']

export function describeModifier(modifier: StatModifierInfo): string {
  const suffix = percentStats.includes(modifier.stat) ? '%' : ''
  return `+${modifier.amount}${suffix} ${statLabels[modifier.stat]}`
}

const effectDescriptions: Record<EffectKind, (amount: number) => string> = {
  FireBoost: amount => `+${amount}% fire chance, burns hotter`,
  IceBoost: amount => `+${amount}% freeze chance, freezes longer`,
  LightningBoost: amount => `+${amount}% lightning chance, jumps one more enemy`,
  WindBoost: amount => `+${amount}% wind chance, pushes two tiles`,
  LifeSteal: amount => `Heals ${amount}% of the damage you deal`,
  Thorns: amount => `Attackers take ${amount} damage`,
  DoubleShot: amount => `${amount}% chance to loose a second arrow`,
  Luck: amount => `+${amount}% chance of loot`,
  Wisdom: amount => `+${amount}% experience`,
  ManaFlow: amount => `+${amount} mana every regeneration`,
  Haste: amount => `Abilities recharge ${amount}% faster (40% at most)`,
  Regeneration: amount => `Regenerate ${amount} health every turn`,
  BulwarkExtension: amount => `Bulwark lasts ${amount} more ${amount === 1 ? 'turn' : 'turns'}`,
  BlockReflect: () => 'Blocked blows hit the attacker',
  FuryLifeSteal: amount => `During Fury, heal ${amount}% of your damage`,
  FuryRegeneration: amount => `+${amount} regeneration during Fury`,
  FuryExtension: amount => `Every kill during Fury extends it by ${amount}`,
  FarShotCritical: amount => `+${amount}% critical from 4 or more tiles`,
  PowerShotMastery: () => 'Power Shot recharges 30% faster, element guaranteed',
  Stealth: amount => `Monsters wake up ${amount} ${amount === 1 ? 'tile' : 'tiles'} later`,
  Ambush: () => 'Strikes from invisibility deal triple damage',
  Phantom: amount => `Invisibility +${amount} turns, kills keep you hidden`,
  ManaSiphon: amount => `Every kill gives back ${amount} mana`,
  SpellMastery: amount => `Spells cost 1 less mana, +${amount}% element chance`,
}

export function describeEffect(effect: ItemEffectInfo): string {
  return effectDescriptions[effect.kind](effect.amount)
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

export function describeElement(item: ItemInfo): string | null {
  if (!item.element) return null
  if (item.category === 'Cloak') return `Resists ${elementNames[item.element]}`
  return `${capitalize(elementNames[item.element])} · ${item.effectChance}% ${elementEffects[item.element]}`
}

export const classNames: Record<string, string> = {
  knight: 'Knight',
  berserker: 'Berserker',
  ranger: 'Ranger',
  rogue: 'Rogue',
  mage: 'Mage',
}

const classAbilities: Record<string, ClassAbility> = {
  knight: 'Bulwark',
  berserker: 'Fury',
  ranger: 'PowerShot',
  rogue: 'Invisibility',
}

export interface ItemKind {
  name: string
  hint: string
}

const twinHint = 'Pair it with a shield or a second weapon'

const itemKinds: Record<Exclude<ItemCategory, 'ClassMod'>, ItemKind> = {
  Staff: { name: 'Two-handed staff', hint: 'Attack casts the selected spell · 6 tiles · no penalty up close' },
  Scroll: { name: 'Scroll', hint: 'Cast it once, or learn it for good while holding a staff' },
  Sword: { name: 'One-handed sword', hint: twinHint },
  Dagger: { name: 'One-handed dagger', hint: twinHint },
  Axe: { name: 'Two-handed axe', hint: 'Sweeps the target and both tiles beside it' },
  Bow: { name: 'Two-handed bow', hint: 'Shoots up to 6 tiles · half damage up close' },
  Spear: { name: 'Two-handed spear', hint: 'Hits the enemy in front and the one behind it' },
  Shield: { name: 'Shield', hint: 'Goes in the off hand · a blocked blow deals no damage' },
  Head: { name: 'Helmet', hint: 'Goes in the head slot' },
  Body: { name: 'Armor', hint: 'Goes in the body slot' },
  Feet: { name: 'Boots', hint: 'Goes in the feet slot' },
  Ring: { name: 'Ring', hint: 'You can wear two rings at once' },
  Cloak: { name: 'Cloak', hint: 'Goes in the cloak slot' },
  Potion: { name: 'Potion', hint: 'Used up when you drink it' },
}

export function describeKind(item: ItemInfo): ItemKind {
  if (item.category !== 'ClassMod') return itemKinds[item.category]
  const ability = item.classId !== undefined ? classAbilities[item.classId] : undefined
  const target = ability ? abilityNames[ability] : 'the class ability'
  return {
    name: 'Class mod',
    hint: `One at a time in the class mod slot · strengthens ${target}`,
  }
}

export function describeItem(item: ItemInfo): string {
  if (item.healing) return `Restores ${item.healing} health`
  if (item.manaRestore) return `Restores ${item.manaRestore} mana`
  if (item.boost) return `${describeModifier(item.boost)} for ${item.boostTurns} turns`
  return item.modifiers.map(describeModifier).join(' · ')
}

export const abilityNames: Record<ClassAbility, string> = {
  Bulwark: 'Bulwark',
  Fury: 'Fury',
  PowerShot: 'Power Shot',
  Invisibility: 'Invisibility',
  Overload: 'Overload',
}

export const abilityDescriptions: Record<ClassAbility, string> = {
  Bulwark: 'For 3 turns every blow is halved, and stopped cold with a shield.',
  Fury: 'For 5 turns your attack rises by half.',
  PowerShot: 'With a bow, a double damage arrow that pierces every enemy in a line. With any other weapon, a double damage blow.',
  Invisibility: 'For 5 turns monsters cannot see you. Your first strike is always critical.',
  Overload: 'Your next spell costs no mana and its area grows by one tile. A bolt becomes an explosion.',
}

const shapeDescriptions: Record<SpellShape, (spell: SpellInfo) => string> = {
  Bolt: () => 'hits the first enemy in line',
  Burst: spell =>
    spell.element === 'Wind'
      ? `whirls on a ${spell.size * 2 + 1}×${spell.size * 2 + 1} area and pulls enemies in`
      : `explodes on a ${spell.size * 2 + 1}×${spell.size * 2 + 1} area`,
  Line: spell => `hits the whole row and pushes ${spell.size} tiles`,
  Chain: spell => `jumps between up to ${spell.size} enemies`,
  Nova: spell => `strikes every enemy within ${spell.size} ${spell.size === 1 ? 'tile' : 'tiles'} of you`,
  Storm: spell => `strikes every enemy within ${spell.size} tiles of you`,
}

export function describeSpell(spell: SpellInfo): string {
  return `${spell.name} · ${shapeDescriptions[spell.shape](spell)} · +${spell.power} power · ${spell.manaCost} mana`
}

export const quickSlotKeys = ['Q', 'R']

export function capitalize(text: string): string {
  return text.charAt(0).toUpperCase() + text.slice(1)
}
