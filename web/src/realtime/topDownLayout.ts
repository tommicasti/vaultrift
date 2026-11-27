import { clearLine, visibleTiles, type Visibility } from '../scene3d/vision'

export const playTile = 40
export const exploreRadius = 7
export const meetRadius = 4

export function exploredKey(x: number, y: number): string {
  return `${x},${y}`
}

export function tileSize(mode: 'play' | 'map', width: number, height: number, columns: number, rows: number, zoom: number): number {
  if (mode === 'play') return Math.round(playTile * Math.max(0.6, Math.min(1.6, zoom)) * (Math.min(width, height) < 500 ? 0.85 : 1))
  if (columns === 0 || rows === 0) return 1
  return Math.max(2, Math.floor(Math.min((width * 0.92) / columns, (height * 0.86) / rows)))
}

export interface Vision {
  explored: ReadonlySet<string>
  sight: Visibility
}

export function sightFrom(map: string[], x: number, y: number, radius = exploreRadius): Visibility {
  return visibleTiles(map, Math.round(x), Math.round(y), radius)
}

export function explore(explored: Set<string>, map: readonly string[], sight: Visibility): boolean {
  let added = false
  for (let y = 0; y < map.length; y++) {
    for (let x = 0; x < map[y].length; x++) {
      if (!sight(x, y)) continue
      const key = exploredKey(x, y)
      if (explored.has(key)) continue
      explored.add(key)
      added = true
    }
  }
  return added
}

export function meetBosses(
  met: Set<number>,
  map: string[],
  heroX: number,
  heroY: number,
  monsters: readonly { id: number; isBoss: boolean; x: number; y: number }[],
  radius = meetRadius,
): void {
  const fromX = Math.round(heroX)
  const fromY = Math.round(heroY)
  for (const monster of monsters) {
    if (!monster.isBoss || met.has(monster.id)) continue
    const toX = Math.round(monster.x)
    const toY = Math.round(monster.y)
    if (Math.hypot(toX - fromX, toY - fromY) <= radius && clearLine(map, fromX, fromY, toX, toY)) met.add(monster.id)
  }
}
