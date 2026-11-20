import { useLayoutEffect, useRef, type CSSProperties, type PointerEvent, type ReactElement } from 'react'
import type { Direction, GameState } from '../api/types'
import { tileArt } from '../game/biomes'
import type { Effect } from '../game/effects'
import { chestSprite, graveSprite, merchantSprite, sheets, stairsSprites } from '../game/sprites'
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

const cameraSlideMs = 120
const margin = 1

export function Board({ game, viewColumns, viewRows, effects, onAttack }: BoardProps) {
  const boardRef = useRef<HTMLDivElement>(null)
  const cameraRef = useRef<HTMLDivElement>(null)
  const heroRef = useRef<HTMLSpanElement>(null)
  const lastEnemies = useRef(new Map<number, { x: number; y: number }>())
  const lastSpot = useRef({ x: game.player.x, y: game.player.y, floor: game.floor })
  const columns = viewColumns + margin * 2
  const rows = viewRows + margin * 2
  const left = game.player.x - Math.floor(viewColumns / 2) - margin
  const top = game.player.y - Math.floor(viewRows / 2) - margin

  useLayoutEffect(() => {
    const previous = lastSpot.current
    const dx = game.player.x - previous.x
    const dy = game.player.y - previous.y
    lastSpot.current = { x: game.player.x, y: game.player.y, floor: game.floor }

    const board = boardRef.current
    const camera = cameraRef.current
    const stepped = previous.floor === game.floor && Math.abs(dx) + Math.abs(dy) > 0 && Math.abs(dx) <= 2 && Math.abs(dy) <= 2

    if (!board || !camera || !stepped || typeof camera.animate !== 'function') return
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return

    const tile = board.clientWidth / viewColumns
    const running = camera.getAnimations()
    const current = new DOMMatrixReadOnly(running.length > 0 ? getComputedStyle(camera).transform : undefined)
    running.forEach(animation => animation.cancel())

    camera.animate(
      [{ transform: `translate(${current.e + dx * tile}px, ${current.f + dy * tile}px)` }, { transform: 'translate(0px, 0px)' }],
      { duration: cameraSlideMs, easing: running.length > 0 ? 'linear' : 'cubic-bezier(0.25, 0.1, 0.25, 1)' },
    )

    heroRef.current?.animate(
      [{ transform: 'translateY(0)' }, { transform: 'translateY(-8%)' }, { transform: 'translateY(0)' }],
      { duration: cameraSlideMs, easing: 'ease-in-out' },
    )
  }, [game.player.x, game.player.y, game.floor, viewColumns])

  useLayoutEffect(() => {
    const previous = lastEnemies.current
    const next = new Map(game.enemies.map(enemy => [enemy.id, { x: enemy.x, y: enemy.y }]))
    lastEnemies.current = next

    const board = boardRef.current
    if (!board || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return

    for (const enemy of game.enemies) {
      const before = previous.get(enemy.id)
      if (!before) continue

      const dx = before.x - enemy.x
      const dy = before.y - enemy.y
      if ((dx === 0 && dy === 0) || Math.abs(dx) > 3 || Math.abs(dy) > 3) continue

      const layer = board.querySelector<HTMLElement>(`[data-enemy-id="${enemy.id}"]`)
      if (!layer || typeof layer.animate !== 'function') continue

      layer.animate(
        [{ transform: `translate(${dx * 100}%, ${dy * 100}%)` }, { transform: 'translate(0, 0)' }],
        { duration: cameraSlideMs, easing: 'ease-out' },
      )
    }
  }, [game.enemies])

  const enemiesByTile = new Map(game.enemies.map(enemy => [`${enemy.x},${enemy.y}`, enemy]))
  const itemsByTile = new Map(game.floorItems.map(floorItem => [`${floorItem.x},${floorItem.y}`, floorItem.item]))
  const chestTiles = new Set(game.chests.map(chest => `${chest.x},${chest.y}`))
  const mainHand = game.player.equipment.find(equipped => equipped.slot === 'MainHand')?.item
  const offHand = game.player.equipment.find(equipped => equipped.slot === 'OffHand')?.item
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

  for (let row = 0; row < rows; row++) {
    for (let column = 0; column < columns; column++) {
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
        <div key={`${column},${row}`} className={enemy?.kind === 'custode' ? `${tileClass(symbol)} giant` : tileClass(symbol)}>
          {art && <Sprite sheet={sheets.biomes} index={art.base} className={art.deep ? 'tile-art deep' : 'tile-art'} />}
          {art?.overlay !== undefined && <Sprite sheet={sheets.biomes} index={art.overlay} className="tile-art" />}
          {art?.terrain && <span className={`tile-art terrain terrain-${art.terrain}`} aria-hidden="true" />}
          {stairs && (
            <Sprite
              sheet={sheets.vault}
              index={stairs.isOpen ? stairsSprites.open : stairsSprites.sealed}
              className="stairs"
              label={stairs.isOpen ? 'Stairs down' : 'Sealed stairs'}
            />
          )}
          {game.grave && game.grave.x === x && game.grave.y === y && (
            <Sprite
              sheet={sheets.dungeon}
              index={graveSprite}
              className="grave"
              label={`Your grave (${game.grave.count} ${game.grave.count === 1 ? 'item' : 'items'})`}
            />
          )}
          {isPlayer && (
            <span className="hero-layer" ref={heroRef}>
              <HeroSprite player={game.player} className="actor" />
              {mainHand && <ItemIcon item={mainHand} className="held-weapon on-board" />}
              {offHand && <ItemIcon item={offHand} className="held-off-hand on-board" />}
              <span className={`facing facing-${game.player.facing.toLowerCase()}`} aria-hidden="true">
                {facingArrows[game.player.facing]}
              </span>
              <LevelBadge level={game.player.level} highlight={game.player.unspentPoints > 0} />
            </span>
          )}
          {enemy && (
            <span className="enemy-layer" data-enemy-id={enemy.id}>
              <EnemyMarker enemy={enemy} />
            </span>
          )}
          {game.merchant && game.merchant.x === x && game.merchant.y === y && (
            <Sprite sheet={merchantSprite.sheet} index={merchantSprite.index} className="actor merchant" label="Merchant" />
          )}
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
            >
              {effect.label}
            </span>
          ))}
        </div>,
      )
    }
  }

  return (
    <div
      ref={boardRef}
      className={game.biome ? 'board with-art' : 'board'}
      style={{ '--columns': viewColumns, '--rows': viewRows, '--grid-columns': columns, '--grid-rows': rows } as CSSProperties}
      onPointerDown={handlePointerDown}
    >
      <div ref={cameraRef} className="board-camera">
        <div className="board-grid" style={{ transform: gridOffset(columns, rows) }}>
          {tiles}
        </div>
      </div>
      {game.grave && <GraveCompass game={game} viewColumns={viewColumns} viewRows={viewRows} />}
    </div>
  )
}

