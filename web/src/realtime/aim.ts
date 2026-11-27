import { headingVector } from './input'

export interface Aimer {
  x: number
  y: number
  reach: number
  arcDegrees: number
}

export interface Target {
  x: number
  y: number
  radius: number
  health: number
}

export const minimumAimDegrees = 30
export const reachSlack = 0.3

export function enemyAhead(aimer: Aimer, yaw: number, targets: Iterable<Target>): boolean {
  const heading = headingVector(yaw)
  const halfArc = (Math.max(aimer.arcDegrees, minimumAimDegrees) / 2) * (Math.PI / 180)

  for (const target of targets) {
    if (target.health <= 0) continue
    const dx = target.x - aimer.x
    const dy = target.y - aimer.y
    const distance = Math.hypot(dx, dy)
    if (distance > aimer.reach + target.radius + reachSlack) continue
    if (distance < 1e-6) return true
    const cosine = (dx * heading.x + dy * heading.y) / distance
    if (cosine >= Math.cos(halfArc)) return true
  }

  return false
}
