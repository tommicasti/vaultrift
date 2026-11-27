import { useEffect, useRef } from 'react'
import type { MerchantState, TotemState, WaystoneState } from '../api/types'
import { tileArt } from '../game/biomes'
import {
  chestSprite,
  crownSprite,
  classSprite,
  enemySprite,
  graveSprite,
  merchantSprite,
  sheets,
  stairsSprites,
  totemSprite,
  waystoneSprite,
  type SpriteSheet,
} from '../game/sprites'
import terrainSheet from '../assets/tilesets/vaultrift-terrain.png'
import { rarityColors } from '../scene3d/models'
import type { Placed } from './interpolation'
import type { LiveCue, Lunge, MovingProjectile, Popup, Spark } from './LiveScene'
import type { LiveChest, LiveGrave, LiveHero, LiveItem, LiveMonster, LiveStairs } from './types'
import { exploredKey, sightFrom, tileSize } from './topDownLayout'

export type TopDownMode = 'play' | 'map'

export interface TopDownProps {
  mode: TopDownMode
  biome?: string
  map: string[]
  explored?: ReadonlySet<string>
  heroId: number
  heroes: LiveHero[]
  monsters: LiveMonster[]
  bosses?: LiveMonster[]
  items: LiveItem[]
  chests: LiveChest[]
  stairs: LiveStairs | null
  merchant: MerchantState | null
  waystone: WaystoneState | null
  totem: TotemState | null
  graves: LiveGrave[]
  projectiles: MovingProjectile[]
  popups: Popup[]
  sparks?: Spark[]
  monsterCues?: ReadonlyMap<number, LiveCue>
  lunges?: ReadonlyMap<number, Lunge>
  strikeReach?: number
  zoom?: number
  trackHero: (hero: LiveHero) => Placed
  trackMonster: (monster: LiveMonster) => Placed
  trackProjectile: (projectile: MovingProjectile) => Placed
}

const terrain = { src: terrainSheet, columns: 2, rows: 5 }
const terrainRows: Record<string, number> = { L: 0, '~': 1, C: 2, S: 3, '^': 4, v: 4, '<': 4, '>': 4 }
const currentAngles: Record<string, number> = { '^': -Math.PI / 2, v: Math.PI / 2, '<': Math.PI, '>': 0 }
const images = new Map<string, HTMLImageElement>()
const popupColors: Record<Popup['kind'], string> = {
  damage: '#ffffff',
  critical: '#ffd24a',
  miss: '#b8c0cc',
  hurt: '#ff6a5a',
  reward: '#8affa0',
  level: '#7ec8ff',
  burn: '#ff9a48',
  frozen: '#9ae6ff',
}

function image(src: string): HTMLImageElement {
  let found = images.get(src)
  if (!found) {
    found = new Image()
    found.src = src
    images.set(src, found)
  }
  return found
}

function drawSprite(context: CanvasRenderingContext2D, sheet: SpriteSheet, index: number, x: number, y: number, size: number) {
  const picture = image(sheet.src)
  if (!picture.complete || picture.naturalWidth === 0) return
  const cellWidth = picture.naturalWidth / sheet.columns
  const cellHeight = picture.naturalHeight / sheet.rows
  const column = index % sheet.columns
  const row = Math.floor(index / sheet.columns)
  context.drawImage(picture, column * cellWidth, row * cellHeight, cellWidth, cellHeight, x, y, size, size)
}

export function TopDown(props: TopDownProps) {
  const canvas = useRef<HTMLCanvasElement>(null)
  const latest = useRef(props)

  useEffect(() => {
    latest.current = props
  })

  useEffect(() => {
    let frame = 0
    const draw = (now: number) => {
      frame = requestAnimationFrame(draw)
      const element = canvas.current
      const context = element?.getContext('2d')
      if (!element || !context) return

      const ratio = Math.min(2, window.devicePixelRatio || 1)
      const width = element.clientWidth
      const height = element.clientHeight
      if (element.width !== Math.round(width * ratio) || element.height !== Math.round(height * ratio)) {
        element.width = Math.round(width * ratio)
        element.height = Math.round(height * ratio)
      }

      context.setTransform(ratio, 0, 0, ratio, 0, 0)
      context.imageSmoothingEnabled = false
      render(context, latest.current, width, height, now)
    }
    frame = requestAnimationFrame(draw)
    return () => cancelAnimationFrame(frame)
  }, [])

  return <canvas ref={canvas} className={props.mode === 'map' ? 'top-down top-down-map' : 'top-down'} />
}

