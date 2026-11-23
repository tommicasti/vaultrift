import type { Direction, TilePosition } from '../api/types'

const clockwise: Direction[] = ['Up', 'Right', 'Down', 'Left']

export interface Intent {
  kind: 'move' | 'face'
  direction: Direction
}

export function turned(facing: Direction, quarters: number): Direction {
  return clockwise[(clockwise.indexOf(facing) + quarters + 4) % 4]
}

export function relativeIntent(pressed: Direction, facing: Direction): Intent {
  switch (pressed) {
    case 'Up':
      return { kind: 'move', direction: facing }
    case 'Down':
      return { kind: 'face', direction: turned(facing, 2) }
    case 'Left':
      return { kind: 'face', direction: turned(facing, -1) }
    case 'Right':
      return { kind: 'face', direction: turned(facing, 1) }
  }
}

export function towardFacing(offset: TilePosition, facing: Direction): TilePosition {
  const { x, y } = offset
  switch (facing) {
    case 'Up':
      return { x, y }
    case 'Right':
      return { x: y, y: -x }
    case 'Down':
      return { x: -x, y: -y }
    case 'Left':
      return { x: -y, y: x }
  }
}
