import type { CSSProperties, PointerEvent, ReactElement } from 'react'
import type { Direction, EnemyState, GameState } from '../api/types'
import { ItemIcon } from './ItemIcon'

const facingArrows: Record<Direction, string> = {
  Up: '▲',
  Down: '▼',
  Left: '◀',
  Right: '▶',
}

interface BoardProps {
  game: GameState
  viewColumns: number
  viewRows: number
  onAttack: () => void
}

export function Board({ game, viewColumns, viewRows, onAttack }: BoardProps) {
  const left = game.player.x - Math.floor(viewColumns / 2)
  const top = game.player.y - Math.floor(viewRows / 2)
  const enemiesByTile = new Map(game.enemies.map(enemy => [`${enemy.x},${enemy.y}`, enemy]))
  const itemsByTile = new Map(game.floorItems.map(floorItem => [`${floorItem.x},${floorItem.y}`, floorItem.item]))

  function handlePointerDown(event: PointerEvent<HTMLDivElement>) {
    if (event.pointerType === 'mouse' && event.button === 0)
      onAttack()
  }

  const tiles: ReactElement[] = []

  for (let row = 0; row < viewRows; row++) {
    for (let column = 0; column < viewColumns; column++) {
      const x = left + column
      const y = top + row
      const key = `${x},${y}`
      const symbol: string | undefined = game.map[y]?.[x]
      const isPlayer = game.player.x === x && game.player.y === y
      const enemy = enemiesByTile.get(key)
      const floorItem = itemsByTile.get(key)

      tiles.push(
        <div key={`${column},${row}`} className={tileClass(symbol)}>
          {isPlayer && <span className="player">{facingArrows[game.player.facing]}</span>}
          {enemy && <EnemyMarker enemy={enemy} />}
          {!isPlayer && !enemy && floorItem && <ItemIcon item={floorItem} />}
        </div>,
      )
    }
  }

  return (
    <div
      className="board"
      style={{ '--columns': viewColumns, '--rows': viewRows } as CSSProperties}
      onPointerDown={handlePointerDown}
    >
      {tiles}
    </div>
  )
}

function tileClass(symbol: string | undefined): string {
  if (symbol === undefined)
    return 'tile void'

  return symbol === '#' ? 'tile wall' : 'tile floor'
}

function EnemyMarker({ enemy }: { enemy: EnemyState }) {
  const healthPercent = (enemy.health / enemy.maxHealth) * 100

  return (
    <span className={enemy.isAwake ? 'enemy' : 'enemy sleeping'}>
      👾
      <span className="enemy-health" style={{ width: `${healthPercent}%` }} />
    </span>
  )
}