function render(context: CanvasRenderingContext2D, props: TopDownProps, width: number, height: number, now: number) {
  const { map, mode, heroId } = props
  const rows = map.length
  const columns = Math.max(0, ...map.map(row => row.length))
  const me = props.heroes.find(hero => hero.id === heroId)
  const center = me ? props.trackHero(me) : { x: columns / 2, y: rows / 2, moving: false }
  const size = tileSize(mode, width, height, columns, rows, props.zoom ?? 1)
  const originX = mode === 'map' ? (width - columns * size) / 2 : width / 2 - center.x * size - size / 2
  const originY = mode === 'map' ? (height - rows * size) / 2 : height / 2 - center.y * size - size / 2
  const sight = mode === 'play' && me ? sightFrom(map, center.x, center.y) : undefined
  const remembered = (x: number, y: number) => !props.explored || props.explored.has(exploredKey(x, y))
  const visible = (x: number, y: number) => !sight || sight(x, y)
  const seen = (x: number, y: number) => remembered(x, y) || (sight !== undefined && sight(x, y))
  const screenX = (x: number) => originX + x * size
  const screenY = (y: number) => originY + y * size

  context.fillStyle = mode === 'map' ? 'rgba(6, 5, 10, 0.92)' : '#07060a'
  context.fillRect(0, 0, width, height)

  const fromX = mode === 'map' ? 0 : Math.max(0, Math.floor(-originX / size) - 1)
  const toX = mode === 'map' ? columns - 1 : Math.min(columns - 1, Math.ceil((width - originX) / size) + 1)
  const fromY = mode === 'map' ? 0 : Math.max(0, Math.floor(-originY / size) - 1)
  const toY = mode === 'map' ? rows - 1 : Math.min(rows - 1, Math.ceil((height - originY) / size) + 1)
  const terrainFrame = Math.floor(now / 400) % 2

  for (let y = fromY; y <= toY; y++) {
    for (let x = fromX; x <= toX; x++) {
      if (!seen(x, y)) continue
      const symbol = map[y]?.[x]
      if (symbol === undefined) continue
      const art = tileArt(props.biome, map, x, y)
      const left = screenX(x)
      const top = screenY(y)

      if (art?.deep) continue
      if (art) drawSprite(context, sheets.biomes, art.base, left, top, size)
      else {
        context.fillStyle = symbol === '#' ? '#2a2630' : '#4a4352'
        context.fillRect(left, top, size, size)
      }

      if (art?.overlay !== undefined) drawSprite(context, sheets.biomes, art.overlay, left, top, size)

      if (symbol === '#') {
        context.fillStyle = 'rgba(0, 0, 0, 0.32)'
        context.fillRect(left, top, size, size)
      } else {
        context.strokeStyle = 'rgba(0, 0, 0, 0.1)'
        context.lineWidth = 1
        context.strokeRect(left + 0.5, top + 0.5, size - 1, size - 1)
        edgeShadows(context, map, x, y, left, top, size)
      }

      const terrainRow = terrainRows[symbol]
      if (terrainRow !== undefined) {
        const angle = currentAngles[symbol]
        if (angle === undefined) drawSprite(context, terrain, terrainRow * terrain.columns + terrainFrame, left, top, size)
        else {
          context.save()
          context.translate(left + size / 2, top + size / 2)
          context.rotate(angle)
          drawSprite(context, terrain, terrainRow * terrain.columns + terrainFrame, -size / 2, -size / 2, size)
          context.restore()
        }
      }

      if (sight && !sight(x, y)) {
        context.fillStyle = 'rgba(4, 3, 8, 0.62)'
        context.fillRect(left, top, size, size)
      }
    }
  }

  const place = (sheet: SpriteSheet, index: number, x: number, y: number, scale = 1) => {
    if (!seen(Math.round(x), Math.round(y))) return
    const drawn = size * scale
    drawSprite(context, sheet, index, screenX(x) + (size - drawn) / 2, screenY(y) + (size - drawn) / 2, drawn)
  }

  if (props.stairs) place(sheets.vault, props.stairs.isOpen ? stairsSprites.open : stairsSprites.sealed, props.stairs.x, props.stairs.y)
  for (const chest of props.chests) place(chestSprite.sheet, chestSprite.index, chest.x, chest.y)
  for (const grave of props.graves) place(sheets.dungeon, graveSprite, grave.x, grave.y)
  if (props.waystone) place(waystoneSprite.sheet, waystoneSprite.index, props.waystone.x, props.waystone.y)
  if (props.totem) place(totemSprite.sheet, totemSprite.index, props.totem.x, props.totem.y)
  if (props.merchant) place(merchantSprite.sheet, merchantSprite.index, props.merchant.x, props.merchant.y)

  for (const item of props.items) {
    if (!seen(item.x, item.y)) continue
    context.fillStyle = rarityColors[item.item.rarity] ?? '#ffffff'
    context.beginPath()
    context.arc(screenX(item.x) + size / 2, screenY(item.y) + size / 2, Math.max(2, size * 0.14), 0, Math.PI * 2)
    context.fill()
  }

  if (mode === 'play') {
    for (const monster of props.monsters) {
      const tracked = props.trackMonster(monster)
      if (!visible(Math.round(tracked.x), Math.round(tracked.y))) continue
      const lunge = props.lunges?.get(monster.id)
      const push = lunge ? lungeOffset(now - lunge.at) : 0
      const spot = lunge && push > 0 ? { x: tracked.x + lunge.x * push, y: tracked.y + lunge.y * push } : tracked
      const cue = props.monsterCues?.get(monster.id)
      if (cue?.clip === 'Windup' && cue.durationMs) windup(context, screenX(spot.x) + size / 2, screenY(spot.y) + size / 2, (monster.radius + (props.strikeReach ?? 0.75)) * size, (now - cue.at) / cue.durationMs)
      const sprite = enemySprite(monster.kind)
      const scale = monster.isBoss ? 1.5 : 1
      place(sprite.sheet, sprite.index, spot.x, spot.y, scale)
      if (monster.frozen || monster.burning) {
        context.fillStyle = monster.frozen ? 'rgba(150, 220, 255, 0.35)' : 'rgba(255, 120, 40, 0.3)'
        context.fillRect(screenX(spot.x), screenY(spot.y), size, size)
      }
      if (monster.health < monster.maxHealth) bar(context, screenX(spot.x), screenY(spot.y) - 4, size, monster.health / Math.max(1, monster.maxHealth), '#e04848')
    }

    for (const projectile of props.projectiles) {
      const spot = props.trackProjectile(projectile)
      if (!visible(Math.round(spot.x), Math.round(spot.y))) continue
      context.fillStyle = projectile.kind === 'Arrow' ? '#f3ead8' : '#9ad8ff'
      context.beginPath()
      context.arc(screenX(spot.x) + size / 2, screenY(spot.y) + size / 2, Math.max(2, size * 0.1), 0, Math.PI * 2)
      context.fill()
    }
  }

  if (mode === 'map') {
    for (const monster of props.monsters) {
      const spot = props.trackMonster(monster)
      const left = screenX(spot.x) + size / 2
      const top = screenY(spot.y) + size / 2
      const radius = Math.max(4, size * 0.6)
      context.fillStyle = '#ff5a4a'
      context.strokeStyle = '#1a0606'
      context.lineWidth = 1.5
      context.beginPath()
      context.moveTo(left, top - radius)
      context.lineTo(left + radius, top)
      context.lineTo(left, top + radius)
      context.lineTo(left - radius, top)
      context.closePath()
      context.fill()
      context.stroke()
      label(context, monster.name, left, top - radius - 4)
    }
  }

  for (const hero of props.heroes) {
    const spot = props.trackHero(hero)
    const left = screenX(spot.x)
    const top = screenY(spot.y)

    if (mode === 'map') {
      context.fillStyle = hero.id === heroId ? '#ffd24a' : hero.dead ? '#ff7a6b' : '#7ec8ff'
      context.beginPath()
      context.arc(left + size / 2, top + size / 2, Math.max(3, size * 0.45), 0, Math.PI * 2)
      context.fill()
      continue
    }

    context.globalAlpha = hero.dead ? 0.35 : hero.invisible || hero.away ? 0.5 : 1
    if (hero.id === heroId) {
      context.strokeStyle = 'rgba(255, 210, 74, 0.8)'
      context.lineWidth = 2
      context.beginPath()
      context.ellipse(left + size / 2, top + size * 0.85, size * 0.38, size * 0.16, 0, 0, Math.PI * 2)
      context.stroke()
    }
    const sprite = classSprite(hero.classId)
    drawSprite(context, sprite.sheet, sprite.index, left, top, size)
    context.globalAlpha = 1
    bar(context, left, top - 4, size, hero.health / Math.max(1, hero.maxHealth), '#4ad06a')
    if (hero.id !== heroId) label(context, hero.away ? `${hero.name} · reconnecting` : hero.name, left + size / 2, top - 8)
  }

  if (mode === 'play' && me) {
    const beacon = (x: number, y: number, name: string, color: string, crown: boolean) =>
      edgeBeacon(context, width, height, screenX(x) + size / 2, screenY(y) + size / 2, name, `${Math.round(Math.hypot(x - center.x, y - center.y))} m`, color, crown)
    for (const hero of props.heroes) {
      if (hero.id === heroId) continue
      const spot = props.trackHero(hero)
      beacon(spot.x, spot.y, hero.name, hero.dead ? '#ff8a7a' : '#7ec8ff', false)
    }
    for (const boss of props.bosses ?? []) {
      const spot = props.trackMonster(boss)
      beacon(spot.x, spot.y, boss.name, '#ff7a5a', true)
    }
  }

  if (sight) {
    const reach = 7.5 * size
    const centerX = screenX(center.x) + size / 2
    const centerY = screenY(center.y) + size / 2
    const shade = context.createRadialGradient(centerX, centerY, reach * 0.45, centerX, centerY, reach * 1.1)
    shade.addColorStop(0, 'rgba(4, 3, 8, 0)')
    shade.addColorStop(1, 'rgba(4, 3, 8, 0.55)')
    context.fillStyle = shade
    context.fillRect(0, 0, width, height)
  }

  if (mode === 'play') {
    for (const spark of props.sparks ?? []) sparkle(context, spark, screenX(spark.x) + size / 2, screenY(spark.y) + size / 2, size, now)
  }

  if (mode === 'play') {
    for (const popup of props.popups) {
      const age = 1 - Math.max(0, popup.until - now) / 900
      context.globalAlpha = Math.max(0, 1 - age)
      context.fillStyle = popupColors[popup.kind]
      context.font = `bold ${Math.round(size * 0.4)}px system-ui, sans-serif`
      context.textAlign = 'center'
      context.fillText(popup.text, screenX(popup.x) + size / 2, screenY(popup.y) - age * size * 0.6)
      context.globalAlpha = 1
    }
  }
}

