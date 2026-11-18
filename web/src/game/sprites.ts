import type { CSSProperties } from 'react'
import type { Element, Rarity } from '../api/types'

export interface SpriteSheet {
  src: string
  columns: number
  rows: number
}

export interface SpriteRef {
  sheet: SpriteSheet
  index: number
}

export const sheets = {
  dungeon: { src: '/assets/tilesets/tiny-dungeon.png', columns: 12, rows: 11 },
  town: { src: '/assets/tilesets/tiny-town.png', columns: 12, rows: 11 },
  items: { src: '/assets/tilesets/vaultrift-items.png', columns: 8, rows: 9 },
  world: { src: '/assets/tilesets/vaultrift-world.png', columns: 8, rows: 1 },
  heroes: { src: '/assets/tilesets/vaultrift-heroes.png', columns: 8, rows: 1 },
  vault: { src: '/assets/tilesets/vaultrift-dungeon.png', columns: 8, rows: 2 },
  outfits: { src: '/assets/tilesets/vaultrift-outfits.png', columns: 8, rows: 6 },
  biomes: { src: '/assets/tilesets/vaultrift-biomes.png', columns: 8, rows: 6 },
} satisfies Record<string, SpriteSheet>

const classSprites: Record<string, SpriteRef> = {
  knight: { sheet: sheets.dungeon, index: 96 },
  berserker: { sheet: sheets.dungeon, index: 87 },
  ranger: { sheet: sheets.dungeon, index: 112 },
  rogue: { sheet: sheets.heroes, index: 0 },
  mage: { sheet: sheets.dungeon, index: 84 },
}

const defaultClassSprite = classSprites.knight

export function classSprite(classId?: string): SpriteRef {
  return classId !== undefined && classId in classSprites ? classSprites[classId] : defaultClassSprite
}

const enemySprites: Record<string, SpriteRef> = {
  rat: { sheet: sheets.dungeon, index: 123 },
  bat: { sheet: sheets.dungeon, index: 120 },
  spider: { sheet: sheets.dungeon, index: 122 },
  slime: { sheet: sheets.dungeon, index: 108 },
  ghost: { sheet: sheets.dungeon, index: 121 },
  cyclops: { sheet: sheets.dungeon, index: 109 },
  'rat-king': { sheet: sheets.vault, index: 0 },
  'spider-queen': { sheet: sheets.vault, index: 1 },
  troll: { sheet: sheets.vault, index: 2 },
  'dune-scorpion': { sheet: sheets.vault, index: 3 },
  'frost-yeti': { sheet: sheets.vault, index: 4 },
  'black-knight': { sheet: sheets.vault, index: 5 },
  mimic: { sheet: sheets.dungeon, index: 92 },
}

const defaultEnemySprite = enemySprites.rat

export function enemySprite(kind: string): SpriteRef {
  return kind in enemySprites ? enemySprites[kind] : defaultEnemySprite
}

export const cloakSprites: Record<Element | 'Plain', number> = {
  Fire: 0,
  Ice: 1,
  Lightning: 2,
  Wind: 3,
  Plain: 4,
}

export const helmetSprites: Record<Rarity, number> = {
  Common: 8,
  Rare: 9,
  Epic: 10,
  Legendary: 11,
}

export interface LegendaryOutfit {
  column: number
  glow: string
}

export const legendaryOutfits: Record<string, LegendaryOutfit> = {
  'body-aegis-of-the-rift': { column: 0, glow: 'aegis' },
  'body-bramblehide': { column: 1, glow: 'bramble' },
  'body-lifewell-plate': { column: 2, glow: 'lifewell' },
}

const outfitRows: Record<string, number> = {
  knight: 2,
  berserker: 3,
  ranger: 4,
  rogue: 5,
}

export function outfitSprite(classId: string | undefined, bodyItemId: string | undefined): SpriteRef | null {
  if (classId === undefined || bodyItemId === undefined) return null

  const outfit = legendaryOutfits[bodyItemId]
  const row = outfitRows[classId]

  if (!outfit || row === undefined) return null

  return { sheet: sheets.outfits, index: row * sheets.outfits.columns + outfit.column }
}

export const crownSprite = 29

export const chestSprite = { sheet: sheets.dungeon, index: 89 }

export const stairsSprites = {
  open: 8,
  sealed: 9,
}

export const rarityStars: Record<Rarity, number> = {
  Common: 0,
  Rare: 1,
  Epic: 2,
  Legendary: 3,
}

export const starSprites: Record<Rarity, number> = {
  Common: 26,
  Rare: 26,
  Epic: 27,
  Legendary: 28,
}

export const elementSprites: Record<Element, number> = {
  Fire: 34,
  Ice: 35,
  Lightning: 36,
  Wind: 37,
}

function axisPercent(index: number, count: number): number {
  return count === 1 ? 0 : (index / (count - 1)) * 100
}

export function spriteStyle(sheet: SpriteSheet, index: number): CSSProperties {
  const column = index % sheet.columns
  const row = Math.floor(index / sheet.columns)

  return {
    backgroundImage: `url(${sheet.src})`,
    backgroundSize: `${sheet.columns * 100}% ${sheet.rows * 100}%`,
    backgroundPosition: `${axisPercent(column, sheet.columns)}% ${axisPercent(row, sheet.rows)}%`,
  }
}