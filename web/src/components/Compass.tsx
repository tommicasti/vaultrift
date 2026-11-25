import type { Direction, TilePosition } from '../api/types'
import { towardFacing } from '../scene3d/firstPerson'
import type { SpriteSheet } from '../game/sprites'
import { Sprite } from './Sprite'

interface CompassProps {
  from: TilePosition
  to: TilePosition
  viewColumns: number
  viewRows: number
  sheet: SpriteSheet
  icon: number
  label: string
  variant: 'grave' | 'boss'
  facing?: Direction
  always?: boolean
}

const compassInset = 9

export function Compass({ from, to, viewColumns, viewRows, sheet, icon, label, variant, facing, always = false }: CompassProps) {
  const offset = { x: to.x - from.x, y: to.y - from.y }
  const { x: dx, y: dy } = facing ? towardFacing(offset, facing) : offset
  const halfColumns = Math.floor(viewColumns / 2)
  const halfRows = Math.floor(viewRows / 2)
  const distance = Math.abs(dx) + Math.abs(dy)

  const nearby = facing || always ? distance <= 2 : Math.abs(dx) <= halfColumns && Math.abs(dy) <= halfRows
  if (nearby) return null

  const reach = Math.max(Math.abs(dx) / (halfColumns + 0.5), Math.abs(dy) / (halfRows + 0.5))
  const along = (offset: number) => Math.min(100 - compassInset, Math.max(compassInset, 50 + (offset / reach) * 50))
  const angle = (Math.atan2(dy, dx) * 180) / Math.PI

  return (
    <span
      className={`grave-compass compass-${variant}`}
      style={{ left: `${along(dx / (halfColumns + 0.5))}%`, top: `${along(dy / (halfRows + 0.5))}%` }}
      title={`${label}: ${distance} steps away`}
      aria-label={`${label} is ${distance} steps away`}
    >
      <span className="grave-compass-arrow" style={{ transform: `rotate(${angle}deg)` }} aria-hidden="true">
        ➜
      </span>
      <Sprite sheet={sheet} index={icon} className="grave-compass-icon" />
      <span className="grave-compass-distance">{distance}</span>
    </span>
  )
}
