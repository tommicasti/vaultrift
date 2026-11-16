import type { CSSProperties } from 'react'

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