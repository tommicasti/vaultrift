import { Html } from '@react-three/drei'
import { useFrame } from '@react-three/fiber'
import { useMemo, useRef, useState } from 'react'
import { AdditiveBlending, DoubleSide, Group, Mesh, MeshBasicMaterial, MeshStandardMaterial, Vector3 } from 'three'
import type { TilePosition } from '../api/types'
import { cell } from './models'

export interface Burst3D {
  key: string
  x: number
  y: number
  kind: string
  delayMs: number
}

export interface Projectile3D {
  key: string
  kind: 'arrow' | 'power' | 'fire' | 'ice' | 'lightning' | 'wind' | 'impact'
  from: TilePosition
  tiles: TilePosition[]
  delayMs: number
}

type Shape = 'sphere' | 'shard' | 'ring' | 'cone' | 'dome'

interface BurstStyle {
  color: string
  count: number
  shape: Shape
  size: number
  spread: number
  rise: number
  core?: Shape
  coreSize?: number
  seconds?: number
}

const styles: Record<string, BurstStyle> = {
  fire: { color: '#ff7a2a', count: 9, shape: 'sphere', size: 0.32, spread: 1.4, rise: 2.4, core: 'sphere', coreSize: 1.1 },
  ice: { color: '#a8ecff', count: 8, shape: 'shard', size: 0.42, spread: 2, rise: 0.8, core: 'shard', coreSize: 0.9 },
  lightning: { color: '#fff27a', count: 6, shape: 'shard', size: 0.25, spread: 1.6, rise: 0.4, core: 'cone', coreSize: 1 },
  wind: { color: '#e8f4ff', count: 0, shape: 'ring', size: 1, spread: 0, rise: 0, core: 'ring', coreSize: 1.4 },
  sweep: { color: '#ffffff', count: 0, shape: 'ring', size: 1, spread: 0, rise: 0, core: 'ring', coreSize: 1.6 },
  thrust: { color: '#ffffff', count: 3, shape: 'shard', size: 0.3, spread: 0.6, rise: 0.2, core: 'cone', coreSize: 0.7 },
  impact: { color: '#ffffff', count: 6, shape: 'shard', size: 0.22, spread: 1.4, rise: 0.6, core: 'shard', coreSize: 0.7 },
  block: { color: '#7ab8ff', count: 0, shape: 'ring', size: 1, spread: 0, rise: 0, core: 'ring', coreSize: 1.1 },
  fury: { color: '#ff3a3a', count: 10, shape: 'cone', size: 0.35, spread: 2.2, rise: 0.4, core: 'ring', coreSize: 2.1, seconds: 0.7 },
  bulwark: { color: '#7ab8ff', count: 0, shape: 'dome', size: 1, spread: 0, rise: 0, core: 'dome', coreSize: 2.2, seconds: 0.8 },
  smoke: { color: '#9a9aa8', count: 10, shape: 'sphere', size: 0.55, spread: 1.6, rise: 1.4, seconds: 0.8 },
  shards: { color: '#8fe6ff', count: 9, shape: 'shard', size: 0.35, spread: 2.2, rise: 1.2 },
  spores: { color: '#a8e05a', count: 10, shape: 'sphere', size: 0.18, spread: 1.4, rise: 2, seconds: 0.8 },
  overload: { color: '#c07aff', count: 10, shape: 'sphere', size: 0.25, spread: 2.4, rise: 1, core: 'sphere', coreSize: 1.8, seconds: 0.7 },
  cast: { color: '#c89aff', count: 8, shape: 'shard', size: 0.16, spread: 1.6, rise: 2.6, core: 'ring', coreSize: 1.7, seconds: 0.7 },
  learn: { color: '#ffd24a', count: 12, shape: 'shard', size: 0.18, spread: 1.4, rise: 3.2, core: 'ring', coreSize: 2, seconds: 1 },
  debris: { color: '#8a6a4a', count: 12, shape: 'shard', size: 0.28, spread: 2.6, rise: 1.6, seconds: 0.6 },
  soul: { color: '#d8e8ff', count: 8, shape: 'sphere', size: 0.2, spread: 0.8, rise: 3.4, core: 'ring', coreSize: 1.4, seconds: 0.9 },
  roar: { color: '#ff5a3a', count: 0, shape: 'ring', size: 1, spread: 0, rise: 0, core: 'ring', coreSize: 3.4, seconds: 0.9 },
}

