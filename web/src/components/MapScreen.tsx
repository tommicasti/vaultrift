import { useEffect, useRef } from 'react'
import type { Direction, EnemyState, GameState, TilePosition } from '../api/types'

interface MapScreenProps {
  game: GameState
  explored: ReadonlySet<string>
  boss?: EnemyState
  onClose: () => void
}

const tileColors: Record<string, string> = {
  '#': '#4a4458',
  L: '#e0542a',
  '~': '#3a7ad0',
  C: '#8fe6ff',
  S: '#a070c0',
  o: '#6a5a4a',
  '^': '#7fd8ff',
  v: '#7fd8ff',
  '<': '#7fd8ff',
  '>': '#7fd8ff',
}

const floorColor = '#9a92a8'
const mapPadding = 2
const maxCellPx = 26
const arrowAngles: Record<Direction, number> = { Up: -Math.PI / 2, Right: 0, Down: Math.PI / 2, Left: Math.PI }

interface Marker {
  at: TilePosition
  glyph: string
  color: string
  label: string
}

export function MapScreen({ game, explored, boss, onClose }: MapScreenProps) {
  const canvas = useRef<HTMLCanvasElement>(null)
  const columns = Math.max(...game.map.map(row => row.length))
  const rows = game.map.length
  const seen = (position: TilePosition) => explored.has(`${position.x},${position.y}`)

  const markers: Marker[] = [
    ...(game.stairs ? [{ at: game.stairs, glyph: '▼', color: '#ffd84a', label: 'Stairs' }] : []),
    ...(game.merchant ? [{ at: game.merchant, glyph: '$', color: '#ffc040', label: 'Merchant' }] : []),
    ...(game.waystone ? [{ at: game.waystone, glyph: '◆', color: '#7ae8ff', label: 'Waystone' }] : []),
    ...(game.totem ? [{ at: game.totem, glyph: '♜', color: '#d090ff', label: 'Totem' }] : []),
    ...game.chests.map(chest => ({ at: chest, glyph: '■', color: '#c8843a', label: 'Chest' })),
  ].filter(marker => seen(marker.at))

  if (game.grave) markers.push({ at: game.grave, glyph: '✝', color: '#ff3a3a', label: 'Your grave' })
  if (boss) markers.push({ at: boss, glyph: '♛', color: '#ff5a4a', label: boss.name })

  useEffect(() => {
    const element = canvas.current
    const context = element?.getContext('2d')
    if (!element || !context) return

    const bounds = { left: game.player.x, top: game.player.y, right: game.player.x, bottom: game.player.y }
    for (const key of explored) {
      const [x, y] = key.split(',').map(Number)
      bounds.left = Math.min(bounds.left, x)
      bounds.top = Math.min(bounds.top, y)
      bounds.right = Math.max(bounds.right, x)
      bounds.bottom = Math.max(bounds.bottom, y)
    }
    const left = Math.max(0, bounds.left - mapPadding)
    const top = Math.max(0, bounds.top - mapPadding)
    const width = Math.min(columns, bounds.right + mapPadding + 1) - left
    const height = Math.min(rows, bounds.bottom + mapPadding + 1) - top

    const ratio = window.devicePixelRatio || 1
    const maxWidth = Math.min(window.innerWidth * 0.9, 960)
    const maxHeight = window.innerHeight * 0.68
    const size = Math.max(4, Math.min(maxCellPx, Math.floor(Math.min(maxWidth / width, maxHeight / height))))
    element.width = width * size * ratio
    element.height = height * size * ratio
    element.style.width = `${width * size}px`
    element.style.height = `${height * size}px`
    context.setTransform(ratio, 0, 0, ratio, -left * size * ratio, -top * size * ratio)
    context.clearRect(left * size, top * size, width * size, height * size)

    game.map.forEach((row, y) => {
      for (let x = 0; x < row.length; x++) {
        if (!explored.has(`${x},${y}`)) continue
        context.fillStyle = tileColors[row[x]] ?? floorColor
        context.fillRect(x * size, y * size, size, size)
      }
    })

    context.strokeStyle = 'rgb(0 0 0 / 18%)'
    context.lineWidth = 1
    game.map.forEach((row, y) => {
      for (let x = 0; x < row.length; x++) {
        if (explored.has(`${x},${y}`) && row[x] !== '#') context.strokeRect(x * size + 0.5, y * size + 0.5, size - 1, size - 1)
      }
    })

    context.textAlign = 'center'
    context.textBaseline = 'middle'
    context.font = `bold ${Math.max(9, size * 0.85)}px system-ui, sans-serif`
    for (const marker of markers) {
      const cx = marker.at.x * size + size / 2
      const cy = marker.at.y * size + size / 2
      if (marker.label === 'Your grave') {
        context.beginPath()
        context.arc(cx, cy, size * 0.75, 0, Math.PI * 2)
        context.strokeStyle = '#ff3a3a'
        context.lineWidth = Math.max(1.5, size * 0.12)
        context.stroke()
      }
      context.lineWidth = 3
      context.strokeStyle = 'rgb(0 0 0 / 70%)'
      context.strokeText(marker.glyph, cx, cy)
      context.fillStyle = marker.color
      context.fillText(marker.glyph, cx, cy)
    }

    const player = game.player
    const px = player.x * size + size / 2
    const py = player.y * size + size / 2
    context.save()
    context.translate(px, py)
    context.rotate(arrowAngles[player.facing])
    context.beginPath()
    context.moveTo(size * 0.6, 0)
    context.lineTo(-size * 0.45, -size * 0.42)
    context.lineTo(-size * 0.2, 0)
    context.lineTo(-size * 0.45, size * 0.42)
    context.closePath()
    context.fillStyle = '#4ad0ff'
    context.strokeStyle = '#ffffff'
    context.lineWidth = Math.max(1, size * 0.1)
    context.fill()
    context.stroke()
    context.restore()
  })

  useEffect(() => {
    function handleKey(event: KeyboardEvent) {
      if (event.code === 'Escape' || event.code === 'KeyM') {
        event.preventDefault()
        event.stopPropagation()
        onClose()
      }
    }
    window.addEventListener('keydown', handleKey, true)
    return () => window.removeEventListener('keydown', handleKey, true)
  }, [onClose])

  const legend = [
    { glyph: '▲', color: '#4ad0ff', label: 'You' },
    ...markers.filter((marker, index) => markers.findIndex(other => other.label === marker.label) === index),
  ]

  return (
    <div className="overlay" role="dialog" aria-modal="true" aria-label="Dungeon map" onClick={onClose}>
      <section className="map-screen" onClick={event => event.stopPropagation()}>
        <header className="map-header">
          <h2>
            Floor {game.floor} <span className="label">· map</span>
          </h2>
          <button onClick={onClose}>Close</button>
        </header>
        <div className="map-canvas">
          <canvas ref={canvas} role="img" aria-label={`Map of floor ${game.floor}`} />
        </div>
        <ul className="map-legend">
          {legend.map(entry => (
            <li key={entry.label}>
              <span style={{ color: entry.color }} aria-hidden="true">
                {entry.glyph}
              </span>{' '}
              {entry.label}
            </li>
          ))}
        </ul>
        <p className="label map-hint">The map fills in as you explore. Press M or Esc to close.</p>
      </section>
    </div>
  )
}
