const solid = new Set(['#', 'o', 'C'])
const snapDistance = 1.5
const deadZone = 0.04
const reconcileShare = 0.35
const displayRate = 18
const historyMs = 1500

export interface Point {
  x: number
  y: number
}

export function isSolid(map: readonly string[], x: number, y: number): boolean {
  if (y < 0 || y >= map.length || x < 0 || x >= map[y].length) return true
  return solid.has(map[y][x])
}

function overlaps(cx: number, cy: number, radius: number, tx: number, ty: number) {
  const nx = Math.min(Math.max(cx, tx - 0.5), tx + 0.5)
  const ny = Math.min(Math.max(cy, ty - 0.5), ty + 0.5)
  const dx = cx - nx
  const dy = cy - ny
  return dx * dx + dy * dy < radius * radius
}

export function hitsWall(map: readonly string[], cx: number, cy: number, radius: number): boolean {
  const fromX = Math.floor(cx - radius + 0.5)
  const toX = Math.floor(cx + radius + 0.5)
  const fromY = Math.floor(cy - radius + 0.5)
  const toY = Math.floor(cy + radius + 0.5)
  for (let y = fromY; y <= toY; y++) {
    for (let x = fromX; x <= toX; x++) {
      if (isSolid(map, x, y) && overlaps(cx, cy, radius, x, y)) return true
    }
  }
  return false
}

export interface Solid {
  x: number
  y: number
  radius: number
}

function pushesInto(solids: readonly Solid[], from: Point, x: number, y: number, radius: number): boolean {
  return solids.some(solid => {
    const distance = Math.hypot(x - solid.x, y - solid.y)
    return distance < solid.radius + radius && distance < Math.hypot(from.x - solid.x, from.y - solid.y)
  })
}

export function slide(map: readonly string[], from: Point, dx: number, dy: number, radius: number, solids: readonly Solid[] = [], roundsCorners = false): Point {
  const blocked = (x: number, y: number) => hitsWall(map, x, y, radius) || pushesInto(solids, from, x, y, radius)
  let position: Point = { x: from.x, y: from.y }
  if (dx !== 0 && !blocked(position.x + dx, position.y)) position = { x: position.x + dx, y: position.y }
  else if (roundsCorners && dx !== 0 && dy === 0) position = roundCorner(map, position, Math.sign(dx), 0, Math.abs(dx), blocked)
  if (dy !== 0 && !blocked(position.x, position.y + dy)) position = { x: position.x, y: position.y + dy }
  else if (roundsCorners && dy !== 0 && dx === 0) position = roundCorner(map, position, 0, Math.sign(dy), Math.abs(dy), blocked)
  return position
}

function roundCorner(map: readonly string[], position: Point, dx: number, dy: number, distance: number, blocked: (x: number, y: number) => boolean): Point {
  const tileX = Math.floor(position.x + 0.5)
  const tileY = Math.floor(position.y + 0.5)
  if (isSolid(map, tileX + dx, tileY + dy)) return position
  const offset = dx !== 0 ? tileY - position.y : tileX - position.x
  if (Math.abs(offset) < 1e-6) return position
  const shift = Math.sign(offset) * Math.min(distance, Math.abs(offset))
  const nudged = dx !== 0 ? { x: position.x, y: position.y + shift } : { x: position.x + shift, y: position.y }
  return blocked(nudged.x, nudged.y) ? position : nudged
}

const currentOffsets: Record<string, Point> = { '^': { x: 0, y: -1 }, v: { x: 0, y: 1 }, '<': { x: -1, y: 0 }, '>': { x: 1, y: 0 } }

export function currentAt(map: readonly string[], x: number, y: number): Point | undefined {
  return currentOffsets[map[Math.round(y)]?.[Math.round(x)] ?? '']
}

interface Sample {
  time: number
  x: number
  y: number
}

export interface Motion {
  move: Point
  speed: number
  radius: number
  currentSpeed: number
  solids?: readonly Solid[]
}

export class Predictor {
  x = 0
  y = 0
  private logicalX = 0
  private logicalY = 0
  private history: Sample[] = []
  private ready = false

  get isReady() {
    return this.ready
  }

  reset(x: number, y: number) {
    this.x = x
    this.y = y
    this.logicalX = x
    this.logicalY = y
    this.history = []
    this.ready = true
  }

  forget() {
    this.ready = false
    this.history = []
  }

  advance(map: readonly string[], motion: Motion, seconds: number, now: number) {
    if (!this.ready) return
    let position: Point = { x: this.logicalX, y: this.logicalY }
    const { move } = motion
    if (move.x !== 0 || move.y !== 0) {
      const scale = (motion.speed * seconds) / Math.max(1, Math.hypot(move.x, move.y))
      position = slide(map, position, move.x * scale, move.y * scale, motion.radius, motion.solids, true)
    }
    const flow = currentAt(map, position.x, position.y)
    if (flow && motion.currentSpeed > 0)
      position = slide(map, position, flow.x * motion.currentSpeed * seconds, flow.y * motion.currentSpeed * seconds, motion.radius, motion.solids)
    this.logicalX = position.x
    this.logicalY = position.y
    this.history.push({ time: now, x: position.x, y: position.y })
    while (this.history.length > 0 && this.history[0].time < now - historyMs) this.history.shift()
    const blend = Math.min(1, seconds * displayRate)
    this.x += (this.logicalX - this.x) * blend
    this.y += (this.logicalY - this.y) * blend
  }

  reconcile(server: Point, receivedAt: number, roundTripMs: number) {
    if (!this.ready) {
      this.reset(server.x, server.y)
      return
    }
    const past = this.positionAt(receivedAt - roundTripMs)
    const errorX = server.x - past.x
    const errorY = server.y - past.y
    const error = Math.hypot(errorX, errorY)
    if (error > snapDistance) {
      this.reset(server.x, server.y)
      return
    }
    if (error < deadZone) return
    const shiftX = errorX * reconcileShare
    const shiftY = errorY * reconcileShare
    this.logicalX += shiftX
    this.logicalY += shiftY
    for (const sample of this.history) {
      sample.x += shiftX
      sample.y += shiftY
    }
  }

  private positionAt(time: number): Point {
    const history = this.history
    if (history.length === 0 || time >= history[history.length - 1].time) return { x: this.logicalX, y: this.logicalY }
    if (time <= history[0].time) return { x: history[0].x, y: history[0].y }
    let index = history.length - 1
    while (index > 0 && history[index - 1].time > time) index--
    const before = history[index - 1]
    const after = history[index]
    const alpha = (time - before.time) / Math.max(1e-6, after.time - before.time)
    return { x: before.x + (after.x - before.x) * alpha, y: before.y + (after.y - before.y) * alpha }
  }
}
