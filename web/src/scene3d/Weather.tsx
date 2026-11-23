import { useFrame } from '@react-three/fiber'
import { useMemo, useRef } from 'react'
import { AdditiveBlending, BufferAttribute, BufferGeometry, CanvasTexture, NormalBlending, Points, PointsMaterial } from 'three'
import { cell } from './models'

interface WeatherStyle {
  count: number
  color: string
  size: number
  fall: number
  drift: number
  sway: number
  glow: boolean
  secondColor?: string
}

const weather: Record<string, WeatherStyle> = {
  volcano: { count: 420, color: '#8a8580', secondColor: '#ff7a2a', size: 0.22, fall: 1.1, drift: 0.4, sway: 0.6, glow: false },
  snow: { count: 520, color: '#ffffff', size: 0.28, fall: 1.6, drift: 0.3, sway: 1.1, glow: false },
  swamp: { count: 160, color: '#c8ff6a', size: 0.3, fall: -0.15, drift: 0.2, sway: 1.4, glow: true },
  forest: { count: 180, color: '#d89a3a', secondColor: '#7aa84a', size: 0.3, fall: 0.9, drift: 0.5, sway: 1.6, glow: false },
  desert: { count: 360, color: '#e8c890', size: 0.16, fall: 0.15, drift: 4.5, sway: 0.4, glow: false },
  fungal: { count: 260, color: '#d08aff', size: 0.24, fall: -0.45, drift: 0.2, sway: 0.9, glow: true },
  rift: { count: 260, color: '#c07aff', secondColor: '#6a3aff', size: 0.22, fall: -0.6, drift: 0.3, sway: 1.2, glow: true },
  sky: { count: 140, color: '#ffffff', size: 0.5, fall: 0.05, drift: 2.4, sway: 0.3, glow: false },
  crypt: { count: 120, color: '#cfc8b8', size: 0.12, fall: 0.15, drift: 0.15, sway: 0.5, glow: false },
  castle: { count: 120, color: '#cfc8b8', size: 0.12, fall: 0.15, drift: 0.15, sway: 0.5, glow: false },
  ruins: { count: 140, color: '#9af0d0', size: 0.14, fall: -0.1, drift: 0.2, sway: 0.7, glow: true },
  mines: { count: 140, color: '#a89880', size: 0.12, fall: 0.25, drift: 0.1, sway: 0.4, glow: false },
}

const span = cell * 9
const height = 5

let dot: CanvasTexture | null = null

function roundDot(): CanvasTexture {
  if (dot) return dot
  const canvas = document.createElement('canvas')
  canvas.width = canvas.height = 32
  const context = canvas.getContext('2d')
  if (context) {
    const gradient = context.createRadialGradient(16, 16, 0, 16, 16, 16)
    gradient.addColorStop(0, 'rgba(255,255,255,1)')
    gradient.addColorStop(0.5, 'rgba(255,255,255,0.8)')
    gradient.addColorStop(1, 'rgba(255,255,255,0)')
    context.fillStyle = gradient
    context.fillRect(0, 0, 32, 32)
  }
  dot = new CanvasTexture(canvas)
  return dot
}

function noise(index: number, salt: number): number {
  let value = Math.imul(index ^ (salt * 0x9e3779b1), 0x85ebca6b)
  value = Math.imul(value ^ (value >>> 13), 0xc2b2ae35)
  return ((value ^ (value >>> 16)) >>> 0) / 4294967295
}

function wrap(value: number, size: number): number {
  return ((((value + size / 2) % size) + size) % size) - size / 2
}

interface WeatherProps {
  biome: string | undefined
  x: number
  y: number
  lit: (x: number, y: number) => boolean
}

export function Weather({ biome, x, y, lit }: WeatherProps) {
  const style = weather[biome ?? ''] ?? weather.crypt
  const points = useRef<Points>(null)

  const { geometry, seeds } = useMemo(() => {
    const positions = new Float32Array(style.count * 3)
    const colors = new Float32Array(style.count * 3)
    const seeds = new Float32Array(style.count * 4)
    const first = hexToRgb(style.color)
    const second = hexToRgb(style.secondColor ?? style.color)
    for (let index = 0; index < style.count; index++) {
      seeds[index * 4] = noise(index, 1) * span - span / 2
      seeds[index * 4 + 1] = noise(index, 2) * height
      seeds[index * 4 + 2] = noise(index, 3) * span - span / 2
      seeds[index * 4 + 3] = noise(index, 4) * Math.PI * 2
      const color = index % 4 === 0 ? second : first
      colors.set(color, index * 3)
    }
    const geometry = new BufferGeometry()
    geometry.setAttribute('position', new BufferAttribute(positions, 3))
    geometry.setAttribute('color', new BufferAttribute(colors, 3))
    return { geometry, seeds }
  }, [style])

  const material = useMemo(
    () =>
      new PointsMaterial({
        size: style.size,
        map: roundDot(),
        vertexColors: true,
        transparent: true,
        opacity: style.glow ? 0.95 : 0.8,
        depthWrite: false,
        blending: style.glow ? AdditiveBlending : NormalBlending,
      }),
    [style],
  )

  useFrame(({ clock }) => {
    const cloud = points.current
    if (!cloud) return

    const time = clock.elapsedTime
    const attribute = cloud.geometry.getAttribute('position') as BufferAttribute
    const centerX = x * cell
    const centerZ = y * cell
    for (let index = 0; index < style.count; index++) {
      const seedX = seeds[index * 4]
      const seedY = seeds[index * 4 + 1]
      const seedZ = seeds[index * 4 + 2]
      const phase = seeds[index * 4 + 3]
      const fallen = wrap(seedY - time * style.fall, height) + height / 2
      const px = wrap(seedX + time * style.drift + Math.sin(time * 0.8 + phase) * style.sway - centerX, span) + centerX
      const pz = wrap(seedZ + Math.cos(time * 0.6 + phase) * style.sway * 0.5 - centerZ, span) + centerZ
      attribute.setXYZ(index, px, lit(Math.round(px / cell), Math.round(pz / cell)) ? fallen : -50, pz)
    }
    attribute.needsUpdate = true
  })

  return <points ref={points} geometry={geometry} material={material} frustumCulled={false} />
}

function hexToRgb(hex: string): [number, number, number] {
  const value = parseInt(hex.slice(1), 16)
  return [((value >> 16) & 255) / 255, ((value >> 8) & 255) / 255, (value & 255) / 255]
}