const beaconMargin = 44
const lungeMs = 180
const sparkSeconds = 0.6
const sparkColors: Record<Spark['kind'], string> = { rubble: '#b8a690', crystal: '#9ff0ff', claw: '#ff4a3a' }

function lungeOffset(elapsed: number): number {
  if (elapsed < 0 || elapsed > lungeMs) return 0
  return Math.sin((elapsed / lungeMs) * Math.PI) * 0.35
}

function windup(context: CanvasRenderingContext2D, x: number, y: number, radius: number, progress: number) {
  if (progress < 0 || progress > 1.15) return
  const grown = Math.min(1, Math.max(0.05, progress))
  context.fillStyle = `rgba(255, 58, 42, ${progress > 1 ? 0.6 * (1 - (progress - 1) / 0.15) : 0.18 + 0.3 * grown})`
  context.beginPath()
  context.arc(x, y, radius * grown, 0, Math.PI * 2)
  context.fill()
  context.strokeStyle = 'rgba(255, 58, 42, 0.9)'
  context.lineWidth = 2
  context.beginPath()
  context.arc(x, y, radius, 0, Math.PI * 2)
  context.stroke()
  if (progress <= 1) {
    context.font = `900 ${Math.round(radius * 0.7)}px system-ui, sans-serif`
    context.textAlign = 'center'
    context.fillStyle = '#ff4a3a'
    context.fillText('!', x, y - radius * 0.9)
  }
}

