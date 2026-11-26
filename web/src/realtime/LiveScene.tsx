import { Html } from '@react-three/drei'
import { Canvas, useFrame, useThree } from '@react-three/fiber'
import { Suspense, useEffect, useMemo, useRef, useState } from 'react'
import { DirectionalLight, Group, Mesh, MeshBasicMaterial, PCFShadowMap, Plane, Raycaster, Vector2, Vector3 } from 'three'
import type { EquippedItem, ItemInfo } from '../api/types'
import { Actor, type Cue, type Tracked } from '../scene3d/Actor'
import { Decor } from '../scene3d/Decor'
import { DungeonMap } from '../scene3d/DungeonMap'
import { heroGear, type HeroGear } from '../scene3d/heroLook'
import { FloorItem } from '../scene3d/Scene3D'
import { useKit } from '../scene3d/kit'
import { biomeLook, cell, heroIdleClip, heroUrl, modelUrls, monsterLook, rarityColors } from '../scene3d/models'
import { loadZoom, saveZoom } from '../scene3d/viewMode'
import type { FrameBuffer } from './interpolation'
import type { Element } from '../api/types'
import type { LiveHero, LiveItem, LiveMonster, LiveProjectile, LiveWelcome } from './types'
import './live.css'

export interface Popup {
  key: number
  x: number
  y: number
  text: string
  kind: 'damage' | 'critical' | 'miss' | 'hurt' | 'reward' | 'level' | 'burn' | 'frozen'
  until: number
}

export interface LiveCue extends Cue {
  at: number
}

export interface Blast {
  key: number
  x: number
  y: number
  radius: number
  element?: Element | null
  start: number
}

export type MovingProjectile = LiveProjectile & { moving: boolean }

export interface Corpse {
  monster: LiveMonster
  until: number
}

export interface GroundPoint {
  x: number
  y: number
}

interface LiveSceneProps {
  welcome: LiveWelcome
  equipment: EquippedItem[]
  party: ReadonlyMap<number, EquippedItem[]>
  pointer: { current: GroundPoint | null }
  items: LiveItem[]
  projectiles: MovingProjectile[]
  projectileBuffer: FrameBuffer<MovingProjectile>
  blasts: Blast[]
  heroes: LiveHero[]
  monsters: LiveMonster[]
  corpses: Corpse[]
  heroBuffer: FrameBuffer<LiveHero>
  monsterBuffer: FrameBuffer<LiveMonster>
  heroCues: Map<number, Cue>
  monsterCues: Map<number, LiveCue>
  popups: Popup[]
}

const heroScale = 1.3
const voidColor = '#000000'
const cameraOffset = new Vector3(0, 12, 9)
const lookAhead = new Vector3(0, 1.2, 0)
const noBlocks: ReadonlySet<string> = new Set()

const minZoom = 0.6
const maxZoom = 2.2
const zoomStep = 1.15

function clampZoom(zoom: number): number {
  return Math.min(maxZoom, Math.max(minZoom, zoom))
}

export function LiveScene(props: LiveSceneProps) {
  const container = useRef<HTMLDivElement>(null)
  const [zoom, setZoomState] = useState(() => clampZoom(loadZoom(1)))

  const setZoom = (next: number) => {
    const value = clampZoom(next)
    saveZoom(value)
    setZoomState(value)
  }

  useEffect(() => {
    const element = container.current
    if (!element) return

    const handleWheel = (event: WheelEvent) => {
      event.preventDefault()
      setZoomState(current => {
        const value = clampZoom(current * (event.deltaY < 0 ? 1 / 1.08 : 1.08))
        saveZoom(value)
        return value
      })
    }

    element.addEventListener('wheel', handleWheel, { passive: false })
    return () => element.removeEventListener('wheel', handleWheel)
  }, [])

  return (
    <div ref={container} className="scene3d live-scene">
      <Canvas shadows={{ type: PCFShadowMap }} dpr={[1, 2]} camera={{ fov: 42, near: 0.5, far: 260, position: [0, 30, 30] }}>
        <color attach="background" args={[voidColor]} />
        <fog attach="fog" args={[voidColor, 34 * zoom, 90 * zoom]} />
        <Suspense fallback={null}>
          <LiveWorld {...props} zoom={zoom} />
        </Suspense>
      </Canvas>
      <div className="scene3d-vignette" />
      <div className="scene3d-zoom" onPointerDown={event => event.stopPropagation()}>
        <button type="button" aria-label="Zoom in" disabled={zoom <= minZoom} onClick={() => setZoom(zoom / zoomStep)}>
          +
        </button>
        <button type="button" aria-label="Zoom out" disabled={zoom >= maxZoom} onClick={() => setZoom(zoom * zoomStep)}>
          −
        </button>
      </div>
    </div>
  )
}

