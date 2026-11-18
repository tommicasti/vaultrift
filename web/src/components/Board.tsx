import type { CSSProperties, PointerEvent, ReactElement } from 'react'
import type { Direction, GameState } from '../api/types'
import { tileArt } from '../game/biomes'
import type { Effect } from '../game/effects'
import { chestSprite, sheets, stairsSprites } from '../game/sprites'
import { EnemyMarker } from './EnemyMarker'
import { HeroSprite } from './HeroSprite'
import { ItemIcon } from './ItemIcon'
import { LevelBadge } from './LevelBadge'
import { Sprite } from './Sprite'

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
  effects: Effect[]
  onAttack: () => void
}

export function Board({ game, viewColumns, viewRows, effects, onAttack }: BoardProps) {
  const left = game.player.x - Math.floor(viewColumns / 2)
  const top = game.player.y - Math.floor(viewRows / 2)
  const enemiesByTile = new Map(game.enemies.map(enemy => [`${enemy.x},${enemy.y}`, enemy]))
  const itemsByTile = new Map(game.floorItems.map(floorItem => [`${floorItem.x},${floorItem.y}`, floorItem.item]))
  const chestTiles = new Set(game.chests.map(chest => `${chest.x},${chest.y}`))
  const effectsByTile = new Map<string, Effect[]>()

  for (const effect of effects) {
    const tileKey = `${effect.x},${effect.y}`
    effectsByTile.set(tileKey, [...(effectsByTile.get(tileKey) ?? []), effect])
  }

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
      const stairs = game.stairs && game.stairs.x === x && game.stairs.y === y ? game.stairs : undefined
      const art = tileArt(game.biome, game.map, x, y)

      tiles.push(
        <div key={`${column},${row}`} className={tileClass(symbol)}>
          {art && <Sprite sheet={sheets.biomes} index={art.base} className={art.deep ? 'tile-art deep' : 'tile-art'} />}
          {art?.overlay !== undefined && <Sprite sheet={sheets.biomes} index={art.overlay} className="tile-art" />}
          {stairs && (
            <Sprite
              sheet={sheets.vault}
              index={stairs.isOpen ? stairsSprites.open : stairsSprites.sealed}
              className="stairs"
              label={stairs.isOpen ? 'Stairs down' : 'Sealed stairs'}
            />
          )}
          {isPlayer && (
            <>
              <HeroSprite player={game.player} className="actor" />
              <span className={`facing facing-${game.player.facing.toLowerCase()}`} aria-hidden="true">
                {facingArrows[game.player.facing]}
              </span>
              <LevelBadge level={game.player.level} highlight={game.player.unspentPoints > 0} />
            </>
          )}
          {enemy && <EnemyMarker enemy={enemy} />}
          {chestTiles.has(key) && (
            <Sprite sheet={chestSprite.sheet} index={chestSprite.index} className="chest" label="Chest" />
          )}
          {!isPlayer && !enemy && floorItem && (
            <span className={`floor-item rarity-${floorItem.rarity.toLowerCase()}`}>
              <ItemIcon item={floorItem} />
            </span>
          )}
          {effectsByTile.get(key)?.map(effect => (
            <span
              key={effect.key}
              className={`effect effect-${effect.kind} toward-${effect.facing.toLowerCase()}`}
              style={{ animationDelay: `${effect.delayMs}ms` }}
              aria-hidden="true"
            />
          ))}
        </div>,
      )
    }
  }

  return (
    <div
      className={game.biome ? 'board with-art' : 'board'}
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

  if (symbol === '#') return 'tile wall'

  return symbol === 'o' ? 'tile floor obstacle' : 'tile floor'
}