function sparkle(context: CanvasRenderingContext2D, spark: Spark, x: number, y: number, size: number, now: number) {
  const progress = (now - spark.start) / 1000 / sparkSeconds
  if (progress < 0 || progress > 1) return
  const count = spark.kind === 'claw' ? 6 : 12
  context.globalAlpha = 1 - progress * progress
  context.fillStyle = sparkColors[spark.kind]
  if (spark.kind === 'claw') {
    context.strokeStyle = sparkColors.claw
    context.lineWidth = Math.max(2, size * 0.08)
    for (let mark = -1; mark <= 1; mark++) {
      context.beginPath()
      context.moveTo(x - size * 0.35 + mark * size * 0.15, y - size * 0.35)
      context.lineTo(x + size * 0.35 + mark * size * 0.15, y + size * 0.35)
      context.stroke()
    }
  }
  for (let index = 0; index < count; index++) {
    const angle = ((spark.key * 37 + index * 360) / count) * (Math.PI / 180) + index
    const reach = size * (0.3 + 0.9 * progress) * (0.6 + ((index * 7) % 5) / 10)
    const bit = Math.max(3, size * 0.16 * (1 - progress * 0.6))
    context.fillRect(x + Math.cos(angle) * reach - bit / 2, y + Math.sin(angle) * reach - bit / 2 + progress * size * 0.3, bit, bit)
  }
  context.globalAlpha = 1
}