function LiveWorld({ welcome, equipment, party, pointer, items, projectiles, projectileBuffer, blasts, heroes, monsters, corpses, heroBuffer, monsterBuffer, heroCues, monsterCues, popups, zoom }: LiveSceneProps & { zoom: number }) {
  const kit = useKit(modelUrls.dungeon)
  const look = biomeLook(welcome.biome)
  const gear = useMemo(() => heroGear(equipment), [equipment])
  const me = heroes.find(hero => hero.id === welcome.heroId)
  const focusY = welcome.map.length + 2

  const follow = () => heroBuffer.sample(welcome.heroId, performance.now()) ?? { x: me?.x ?? 0, y: me?.y ?? 0, moving: false }

  return (
    <>
      <hemisphereLight args={[look.ambient, '#000000', 1]} />
      <FollowCamera follow={follow} light={look.light} zoom={zoom} />
      <DungeonMap map={welcome.map} focusY={focusY} biome={welcome.biome} kit={kit} blocked={noBlocks} />
      <Decor map={welcome.map} biome={welcome.biome} blocked={noBlocks} />
      <PointerTracker pointer={pointer} />
      {me && !me.dead && (
        <AimArc
          follow={follow}
          facing={me.facing}
          pointer={pointer}
          reach={me.reach}
          arcDegrees={me.arcDegrees}
          ranged={me.attack === 'Shoot' || me.attack === 'Cast'}
          monsters={monsters}
          monsterBuffer={monsterBuffer}
        />
      )}
      {heroes.map(hero => (
        <HeroActor
          key={hero.id}
          hero={hero}
          label={hero.id === welcome.heroId ? undefined : hero.name}
          mainHand={(hero.id === welcome.heroId ? equipment : party.get(hero.id))?.find(equipped => equipped.slot === 'MainHand')?.item}
          gear={hero.id === welcome.heroId ? gear : heroGear(party.get(hero.id) ?? [])}
          cue={heroCues.get(hero.id)}
          track={() => heroBuffer.sample(hero.id, performance.now()) ?? { x: hero.x, y: hero.y, moving: false }}
        />
      ))}
      {monsters.map(monster => (
        <Suspense key={monster.id} fallback={null}>
          <MonsterActor
            monster={monster}
            cue={monsterCues.get(monster.id)}
            strikeReach={welcome.monsterStrikeReach}
            dying={false}
            track={() => monsterBuffer.sample(monster.id, performance.now()) ?? { x: monster.x, y: monster.y, moving: false }}
          />
        </Suspense>
      ))}
      {corpses.map(corpse => (
        <Suspense key={`corpse-${corpse.monster.id}`} fallback={null}>
          <MonsterActor
            monster={corpse.monster}
            cue={monsterCues.get(corpse.monster.id)}
            strikeReach={welcome.monsterStrikeReach}
            dying
            track={() => ({ x: corpse.monster.x, y: corpse.monster.y, moving: false })}
          />
        </Suspense>
      ))}
      {projectiles.map(projectile => (
        <Projectile key={projectile.id} projectile={projectile} track={() => projectileBuffer.sample(projectile.id, performance.now()) ?? projectile} />
      ))}
      {blasts.map(blast => (
        <BlastRing key={blast.key} blast={blast} />
      ))}
      {items.map(item => (
        <FloorItem key={item.id} x={item.x} y={item.y} item={item.item} />
      ))}
      {popups.map(popup => (
        <Html key={popup.key} position={[popup.x * cell, 3.4, popup.y * cell]} center zIndexRange={[30, 0]}>
          <div className={`live-popup live-popup-${popup.kind}`}>{popup.text}</div>
        </Html>
      ))}
    </>
  )
}

interface HeroActorProps {
  hero: LiveHero
  label?: string
  mainHand: ItemInfo | undefined
  gear: HeroGear
  cue: Cue | undefined
  track: () => Tracked
}

