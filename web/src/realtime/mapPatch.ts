export function patchMap(rows: readonly string[], tiles: readonly (readonly [number, number])[], symbol: string): string[] {
  if (tiles.length === 0 || symbol.length !== 1) return [...rows]
  const grid = rows.map(row => row.split(''))
  for (const [x, y] of tiles) {
    if (y >= 0 && y < grid.length && x >= 0 && x < grid[y].length) grid[y][x] = symbol
  }
  return grid.map(row => row.join(''))
}
