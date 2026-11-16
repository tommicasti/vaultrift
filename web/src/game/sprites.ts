import type { CSSProperties } from 'react'
import type { Rarity } from '../api/types'

export interface SpriteSheet {
  src: string
  columns: number
  rows: number
}

export const sheets = {
  dungeon: { src: '/assets/tilesets/tiny-dungeon.png', columns: 12, rows: 11 },
  town: { src: '/assets/tilesets/tiny-town.png', columns: 12, rows: 11 },
  items: { src: '/assets/tilesets/vaultrift-items.png', columns: 8, rows: 4 },
  world: { src: '/assets/tilesets/vaultrift-world.png', columns: 8, rows: 1 },
} satisfies Record<string, SpriteSheet>

const classSprites: Record<string, number> = {
  knight: 96,
  berserker: 87,
  ranger: 112,
  rogue: 111,
  mage: 84,
}

const defaultClassSprite = 96

export function classSprite(classId?: string): number {
  return classId !== undefined && classId in classSprites ? classSprites[classId] : defaultClassSprite
}

const enemySprites: Record<string, number> = {
  rat: 123,
  bat: 120,
  spider: 122,
  slime: 108,
  ghost: 121,
  cyclops: 109,
}

const defaultEnemySprite = 123

export function enemySprite(kind: string): number {
  return kind in enemySprites ? enemySprites[kind] : defaultEnemySprite
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