function HeroActor({ hero, label, mainHand, gear, cue, track }: HeroActorProps) {
  return (
    <Actor
      url={heroUrl(hero.classId)}
      x={hero.x}
      y={hero.y}
      rotation={hero.facing}
      idle={heroIdleClip(mainHand)}
      cue={cue}
      scale={heroScale}
      alive={!hero.dead}
      mainHand={gear.mainHandModel}
      offHand={gear.offHandModel}
      outfit={gear.outfit}
      ghostly={hero.invisible}
      track={track}
    >
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.06, 0]}>
        <ringGeometry args={[1.05, 1.35, 40]} />
        <meshBasicMaterial color={label ? '#7ec8ff' : '#ffd36a'} transparent opacity={0.55} />
      </mesh>
      {(hero.bulwark || hero.fury) && (
        <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.07, 0]}>
          <ringGeometry args={[1.45, 1.75, 40]} />
          <meshBasicMaterial color={hero.bulwark ? '#6fb8ff' : '#ff5a3a'} transparent opacity={0.8} depthWrite={false} />
        </mesh>
      )}
      {label && (
        <Html position={[0, 5.2, 0]} center zIndexRange={[20, 0]}>
          <div className="live-name">{label}</div>
        </Html>
      )}
    </Actor>
  )
}

interface MonsterActorProps {
  monster: LiveMonster
  cue: LiveCue | undefined
  strikeReach: number
  dying: boolean
  track: () => Tracked
}

function MonsterActor({ monster, cue, strikeReach, dying, track }: MonsterActorProps) {
  const look = useMemo(() => monsterLook(monster.kind), [monster.kind])
  const modelCue = useMemo(() => {
    if (!cue) return undefined
    if (cue.clip === 'Windup') return { ...cue, clip: look.procedural ? 'Charge' : look.clips.Attack, durationMs: look.procedural ? cue.durationMs : (cue.durationMs ?? 0) / gltfStrikeAt }
    return { ...cue, clip: look.clips[cue.clip as keyof typeof look.clips] ?? cue.clip }
  }, [cue, look])
  const windup = cue?.clip === 'Windup' && cue.durationMs ? { start: cue.at, ms: cue.durationMs } : undefined
  const wounded = monster.health < monster.maxHealth
  const size = look.height / 2.7

  return (
    <Actor
      url={look.url}
      build={look.build}
      x={monster.x}
      y={monster.y}
      rotation={monster.facing}
      idle={look.clips[monster.awake ? 'Idle' : 'Sleep']}
      walk={look.clips.Walk}
      cue={modelCue}
      tint={look.tint}
      scale={look.scale}
      mainHand={look.weapon}
      offHand={look.offHand}
      ghostly={monster.kind === 'ghost'}
      sinking={dying}
      procedural={look.procedural}
      track={track}
    >
      {!dying && (monster.burning || monster.frozen) && <StatusRing color={monster.frozen ? '#9fe3ff' : '#ff8a2a'} size={size} flicker={monster.burning} />}
      {!dying && windup && <WindupRing key={cue?.id} start={windup.start} ms={windup.ms} radius={(monster.radius + strikeReach) * cell} />}
      {!dying && monster.rarity !== 'Common' && (
        <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.05, 0]}>
          <ringGeometry args={[1.1 * size, 1.35 * size, 32]} />
          <meshBasicMaterial color={rarityColors[monster.rarity] ?? '#ffffff'} transparent opacity={0.6} />
        </mesh>
      )}
      {!dying && (wounded || monster.isBoss) && (
        <Html position={[0, look.height + (monster.isBoss ? 1.2 : 0.5), 0]} center zIndexRange={[20, 0]}>
          <div className={`live-bar${monster.isBoss ? ' boss' : ''}`}>
            {monster.isBoss && <span className="live-bar-name">{monster.name}</span>}
            <div className="live-bar-track">
              <div className="live-bar-fill" style={{ width: `${(monster.health / monster.maxHealth) * 100}%` }} />
            </div>
          </div>
        </Html>
      )}
    </Actor>
  )
}

const gltfStrikeAt = 0.5
const blastMs = 550
const elementColors: Record<Element, string> = { Fire: '#ff7a2a', Ice: '#8fe0ff', Lightning: '#ffe45a', Wind: '#b6ffcf' }

function StatusRing({ color, size, flicker }: { color: string; size: number; flicker: boolean }) {
  const ring = useRef<Mesh>(null)

  useFrame(({ clock }) => {
    if (!ring.current) return
    const material = ring.current.material as MeshBasicMaterial
    material.opacity = flicker ? 0.45 + 0.35 * Math.abs(Math.sin(clock.elapsedTime * 9)) : 0.7
  })

  return (
    <mesh ref={ring} rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.1, 0]}>
      <ringGeometry args={[1.0 * size, 1.5 * size, 32]} />
      <meshBasicMaterial color={color} transparent opacity={0.7} depthWrite={false} />
    </mesh>
  )
}

