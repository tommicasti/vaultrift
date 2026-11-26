export interface MoveInput {
  x: number
  y: number
  attack: boolean
  aimX: number
  aimY: number
}

const moveKeys: Record<string, [number, number]> = {
  KeyW: [0, -1],
  ArrowUp: [0, -1],
  KeyS: [0, 1],
  ArrowDown: [0, 1],
  KeyA: [-1, 0],
  ArrowLeft: [-1, 0],
  KeyD: [1, 0],
  ArrowRight: [1, 0],
}

export function isMoveKey(code: string): boolean {
  return code in moveKeys
}

export function moveVector(pressed: ReadonlySet<string>): { x: number; y: number } {
  let x = 0
  let y = 0

  for (const code of pressed) {
    const step = moveKeys[code]
    if (!step) continue
    x += step[0]
    y += step[1]
  }

  x = Math.sign(x)
  y = Math.sign(y)
  const length = Math.hypot(x, y)
  return length === 0 ? { x: 0, y: 0 } : { x: x / length, y: y / length }
}

const aimTolerance = 0.05

export function sameInput(first: MoveInput, second: MoveInput): boolean {
  return (
    Math.abs(first.x - second.x) < 1e-3 &&
    Math.abs(first.y - second.y) < 1e-3 &&
    first.attack === second.attack &&
    Math.abs(first.aimX - second.aimX) < aimTolerance &&
    Math.abs(first.aimY - second.aimY) < aimTolerance
  )
}

export function aimToward(from: { x: number; y: number }, to: { x: number; y: number } | null): { aimX: number; aimY: number } {
  if (!to) return { aimX: 0, aimY: 0 }
  const dx = to.x - from.x
  const dy = to.y - from.y
  const length = Math.hypot(dx, dy)
  return length < 0.2 ? { aimX: 0, aimY: 0 } : { aimX: dx / length, aimY: dy / length }
}

export function joystickVector(dx: number, dy: number, radius: number): { x: number; y: number; offsetX: number; offsetY: number } {
  const distance = Math.hypot(dx, dy)
  if (distance < radius * 0.15) return { x: 0, y: 0, offsetX: dx, offsetY: dy }
  const clamped = Math.min(distance, radius)
  const offsetX = (dx / distance) * clamped
  const offsetY = (dy / distance) * clamped
  return { x: offsetX / radius, y: offsetY / radius, offsetX, offsetY }
}