function seeded(text: string): () => number {
  let state = 0
  for (const char of text) state = (Math.imul(state, 31) + char.charCodeAt(0)) | 0
  return () => {
    state = (Math.imul(state ^ (state >>> 15), 2246822507) + 0x6d2b79f5) | 0
    return ((state >>> 0) % 10000) / 10000
  }
}

function Geometry({ shape }: { shape: Shape }) {
  switch (shape) {
    case 'shard':
      return <octahedronGeometry args={[1, 0]} />
    case 'ring':
      return <torusGeometry args={[1, 0.1, 6, 28]} />
    case 'cone':
      return <coneGeometry args={[0.5, 1.6, 5]} />
    case 'dome':
      return <sphereGeometry args={[1, 18, 10, 0, Math.PI * 2, 0, Math.PI / 2]} />
    default:
      return <icosahedronGeometry args={[1, 1]} />
  }
}

export function Bursts({ bursts }: { bursts: Burst3D[] }) {
  return (
    <>
      {bursts.map(burst => (
        <BurstView key={burst.key} burst={burst} />
      ))}
    </>
  )
}

interface Particle {
  direction: Vector3
  spin: number
}

function BurstView({ burst }: { burst: Burst3D }) {
  const style = styles[burst.kind] ?? styles.impact
  const seconds = style.seconds ?? 0.5
  const group = useRef<Group>(null)
  const [born] = useState(() => performance.now() + burst.delayMs)
  const particles = useMemo<Particle[]>(() => {
    const random = seeded(burst.key)
    return Array.from({ length: style.count }, () => {
      const angle = random() * Math.PI * 2
      const reach = 0.5 + random() * 0.5
      return { direction: new Vector3(Math.cos(angle) * reach, 0.3 + random() * 0.7, Math.sin(angle) * reach), spin: random() * 6 }
    })
  }, [burst.key, style.count])
  const material = useMemo(
    () => new MeshStandardMaterial({ color: style.color, emissive: style.color, emissiveIntensity: 1.4, transparent: true, depthWrite: false }),
    [style.color],
  )

  useFrame(() => {
    const root = group.current
    if (!root) return

    const progress = (performance.now() - born) / 1000 / seconds
    root.visible = progress >= 0 && progress <= 1
    if (!root.visible) return

    const shared = (root.children[0] as Mesh | undefined)?.material as MeshStandardMaterial | undefined
    if (shared) shared.opacity = 1 - progress * progress
    root.children.forEach((child, index) => {
      if (index === 0 && style.core) {
        const grow = style.core === 'cone' ? 1 : 0.4 + progress * 0.9
        child.scale.setScalar(grow * (style.coreSize ?? 1))
        if (style.core === 'cone') child.scale.set(0.5, 3 * (1 - progress * 0.5), 0.5)
        child.rotation.z = progress * 2
        return
      }

      const particle = particles[style.core ? index - 1 : index]
      if (!particle) return
      child.position.set(
        particle.direction.x * style.spread * progress,
        particle.direction.y * style.rise * progress,
        particle.direction.z * style.spread * progress,
      )
      child.rotation.set(particle.spin * progress, particle.spin * progress * 1.3, 0)
      child.scale.setScalar(style.size * (1 - progress * 0.6))
    })
  })

  const coreY = style.core === 'ring' ? 0.3 : style.core === 'dome' ? 0 : style.core === 'cone' ? 2.4 : 1.2

  return (
    <group ref={group} visible={false} position={[burst.x * cell, 0, burst.y * cell]}>
      {style.core && (
        <mesh material={material} position={[0, coreY, 0]} rotation={style.core === 'ring' ? [-Math.PI / 2, 0, 0] : style.core === 'cone' ? [Math.PI, 0, 0] : [0, 0, 0]}>
          <Geometry shape={style.core} />
        </mesh>
      )}
      {particles.map((_, index) => (
        <mesh key={index} material={material} position={[0, 1.2, 0]}>
          <Geometry shape={style.shape} />
        </mesh>
      ))}
    </group>
  )
}

