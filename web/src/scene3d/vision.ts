export const visionRadius = 5

export type Visibility = (x: number, y: number) => boolean

function blocksSight(map: string[], x: number, y: number): boolean {
  const symbol = map[y]?.[x]
  return symbol === undefined || symbol === '#'
}

function clearLine(map: string[], fromX: number, fromY: number, toX: number, toY: number): boolean {
  const steps = Math.max(Math.abs(toX - fromX), Math.abs(toY - fromY)) * 2

  for (let step = 1; step < steps; step++) {
    const x = Math.round(fromX + ((toX - fromX) * step) / steps)
    const y = Math.round(fromY + ((toY - fromY) * step) / steps)

    if ((x !== toX || y !== toY) && blocksSight(map, x, y)) return false
  }

  return true
}

function isOpen(map: string[], x: number, y: number): boolean {
  return !blocksSight(map, x, y)
}

const roomCache = new WeakMap<string[], Map<string, number>>()

function roomsOf(map: string[]): Map<string, number> {
  const cached = roomCache.get(map)
  if (cached) return cached

  const roomCells = new Set<string>()
  map.forEach((row, y) => {
    for (let x = 0; x < row.length; x++)
      if (isOpen(map, x, y) && isOpen(map, x + 1, y) && isOpen(map, x, y + 1) && isOpen(map, x + 1, y + 1))
        for (const [dx, dy] of [[0, 0], [1, 0], [0, 1], [1, 1]]) roomCells.add(`${x + dx},${y + dy}`)
  })

  const rooms = new Map<string, number>()
  let next = 0
  for (const start of roomCells) {
    if (rooms.has(start)) continue

    const queue = [start]
    rooms.set(start, next)
    while (queue.length > 0) {
      const [x, y] = queue.pop()!.split(',').map(Number)
      for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
        const key = `${x + dx},${y + dy}`
        if (roomCells.has(key) && !rooms.has(key)) {
          rooms.set(key, next)
          queue.push(key)
        }
      }
    }
    next++
  }

  roomCache.set(map, rooms)
  return rooms
}

export function visibleTiles(map: string[], originX: number, originY: number, radius = visionRadius): Visibility {
  const seen = new Set<string>()
  const rooms = roomsOf(map)
  const room = rooms.get(`${originX},${originY}`)

  if (room !== undefined) for (const [key, id] of rooms) if (id === room) seen.add(key)
  const reach = Math.ceil(radius)

  for (let y = originY - reach; y <= originY + reach; y++)
    for (let x = originX - reach; x <= originX + reach; x++)
      if (Math.hypot(x - originX, y - originY) <= radius && clearLine(map, originX, originY, x, y)) seen.add(`${x},${y}`)

  return (x, y) => seen.has(`${x},${y}`)
}

const memories = new Map<string, Set<string>>()
const maxMemories = 4

export function remember(key: string, map: string[], visible: Visibility): ReadonlySet<string> {
  let seen = memories.get(key)
  if (!seen) {
    if (memories.size >= maxMemories) memories.delete(memories.keys().next().value as string)
    seen = new Set()
    memories.set(key, seen)
  }

  const next = new Set(seen)
  map.forEach((row, y) => {
    for (let x = 0; x < row.length; x++) if (visible(x, y)) next.add(`${x},${y}`)
  })
  memories.set(key, next)
  return next
}
