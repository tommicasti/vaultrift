import { sheets } from './sprites'

export type Terrain = 'lava' | 'water' | 'crystal' | 'spores' | 'current-up' | 'current-down' | 'current-left' | 'current-right'

export interface TileArt {
  base: number
  overlay?: number
  deep?: boolean
  terrain?: Terrain
}

const biomeRows: Record<string, number> = {
  crypt: 0,
  forest: 1,
  desert: 2,
  snow: 3,
  castle: 4,
  swamp: 5,
  volcano: 6,
  ruins: 7,
  mines: 8,
  fungal: 9,
  sky: 10,
  rift: 11,
}

export const biomeNames: Record<string, string> = {
  crypt: 'Crypt',
  forest: 'Forest',
  desert: 'Desert',
  snow: 'Frozen Peaks',
  castle: 'Castle',
  swamp: 'Swamp',
  volcano: 'Volcano',
  ruins: 'Sunken Ruins',
  mines: 'Crystal Mines',
  fungal: 'Fungal Grotto',
  sky: 'Sky Temple',
  rift: 'The Rift',
}

const terrainSymbols: Record<string, Terrain> = {
  L: 'lava',
  '~': 'water',
  C: 'crystal',
  S: 'spores',
  '^': 'current-up',
  v: 'current-down',
  '<': 'current-left',
  '>': 'current-right',
}

const floorColumns = [0, 1, 2]
const wallColumn = 3
const wallDetailColumn = 4
const wallDetailChance = 12
const obstacleColumns = [5, 6, 7]

function noise(x: number, y: number): number {
  let hash = Math.imul(x, 374761393) + Math.imul(y, 668265263)
  hash = Math.imul(hash ^ (hash >>> 13), 1274126177)
  return ((hash ^ (hash >>> 16)) >>> 0) % 100
}

function floorColumn(x: number, y: number): number {
  const roll = noise(x, y)
  if (roll < 75) return floorColumns[0]
  if (roll < 92) return floorColumns[1]
  return floorColumns[2]
}

function isWall(map: readonly string[], x: number, y: number): boolean {
  const symbol = map[y]?.[x]
  return symbol === undefined || symbol === '#'
}

function isDeepWall(map: readonly string[], x: number, y: number): boolean {
  for (let dy = -1; dy <= 1; dy++) {
    for (let dx = -1; dx <= 1; dx++) {
      if (!isWall(map, x + dx, y + dy)) return false
    }
  }

  return true
}

export function tileArt(biome: string | undefined, map: readonly string[], x: number, y: number): TileArt | null {
  const symbol = map[y]?.[x]

  if (biome === undefined || symbol === undefined) return null

  const row = biomeRows[biome]

  if (row === undefined) return null

  const index = (column: number) => row * sheets.biomes.columns + column

  if (symbol === '#') {
    if (isDeepWall(map, x, y)) return { base: index(wallColumn), deep: true }

    return { base: index(noise(x, y) < wallDetailChance ? wallDetailColumn : wallColumn) }
  }

  const floor = index(floorColumn(x, y))

  if (symbol === 'o') return { base: floor, overlay: index(obstacleColumns[noise(y, x) % obstacleColumns.length]) }
  const terrain = terrainSymbols[symbol]

  return terrain ? { base: floor, terrain } : { base: floor }
}