export const levelUpSeconds = 4
const levelUpSparks = 16

function fade(progress: number): number {
  if (progress < 0.08) return progress / 0.08
  if (progress < 0.75) return 1
  return Math.max(0, 1 - (progress - 0.75) / 0.25)
}

export function LevelUpAura({ burst, x, y }: { burst: Burst3D; x: number; y: number }) {
  const group = useRef<Group>(null)
  const [born] = useState(() => performance.now() + burst.delayMs)
  const materials = useMemo(
    () => ({
      column: new MeshBasicMaterial({ color: '#ffd84a', transparent: true, opacity: 0, depthWrite: false, blending: AdditiveBlending, side: DoubleSide }),
      ring: new MeshBasicMaterial({ color: '#fff0a0', transparent: true, opacity: 0, depthWrite: false, blending: AdditiveBlending, side: DoubleSide }),
      spark: new MeshBasicMaterial({ color: '#fff6c8', transparent: true, opacity: 0, depthWrite: false, blending: AdditiveBlending }),
    }),
    [],
  )
  const sparks = useMemo(() => {
    const random = seeded(burst.key)
    return Array.from({ length: levelUpSparks }, (_, index) => ({ angle: (index / levelUpSparks) * Math.PI * 2 + random(), speed: 0.7 + random() * 0.6, lag: random() * 0.3 }))
  }, [burst.key])

  useFrame(({ clock }, delta) => {
    const root = group.current
    if (!root) return

    root.position.lerp(new Vector3(x * cell, 0, y * cell), Math.min(1, Math.min(delta, 1 / 20) * 8))
    const progress = (performance.now() - born) / 1000 / levelUpSeconds
    root.visible = progress >= 0 && progress <= 1
    if (!root.visible) return

    const glow = fade(progress)
    const [column, lower, upper] = root.children as Mesh[]
    const rest = root.children.slice(3, 3 + levelUpSparks) as Mesh[]
    ;(column.material as MeshBasicMaterial).opacity = glow * (0.45 + Math.sin(clock.elapsedTime * 10) * 0.1)
    column.scale.set(1 + Math.sin(clock.elapsedTime * 4) * 0.06, 0.4 + Math.min(1, progress * 6) * 0.6, 1 + Math.sin(clock.elapsedTime * 4) * 0.06)
    ;(lower.material as MeshBasicMaterial).opacity = glow
    for (const [ring, offset] of [[lower, 0], [upper, 0.35]] as const) {
      const climb = (progress * 2.6 + offset) % 1
      ring.position.y = 0.2 + climb * 4.6
      ring.scale.setScalar(1.5 - climb * 0.5)
    }
    if (rest[0]) (rest[0].material as MeshBasicMaterial).opacity = glow
    rest.forEach((spark, index) => {
      const data = sparks[index]
      const local = ((Math.max(0, progress - data.lag * 0.2) * data.speed * 2.2) % 1)
      const angle = data.angle + local * 7
      const radius = 1.25 - local * 0.4
      spark.position.set(Math.cos(angle) * radius, 0.3 + local * 5.2, Math.sin(angle) * radius)
      spark.scale.setScalar(0.13 * (1 - local * 0.6))
    })
  })

  return (
    <group ref={group} visible={false} position={[burst.x * cell, 0, burst.y * cell]}>
      <mesh material={materials.column} position={[0, 2.4, 0]}>
        <cylinderGeometry args={[1.3, 1.5, 4.8, 24, 1, true]} />
      </mesh>
      <mesh material={materials.ring} rotation={[-Math.PI / 2, 0, 0]}>
        <torusGeometry args={[1, 0.06, 6, 32]} />
      </mesh>
      <mesh material={materials.ring} rotation={[-Math.PI / 2, 0, 0]}>
        <torusGeometry args={[1, 0.06, 6, 32]} />
      </mesh>
      {sparks.map((_, index) => (
        <mesh key={index} material={materials.spark}>
          <octahedronGeometry args={[1, 0]} />
        </mesh>
      ))}
      <Html position={[0, 5.4, 0]} center zIndexRange={[20, 0]}>
        <span className="scene3d-levelup" style={{ animationDelay: `${burst.delayMs}ms`, animationDuration: `${levelUpSeconds * 1000}ms` }}>
          Level up!
        </span>
      </Html>
    </group>
  )
}

