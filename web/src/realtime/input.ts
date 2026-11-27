export interface MoveInput {
  x: number
  y: number
  attack: boolean
  aimX: number
  aimY: number
  sprint: boolean
}

export const sprintKeys = new Set(['ShiftLeft', 'ShiftRight'])

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
    first.sprint === second.sprint &&
    Math.abs(first.aimX - second.aimX) < aimTolerance &&
    Math.abs(first.aimY - second.aimY) < aimTolerance
  )
}

export function headingVector(yaw: number): { x: number; y: number } {
  return { x: Math.sin(yaw), y: Math.cos(yaw) }
}

export function relativeMove(keys: { x: number; y: number }, yaw: number): { x: number; y: number } {
  const forward = headingVector(yaw)
  const right = { x: -forward.y, y: forward.x }
  const x = forward.x * -keys.y + right.x * keys.x
  const y = forward.y * -keys.y + right.y * keys.x
  const length = Math.hypot(x, y)
  return length < 1e-6 ? { x: 0, y: 0 } : { x: x / length, y: y / length }
}

export function joystickVector(dx: number, dy: number, radius: number): { x: number; y: number; offsetX: number; offsetY: number } {
  const distance = Math.hypot(dx, dy)
  if (distance < radius * 0.15) return { x: 0, y: 0, offsetX: dx, offsetY: dy }
  const clamped = Math.min(distance, radius)
  const offsetX = (dx / distance) * clamped
  const offsetY = (dy / distance) * clamped
  return { x: offsetX / radius, y: offsetY / radius, offsetX, offsetY }
}
