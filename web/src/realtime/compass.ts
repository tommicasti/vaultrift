export interface CompassSpot {
  visible: boolean
  x: number
  y: number
  angle: number
}

const edgeX = 0.9
const edgeY = 0.82
const insideX = 0.97
const insideY = 0.95

export function compassSpot(ndcX: number, ndcY: number, behind: boolean, width: number, height: number): CompassSpot {
  if (!behind && Math.abs(ndcX) <= insideX && Math.abs(ndcY) <= insideY) return { visible: false, x: 0, y: 0, angle: 0 }

  let dx = behind ? -ndcX : ndcX
  let dy = behind ? -ndcY : ndcY
  if (Math.abs(dx) < 1e-6 && Math.abs(dy) < 1e-6) dy = -1

  const scale = 1 / Math.max(Math.abs(dx) / edgeX, Math.abs(dy) / edgeY)
  dx *= scale
  dy *= scale

  return {
    visible: true,
    x: ((dx + 1) / 2) * width,
    y: ((1 - dy) / 2) * height,
    angle: Math.atan2(-dy, dx),
  }
}

export function bossMarkerId(monster: { id: number }): number {
  return -1 - monster.id
}