const projectileColors: Record<Projectile3D['kind'], string> = {
  arrow: '#e8d0a0',
  power: '#ffd04a',
  fire: '#ff7a2a',
  ice: '#a8ecff',
  lightning: '#fff27a',
  wind: '#e8f4ff',
  impact: '#ffffff',
}

export function Projectiles({ projectiles }: { projectiles: Projectile3D[] }) {
  return (
    <>
      {projectiles.map(projectile => (
        <ProjectileView key={projectile.key} projectile={projectile} />
      ))}
    </>
  )
}

export const flightStepMs = 72
const flightHeight = 1.6

function ProjectileView({ projectile }: { projectile: Projectile3D }) {
  const group = useRef<Group>(null)
  const [born] = useState(() => performance.now() + projectile.delayMs)
  const color = projectileColors[projectile.kind]
  const isArrow = projectile.kind === 'arrow' || projectile.kind === 'power'
  const points = useMemo(
    () => [projectile.from, ...projectile.tiles].map(tile => new Vector3(tile.x * cell, flightHeight, tile.y * cell)),
    [projectile.from, projectile.tiles],
  )
  const durationMs = Math.max(1, projectile.tiles.length) * flightStepMs

  useFrame(({ clock }) => {
    const root = group.current
    if (!root || points.length < 2) return

    const elapsed = performance.now() - born
    root.visible = elapsed >= 0 && elapsed <= durationMs
    if (!root.visible) return

    const travel = (elapsed / durationMs) * (points.length - 1)
    const segment = Math.min(points.length - 2, Math.floor(travel))
    const from = points[segment]
    const to = points[segment + 1]
    root.position.lerpVectors(from, to, travel - segment)
    root.lookAt(to.x, to.y, to.z)
    const time = clock.elapsedTime
    root.children.forEach((child, index) => {
      if (isArrow) return
      if (projectile.kind === 'fire') {
        if (index === 0) return
        child.position.set(Math.sin(time * 23 + index * 2) * 0.12, Math.cos(time * 19 + index) * 0.12, -index * 0.32)
        child.scale.setScalar((0.32 - index * 0.045) * (0.85 + Math.sin(time * 30 + index) * 0.15))
      } else if (projectile.kind === 'ice') {
        const angle = time * 10 + (index * Math.PI * 2) / 3
        child.position.set(Math.cos(angle) * 0.25, Math.sin(angle) * 0.25, index === 3 ? -0.5 : 0)
        child.rotation.set(time * 6, time * 4 + index, 0)
      } else if (projectile.kind === 'wind') {
        child.position.set(0, 0, -index * 0.28)
        child.rotation.set(0, 0, time * 14 + index)
        child.scale.setScalar(0.3 + index * 0.12)
      } else child.rotation.set(time * 9, time * 7, 0)
    })
  })

  return (
    <group ref={group} visible={false}>
      {isArrow && (
        <>
          <mesh rotation={[Math.PI / 2, 0, 0]}>
            <cylinderGeometry args={[0.05, 0.05, 1.4, 5]} />
            <meshStandardMaterial color={color} emissive={projectile.kind === 'power' ? color : '#000000'} emissiveIntensity={1.2} />
          </mesh>
          <mesh position={[0, 0, 0.75]} rotation={[Math.PI / 2, 0, 0]}>
            <coneGeometry args={[0.11, 0.3, 5]} />
            <meshStandardMaterial color="#9aa0a8" />
          </mesh>
        </>
      )}
      {projectile.kind === 'fire' &&
        Array.from({ length: 6 }, (_, index) => (
          <mesh key={index} scale={index === 0 ? 0.42 : 0.3}>
            <icosahedronGeometry args={[1, 1]} />
            <meshBasicMaterial color={index === 0 ? '#fff0a0' : index < 3 ? '#ffa02a' : '#e8401a'} transparent opacity={1 - index * 0.13} />
          </mesh>
        ))}
      {projectile.kind === 'ice' &&
        Array.from({ length: 4 }, (_, index) => (
          <mesh key={index} scale={[0.14, 0.32, 0.14]}>
            <octahedronGeometry args={[1, 0]} />
            <meshStandardMaterial color="#dff6ff" emissive="#7ad8ff" emissiveIntensity={1.2} flatShading />
          </mesh>
        ))}
      {projectile.kind === 'wind' &&
        Array.from({ length: 4 }, (_, index) => (
          <mesh key={index}>
            <torusGeometry args={[1, 0.08, 5, 18, Math.PI * 1.5]} />
            <meshBasicMaterial color={index % 2 === 0 ? '#eaffea' : '#9ae0a8'} transparent opacity={0.85 - index * 0.15} />
          </mesh>
        ))}
      {(projectile.kind === 'impact' || projectile.kind === 'lightning') && (
        <mesh scale={0.3}>
          <octahedronGeometry args={[1, 0]} />
          <meshBasicMaterial color={color} />
        </mesh>
      )}
    </group>
  )
}