function Projectile({ projectile, track }: { projectile: MovingProjectile; track: () => Tracked }) {
  const group = useRef<Group>(null)
  const color = projectile.element ? elementColors[projectile.element] : '#f3e2c0'

  useFrame(() => {
    const spot = track()
    group.current?.position.set(spot.x * cell, 2, spot.y * cell)
  })

  return (
    <group ref={group} rotation={[0, projectile.facing, 0]}>
      {projectile.kind === 'Arrow' ? (
        <>
          <mesh rotation={[Math.PI / 2, 0, 0]}>
            <cylinderGeometry args={[0.13, 0.13, 2.4, 6]} />
            <meshBasicMaterial color={color} />
          </mesh>
          <mesh rotation={[Math.PI / 2, 0, 0]} position={[0, 0, -1.6]}>
            <coneGeometry args={[0.32, 1.2, 8, 1, true]} />
            <meshBasicMaterial color={color} transparent opacity={0.35} depthWrite={false} />
          </mesh>
        </>
      ) : (
        <>
          <mesh>
            <sphereGeometry args={[0.8, 14, 14]} />
            <meshBasicMaterial color={color} />
          </mesh>
          <pointLight color={color} intensity={18} distance={9} decay={1.6} />
        </>
      )}
    </group>
  )
}

function BlastRing({ blast }: { blast: Blast }) {
  const ring = useRef<Mesh>(null)
  const color = blast.element ? elementColors[blast.element] : '#ffffff'
  const radius = Math.max(0.6, blast.radius) * cell

  useFrame(() => {
    if (!ring.current) return
    const progress = (performance.now() - blast.start) / blastMs
    ring.current.visible = progress >= 0 && progress <= 1
    ring.current.scale.setScalar(0.2 + 0.8 * Math.min(1, Math.max(0, progress)))
    ;(ring.current.material as MeshBasicMaterial).opacity = 0.75 * (1 - Math.min(1, Math.max(0, progress)))
  })

  return (
    <mesh ref={ring} visible={false} rotation={[-Math.PI / 2, 0, 0]} position={[blast.x * cell, 0.12, blast.y * cell]}>
      <circleGeometry args={[radius, 40]} />
      <meshBasicMaterial color={color} transparent opacity={0.75} depthWrite={false} />
    </mesh>
  )
}
const groundPlane = new Plane(new Vector3(0, 1, 0), 0)

function PointerTracker({ pointer }: { pointer: { current: GroundPoint | null } }) {
  const { camera, gl } = useThree()

  useEffect(() => {
    const element = gl.domElement
    const raycaster = new Raycaster()
    const screen = new Vector2()
    const hit = new Vector3()

    const handleMove = (event: PointerEvent) => {
      if (event.pointerType !== 'mouse') return
      const bounds = element.getBoundingClientRect()
      screen.set(((event.clientX - bounds.left) / bounds.width) * 2 - 1, -((event.clientY - bounds.top) / bounds.height) * 2 + 1)
      raycaster.setFromCamera(screen, camera)
      if (raycaster.ray.intersectPlane(groundPlane, hit)) pointer.current = { x: hit.x / cell, y: hit.z / cell }
    }
    const handleLeave = () => {
      pointer.current = null
    }

    element.addEventListener('pointermove', handleMove)
    element.addEventListener('pointerleave', handleLeave)
    return () => {
      element.removeEventListener('pointermove', handleMove)
      element.removeEventListener('pointerleave', handleLeave)
    }
  }, [camera, gl, pointer])

  return null
}

interface AimArcProps {
  follow: () => Tracked
  facing: number
  pointer: { current: GroundPoint | null }
  reach: number
  arcDegrees: number
  ranged: boolean
  monsters: LiveMonster[]
  monsterBuffer: FrameBuffer<LiveMonster>
}