interface GraveCompassProps {
  game: GameState
  viewColumns: number
  viewRows: number
}

const compassInset = 9

function GraveCompass({ game, viewColumns, viewRows }: GraveCompassProps) {
  if (!game.grave) return null

  const dx = game.grave.x - game.player.x
  const dy = game.grave.y - game.player.y
  const halfColumns = Math.floor(viewColumns / 2)
  const halfRows = Math.floor(viewRows / 2)

  if (Math.abs(dx) <= halfColumns && Math.abs(dy) <= halfRows) return null

  const reach = Math.max(Math.abs(dx) / (halfColumns + 0.5), Math.abs(dy) / (halfRows + 0.5))
  const along = (offset: number) => Math.min(100 - compassInset, Math.max(compassInset, 50 + (offset / reach) * 50))
  const angle = (Math.atan2(dy, dx) * 180) / Math.PI
  const distance = Math.abs(dx) + Math.abs(dy)

  return (
    <span
      className="grave-compass"
      style={{ left: `${along(dx / (halfColumns + 0.5))}%`, top: `${along(dy / (halfRows + 0.5))}%` }}
      title={`Your grave: ${distance} steps away`}
      aria-label={`Your grave is ${distance} steps away`}
    >
      <span className="grave-compass-arrow" style={{ transform: `rotate(${angle}deg)` }} aria-hidden="true">
        ➜
      </span>
      <Sprite sheet={sheets.dungeon} index={graveSprite} className="grave-compass-icon" />
      <span className="grave-compass-distance">{distance}</span>
    </span>
  )
}

function gridOffset(columns: number, rows: number): string {
  return `translate(${(-margin * 100) / columns}%, ${(-margin * 100) / rows}%)`
}

function tileClass(symbol: string | undefined): string {
  if (symbol === undefined)
    return 'tile void'

  if (symbol === '#') return 'tile wall'

  if (symbol === 'L') return 'tile floor lava'
  if (symbol === '~') return 'tile floor water'
  if (symbol === 'C') return 'tile floor obstacle crystal'
  if (symbol === 'S') return 'tile floor spores'
  if ('^v<>'.includes(symbol)) return 'tile floor current'

  return symbol === 'o' ? 'tile floor obstacle' : 'tile floor'
}