export interface Arc3D {
  key: string
  points: TilePosition[]
  delayMs: number
}

const arcSeconds = 0.45
const arcKinks = 6

export function Arcs({ arcs }: { arcs: Arc3D[] }) {
  return (
    <>
      {arcs.map(arc => (
        <ArcView key={arc.key} arc={arc} />
      ))}
    </>
  )
}

function ArcView({ arc }: { arc: Arc3D }) {
  const group = useRef<Group>(null)
  const [born] = useState(() => performance.now() + arc.delayMs)
  const segments = (arc.points.length - 1) * arcKinks
  const start = useMemo(() => new Vector3(), [])
  const end = useMemo(() => new Vector3(), [])

  useFrame(({ clock }) => {
    const root = group.current
    if (!root) return

    const progress = (performance.now() - born) / 1000 / arcSeconds
    root.visible = progress >= 0 && progress <= 1
    if (!root.visible) return

    const flicker = Math.floor(clock.elapsedTime * 30)
    const jitter = (index: number, axis: number) => (seeded(`${arc.key}-${flicker}-${index}-${axis}`)() - 0.5) * 1.2
    const point = (index: number, target: Vector3) => {
      const leg = Math.min(arc.points.length - 2, Math.floor(index / arcKinks))
      const along = (index - leg * arcKinks) / arcKinks
      const from = arc.points[leg]
      const to = arc.points[leg + 1]
      const edge = index % arcKinks === 0
      target.set(
        (from.x + (to.x - from.x) * along) * cell + (edge ? 0 : jitter(index, 0)),
        1.4 + (edge ? 0 : jitter(index, 1) * 0.6),
        (from.y + (to.y - from.y) * along) * cell + (edge ? 0 : jitter(index, 2)),
      )
    }

    root.children.forEach((child, index) => {
      point(index, start)
      if (index + 1 === segments) {
        const last = arc.points[arc.points.length - 1]
        end.set(last.x * cell, 1.4, last.y * cell)
      } else point(index + 1, end)
      child.position.copy(start).lerp(end, 0.5)
      child.lookAt(end)
      child.scale.set(1, 1, start.distanceTo(end))
    })
  })

  return (
    <group ref={group} visible={false}>
      {Array.from({ length: segments }, (_, index) => (
        <mesh key={index}>
          <boxGeometry args={[0.12, 0.12, 1]} />
          <meshBasicMaterial color="#fff6a0" />
        </mesh>
      ))}
    </group>
  )
}