function AimArc({ follow, facing, pointer, reach, arcDegrees, ranged, monsters, monsterBuffer }: AimArcProps) {
  const arc = useRef<Group>(null)
  const mark = useRef<Group>(null)
  const half = (arcDegrees / 2) * (Math.PI / 180)
  const radius = (reach + 0.35) * cell

  useFrame(() => {
    const spot = follow()
    const target = pointer.current
    const angle = target && Math.hypot(target.x - spot.x, target.y - spot.y) > 0.2 ? Math.atan2(target.x - spot.x, target.y - spot.y) : facing

    if (arc.current) {
      arc.current.position.set(spot.x * cell, 0.07, spot.y * cell)
      arc.current.rotation.set(0, angle, 0)
    }

    const now = performance.now()
    let best: { x: number; y: number; distance: number } | null = null
    for (const monster of monsters) {
      const place = monsterBuffer.sample(monster.id, now) ?? monster
      const dx = place.x - spot.x
      const dy = place.y - spot.y
      const distance = Math.hypot(dx, dy)
      if (distance > reach + monster.radius) continue
      if (ranged) {
        const across = Math.abs(dx * Math.cos(angle) - dy * Math.sin(angle))
        const along = dx * Math.sin(angle) + dy * Math.cos(angle)
        if (along < 0 || across > monster.radius + 0.15) continue
      }
      const offset = Math.abs(Math.atan2(Math.sin(Math.atan2(dx, dy) - angle), Math.cos(Math.atan2(dx, dy) - angle)))
      if (!ranged && distance > 1e-3 && offset > half) continue
      if (!best || distance < best.distance) best = { x: place.x, y: place.y, distance }
    }

    if (mark.current) {
      mark.current.visible = best !== null
      if (best) mark.current.position.set(best.x * cell, 0.09, best.y * cell)
    }
  })

  return (
    <>
      <group ref={arc}>
        {ranged ? (
          <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0, (reach * cell) / 2 + 0.8]}>
            <planeGeometry args={[0.35, reach * cell]} />
            <meshBasicMaterial color="#ffe7a8" transparent opacity={0.22} depthWrite={false} />
          </mesh>
        ) : (
          <mesh rotation={[-Math.PI / 2, 0, -Math.PI / 2 - half]}>
            <ringGeometry args={[1.5, radius, 32, 1, 0, half * 2]} />
            <meshBasicMaterial color="#ffe7a8" transparent opacity={0.16} depthWrite={false} />
          </mesh>
        )}
      </group>
      <group ref={mark} visible={false}>
        <mesh rotation={[-Math.PI / 2, 0, 0]}>
          <ringGeometry args={[1.7, 2.05, 36]} />
          <meshBasicMaterial color="#ffffff" transparent opacity={0.85} depthWrite={false} />
        </mesh>
      </group>
    </>
  )
}

function WindupRing({ start, ms, radius }: { start: number; ms: number; radius: number }) {
  const ring = useRef<Mesh>(null)
  const fill = useRef<Mesh>(null)

  useFrame(() => {
    const progress = (performance.now() - start) / ms
    const visible = progress >= 0 && progress <= 1.15
    if (ring.current) ring.current.visible = visible
    if (fill.current) {
      fill.current.visible = visible
      const grown = Math.min(1, Math.max(0.05, progress))
      fill.current.scale.setScalar(grown)
      const material = fill.current.material as MeshBasicMaterial
      material.opacity = progress > 1 ? 0.75 * (1 - (progress - 1) / 0.15) : 0.25 + 0.35 * grown
    }
  })

  return (
    <group position={[0, 0.08, 0]} rotation={[-Math.PI / 2, 0, 0]}>
      <mesh ref={ring} visible={false}>
        <ringGeometry args={[radius * 0.93, radius, 48]} />
        <meshBasicMaterial color="#ff3a2a" transparent opacity={0.85} depthWrite={false} />
      </mesh>
      <mesh ref={fill} visible={false}>
        <circleGeometry args={[radius * 0.93, 48]} />
        <meshBasicMaterial color="#ff3a2a" transparent opacity={0.3} depthWrite={false} />
      </mesh>
    </group>
  )
}

function FollowCamera({ follow, light, zoom }: { follow: () => Tracked; light: string; zoom: number }) {
  const { camera } = useThree()
  const sun = useRef<DirectionalLight>(null)
  const glow = useRef<Group>(null)
  const focus = useRef<Vector3 | null>(null)

  useFrame((_, delta) => {
    const spot = follow()
    const target = new Vector3(spot.x * cell, 0, spot.y * cell)
    if (!focus.current || focus.current.distanceTo(target) > cell * 9) focus.current = target.clone()
    else focus.current.lerp(target, Math.min(1, delta * 10))

    camera.position.copy(focus.current).addScaledVector(cameraOffset, zoom)
    camera.lookAt(focus.current.clone().add(lookAhead))

    if (sun.current) {
      sun.current.position.copy(focus.current).add(new Vector3(-8, 22, 10))
      sun.current.target.position.copy(focus.current)
      sun.current.target.updateMatrixWorld()
    }

    glow.current?.position.copy(focus.current)
  })

  return (
    <>
      <directionalLight
        ref={sun}
        intensity={0.7}
        color={light}
        castShadow
        shadow-mapSize={[1024, 1024]}
        shadow-camera-left={-30}
        shadow-camera-right={30}
        shadow-camera-top={30}
        shadow-camera-bottom={-30}
        shadow-bias={-0.0008}
      />
      <group ref={glow}>
        <pointLight position={[0, 5, 1]} color={light} intensity={70} distance={34} decay={1.4} />
      </group>
    </>
  )
}