function edgeShadows(context: CanvasRenderingContext2D, map: string[], x: number, y: number, left: number, top: number, size: number) {
  const band = Math.max(2, size * 0.12)
  context.fillStyle = 'rgba(0, 0, 0, 0.38)'
  if (map[y - 1]?.[x] === '#') context.fillRect(left, top, size, band)
  if (map[y + 1]?.[x] === '#') context.fillRect(left, top + size - band * 0.6, size, band * 0.6)
  if (map[y]?.[x - 1] === '#') context.fillRect(left, top, band, size)
  if (map[y]?.[x + 1] === '#') context.fillRect(left + size - band, top, band, size)
}

function edgeBeacon(
  context: CanvasRenderingContext2D,
  width: number,
  height: number,
  x: number,
  y: number,
  name: string,
  distance: string,
  color: string,
  crown: boolean,
) {
  if (x >= 0 && x <= width && y >= 0 && y <= height) return
  const centerX = width / 2
  const centerY = height / 2
  const dx = x - centerX
  const dy = y - centerY
  const scale = Math.min((centerX - beaconMargin) / Math.max(1e-6, Math.abs(dx)), (centerY - beaconMargin) / Math.max(1e-6, Math.abs(dy)))
  const left = centerX + dx * scale
  const top = centerY + dy * scale
  const angle = Math.atan2(dy, dx)

  context.save()
  context.translate(left, top)
  context.rotate(angle)
  context.fillStyle = color
  context.beginPath()
  context.moveTo(18, 0)
  context.lineTo(8, -7)
  context.lineTo(8, 7)
  context.closePath()
  context.fill()
  context.restore()

  context.fillStyle = 'rgba(10, 16, 28, 0.78)'
  context.strokeStyle = color
  context.lineWidth = 1.5
  context.beginPath()
  context.arc(left, top, 12, 0, Math.PI * 2)
  context.fill()
  context.stroke()
  if (crown) drawSprite(context, sheets.items, crownSprite, left - 10, top - 10, 20)
  else {
    context.fillStyle = color
    context.font = '700 11px system-ui, sans-serif'
    context.textAlign = 'center'
    context.textBaseline = 'middle'
    context.fillText(name.slice(0, 1).toUpperCase(), left, top + 1)
    context.textBaseline = 'alphabetic'
  }
  label(context, `${name} · ${distance}`, left, top > centerY ? top - 18 : top + 26)
}

function bar(context: CanvasRenderingContext2D, x: number, y: number, width: number, fraction: number, color: string) {
  context.fillStyle = 'rgba(0, 0, 0, 0.6)'
  context.fillRect(x + 2, y, width - 4, 3)
  context.fillStyle = color
  context.fillRect(x + 2, y, (width - 4) * Math.max(0, Math.min(1, fraction)), 3)
}

function label(context: CanvasRenderingContext2D, text: string, x: number, y: number) {
  context.font = '600 11px system-ui, sans-serif'
  context.textAlign = 'center'
  context.fillStyle = 'rgba(0, 0, 0, 0.7)'
  context.fillText(text, x + 1, y + 1)
  context.fillStyle = '#dff1ff'
  context.fillText(text, x, y)
}
