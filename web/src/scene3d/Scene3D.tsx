import { Html, useGLTF, useProgress } from '@react-three/drei'
import { Canvas, useFrame, useThree } from '@react-three/fiber'
import { Suspense, useEffect, useMemo, useRef, useState, type CSSProperties, type PointerEvent, type RefObject } from 'react'
import { DirectionalLight, DoubleSide, PerspectiveCamera, Object3D, MeshBasicMaterial, PCFShadowMap, Group, Mesh, MeshStandardMaterial, Vector3 } from 'three'
import type { Direction, EnemyState, GameState, ItemInfo, TilePosition } from '../api/types'
import type { Effect } from '../game/effects'
import { Compass } from '../components/Compass'
import { ElementIcon } from '../components/ElementIcon'
import { Sprite } from '../components/Sprite'
import { crownSprite, graveSprite, rarityStars, sheets, starSprites } from '../game/sprites'
import { Actor, type Cue } from './Actor'
import { heroGear } from './heroLook'
import { Decor } from './Decor'
import { GridLines, TargetMarks } from './Grid'
import { Weather } from './Weather'
import { DungeonMap } from './DungeonMap'
import { Arcs, Bursts, LevelUpAura, levelUpSeconds, Projectiles, type Burst3D } from './Effects3D'
import { FirstPersonHands } from './FirstPersonHands'
import { buildBow, buildItem, buildSpear, buildStairs, buildTotem } from './creatures'
import { useKit, useKitObjects, type KitPiece } from './kit'
import {
  biomeLook,
  cell,
  heroIdleClip,
  heroUrl,
  merchantUrl,
  modelUrls,
  monsterLook,
  type MonsterClip,
  rarityColors,
  selfCrowned,
  weaponModel,
} from './models'
import { Shroud } from './Shroud'
import { facingAngles, steeredFacing, turned, wrapAngle } from './firstPerson'
import { cuesForTurn, type Popup, type Turn } from './turnCues'
import { layoutKey, remember, visibleTiles, type Visibility } from './vision'
import { loadZoom, saveZoom } from './viewMode'
import './scene3d.css'

interface Scene3DProps {
  game: GameState
  viewColumns: number
  viewRows: number
  effects: Effect[]
  turn: Turn | null
  bossTarget?: EnemyState
  firstPerson: boolean
  paused: boolean
  onFirstPersonChange: (firstPerson: boolean) => void
  onTurn: (direction: Direction) => void
  onAttack: () => void
}

const heroScale = 1.3
const voidColor = '#000000'
const corpseMs = 760

interface Aura {
  burst: Burst3D
  until: number
}

interface Corpse {
  enemy: EnemyState
  until: number
}

const minZoom = 0.6
const maxZoom = 2.2
const zoomStep = 1.15

const lowPower = detectLowPower()

function detectLowPower(): boolean {
  if (typeof window === 'undefined') return false
  const coarse = window.matchMedia?.('(pointer: coarse)').matches ?? false
  const cores = navigator.hardwareConcurrency ?? 8
  return coarse || cores <= 4
}

function clampZoom(zoom: number): number {
  return Math.min(maxZoom, Math.max(minZoom, zoom))
}

export interface LookOffset {
  yaw: number
  pitch: number
  heading: number | null
}

interface Drag {
  pointerId: number
  x: number
  y: number
  moved: boolean
  mouse: boolean
}

const dragThreshold = 6
const yawPerPixel = 0.006
const pitchPerPixel = 0.004
const maxPitch = 0.55
const mouseYawPerPixel = 0.0032
const mousePitchPerPixel = 0.0024
const steerRetryMs = 350

interface Steering {
  asked: Direction | null
  at: number
}

export default function Scene3D({ game, viewColumns, viewRows, effects, turn, bossTarget, firstPerson, paused, onFirstPersonChange, onTurn, onAttack }: Scene3DProps) {
  const [zoom, setZoomState] = useState(() => clampZoom(loadZoom(1)))
  const container = useRef<HTMLDivElement>(null)
  const touches = useRef(new Map<number, { x: number; y: number }>())
  const pinch = useRef<{ distance: number; zoom: number } | null>(null)
  const drag = useRef<Drag | null>(null)
  const look = useRef<LookOffset>({ yaw: 0, pitch: 0, heading: null })
  const settle = useRef<number | null>(null)
  const facing = game.player.facing
  const modes = useRef({ firstPerson, zoom, onFirstPersonChange })
  const [locked, setLocked] = useState(false)
  const steering = useRef<Steering>({ asked: null, at: 0 })
  const live = useRef({ facing, onTurn })
  const canLock = useMemo(() => typeof document !== 'undefined' && 'pointerLockElement' in document && (window.matchMedia?.('(pointer: fine)').matches ?? false), [])

  useEffect(() => {
    modes.current = { firstPerson, zoom, onFirstPersonChange }
  }, [firstPerson, zoom, onFirstPersonChange])

  useEffect(() => {
    live.current = { facing, onTurn }
  }, [facing, onTurn])

  useEffect(() => {
    if (look.current.heading !== null) {
      if (steering.current.asked === facing) steering.current = { asked: null, at: 0 }
      else look.current = { ...look.current, heading: facingAngles[facing] }
      return
    }
    if (drag.current?.moved) return
    look.current = { yaw: 0, pitch: 0, heading: null }
  }, [facing])

  useEffect(() => {
    const element = container.current
    const handleChange = () => {
      const active = element !== null && document.pointerLockElement === element
      setLocked(active)
      steering.current = { asked: null, at: 0 }
      look.current = active
        ? { yaw: 0, pitch: 0, heading: facingAngles[live.current.facing] }
        : { yaw: 0, pitch: 0, heading: null }
    }

    document.addEventListener('pointerlockchange', handleChange)
    return () => {
      document.removeEventListener('pointerlockchange', handleChange)
      if (element !== null && document.pointerLockElement === element) document.exitPointerLock()
    }
  }, [])

  useEffect(() => {
    if ((!firstPerson || paused) && document.pointerLockElement === container.current && container.current !== null)
      document.exitPointerLock()
  }, [firstPerson, paused])

  useEffect(() => {
    if (!locked) return

    const steer = () => {
      const heading = look.current.heading
      if (heading === null) return
      const { facing: current, onTurn: turn } = live.current
      const wanted = steeredFacing(heading, current)
      if (wanted === current) return
      const now = performance.now()
      if (steering.current.asked === wanted && now - steering.current.at < steerRetryMs) return
      steering.current = { asked: wanted, at: now }
      turn(wanted)
    }

    const handleMouseMove = (event: MouseEvent) => {
      const heading = look.current.heading
      if (heading === null) return
      look.current = {
        yaw: 0,
        pitch: Math.max(-maxPitch, Math.min(maxPitch, look.current.pitch - event.movementY * mousePitchPerPixel)),
        heading: wrapAngle(heading - event.movementX * mouseYawPerPixel),
      }
      steer()
    }

    document.addEventListener('mousemove', handleMouseMove)
    const retry = window.setInterval(steer, steerRetryMs)
    return () => {
      document.removeEventListener('mousemove', handleMouseMove)
      window.clearInterval(retry)
    }
  }, [locked])

  function requestLook() {
    const element = container.current
    if (!element) return
    try {
      const request = element.requestPointerLock() as unknown
      if (request instanceof Promise) request.catch(() => undefined)
    } catch {
      return
    }
  }

  useEffect(
    () => () => {
      if (settle.current !== null) window.clearTimeout(settle.current)
    },
    [],
  )

  function setZoom(next: number) {
    const value = clampZoom(next)
    saveZoom(value)
    setZoomState(value)
  }

  function spread(): number {
    const [first, second] = [...touches.current.values()]
    return Math.hypot(first.x - second.x, first.y - second.y)
  }

  function releaseLook() {
    const quarters = Math.round(-look.current.yaw / (Math.PI / 2))
    if (settle.current !== null) window.clearTimeout(settle.current)

    if (quarters === 0) {
      look.current = { yaw: 0, pitch: 0, heading: null }
      return
    }

    look.current = { yaw: look.current.yaw, pitch: 0, heading: null }
    onTurn(turned(facing, quarters))
    settle.current = window.setTimeout(() => {
      settle.current = null
      look.current = { yaw: 0, pitch: 0, heading: null }
    }, 900)
  }

  function handlePointerDown(event: PointerEvent<HTMLDivElement>) {
    const mouse = event.pointerType === 'mouse'
    if (mouse && event.button !== 0) return

    if (mouse && locked) {
      onAttack()
      return
    }

    if (mouse && firstPerson && canLock) {
      requestLook()
      return
    }

    if (!mouse) {
      touches.current.set(event.pointerId, { x: event.clientX, y: event.clientY })
      if (touches.current.size === 2) {
        pinch.current = { distance: spread(), zoom }
        drag.current = null
        look.current = { yaw: 0, pitch: 0, heading: null }
        return
      }
    }

    if (firstPerson) {
      drag.current = { pointerId: event.pointerId, x: event.clientX, y: event.clientY, moved: false, mouse }
      event.currentTarget.setPointerCapture?.(event.pointerId)
    } else if (mouse) onAttack()
  }

  function handlePointerMove(event: PointerEvent<HTMLDivElement>) {
    const current = drag.current
    if (current && current.pointerId === event.pointerId) {
      const dx = event.clientX - current.x
      const dy = event.clientY - current.y
      if (!current.moved && Math.hypot(dx, dy) > dragThreshold) current.moved = true
      if (current.moved) look.current = { yaw: -dx * yawPerPixel, pitch: Math.max(-maxPitch, Math.min(maxPitch, -dy * pitchPerPixel)), heading: null }
    }

    if (!touches.current.has(event.pointerId)) return
    touches.current.set(event.pointerId, { x: event.clientX, y: event.clientY })
    if (!pinch.current || touches.current.size !== 2) return

    const ratio = pinch.current.distance / Math.max(spread(), 1)
    if (firstPerson) {
      if (ratio > 1.25) {
        pinch.current = null
        onFirstPersonChange(false)
      }
      return
    }

    const wanted = pinch.current.zoom * ratio
    if (wanted < minZoom * 0.8) {
      pinch.current = null
      onFirstPersonChange(true)
      return
    }
    setZoom(wanted)
  }

  function handlePointerUp(event: PointerEvent<HTMLDivElement>) {
    const current = drag.current
    if (current && current.pointerId === event.pointerId) {
      drag.current = null
      if (current.moved) releaseLook()
      else if (current.mouse) onAttack()
    }

    touches.current.delete(event.pointerId)
    if (touches.current.size < 2) pinch.current = null
  }

  useEffect(() => {
    const element = container.current
    if (!element) return

    const handleWheel = (event: globalThis.WheelEvent) => {
      event.preventDefault()
      const { firstPerson: inside, zoom: current, onFirstPersonChange: change } = modes.current
      const zoomingIn = event.deltaY < 0

      if (inside) {
        if (!zoomingIn) change(false)
        return
      }

      if (zoomingIn && current <= minZoom + 0.001) {
        change(true)
        return
      }

      const value = clampZoom(current * (zoomingIn ? 1 / 1.08 : 1.08))
      saveZoom(value)
      setZoomState(value)
    }

    element.addEventListener('wheel', handleWheel, { passive: false })
    return () => element.removeEventListener('wheel', handleWheel)
  }, [])

  return (
    <div
      ref={container}
      className="scene3d"
      style={{ '--columns': viewColumns, '--rows': viewRows } as CSSProperties}
      onPointerDown={handlePointerDown}
      onPointerMove={handlePointerMove}
      onPointerUp={handlePointerUp}
      onPointerCancel={handlePointerUp}
    >
      <Canvas shadows={lowPower ? false : { type: PCFShadowMap }} dpr={lowPower ? [1, 1.5] : [1, 2]} camera={{ fov: 42, near: 0.5, far: 260, position: [0, 30, 30] }}>
        <color attach="background" args={[voidColor]} />
        <fog attach="fog" args={firstPerson ? [voidColor, 20, 56] : [voidColor, 34 * zoom, 90 * zoom]} />
        <Suspense fallback={null}>
          <World game={game} effects={effects} turn={turn} zoom={zoom} firstPerson={firstPerson} lookOffset={look} />
        </Suspense>
      </Canvas>
      <div className="scene3d-vignette" />
      {game.grave && (
        <Compass
          from={game.player}
          to={game.grave}
          viewColumns={viewColumns}
          viewRows={viewRows}
          sheet={sheets.dungeon}
          icon={graveSprite}
          label="Your grave"
          variant="grave"
          facing={firstPerson ? game.player.facing : undefined}
          always
        />
      )}
      {bossTarget && (
        <Compass
          from={game.player}
          to={bossTarget}
          viewColumns={viewColumns}
          viewRows={viewRows}
          sheet={sheets.items}
          icon={crownSprite}
          label={bossTarget.name}
          variant="boss"
          facing={firstPerson ? game.player.facing : undefined}
          always
        />
      )}
      {firstPerson && <div className="scene3d-crosshair" aria-hidden="true" />}
      {firstPerson && canLock && !locked && !paused && <div className="scene3d-look-hint">Click to look around with the mouse</div>}
      <div className="scene3d-zoom" hidden={firstPerson} onPointerDown={event => event.stopPropagation()}>
        <button type="button" aria-label="Zoom in" disabled={zoom <= minZoom} onClick={() => setZoom(zoom / zoomStep)}>
          +
        </button>
        <button type="button" aria-label="Zoom out" disabled={zoom >= maxZoom} onClick={() => setZoom(zoom * zoomStep)}>
          −
        </button>
      </div>
      <LoadingVeil />
    </div>
  )
}

function LoadingVeil() {
  const { active, progress } = useProgress()
  if (!active) return null
  return <div className="scene3d-loading">Loading the vault… {Math.round(progress)}%</div>
}

interface WorldProps {
  game: GameState
  effects: Effect[]
  turn: Turn | null
  zoom: number
  firstPerson: boolean
  lookOffset: RefObject<LookOffset>
}

function World({ game, effects, turn, zoom, firstPerson, lookOffset }: WorldProps) {
  const kit = useKit(modelUrls.dungeon)
  const look = biomeLook(game.biome)
  const player = game.player
  const mainHand = player.equipment.find(equipped => equipped.slot === 'MainHand')?.item
  const offHand = player.equipment.find(equipped => equipped.slot === 'OffHand')?.item
  const gear = useMemo(() => heroGear(player.equipment), [player.equipment])
  const cues = useMemo(
    () => cuesForTurn(turn, game.enemies, player, mainHand, offHand),
    [turn, game.enemies, player, mainHand, offHand],
  )
  const [corpses, setCorpses] = useState<Corpse[]>([])
  const [auras, setAuras] = useState<Aura[]>([])

  useEffect(() => {
    const fresh = cues.bursts.filter(burst => burst.kind === 'levelup')
    if (fresh.length === 0) return
    const add = window.setTimeout(() => {
      const now = performance.now()
      setAuras(current => [
        ...current.filter(aura => !fresh.some(burst => burst.key === aura.burst.key)),
        ...fresh.map(burst => ({ burst, until: now + burst.delayMs + levelUpSeconds * 1000 + 200 })),
      ])
    }, 0)
    return () => window.clearTimeout(add)
  }, [cues.bursts])

  useEffect(() => {
    if (auras.length === 0) return
    const next = Math.min(...auras.map(aura => aura.until))
    const prune = window.setTimeout(() => {
      const now = performance.now()
      setAuras(current => current.filter(aura => aura.until > now))
    }, Math.max(0, next - performance.now()))
    return () => window.clearTimeout(prune)
  }, [auras])

  useEffect(() => {
    if (cues.dying.length === 0) return
    const fresh = cues.dying
    const add = window.setTimeout(() => {
      const now = performance.now()
      setCorpses(current => [
        ...current.filter(corpse => corpse.until > now && !fresh.some(enemy => enemy.id === corpse.enemy.id)),
        ...fresh.map(enemy => ({ enemy, until: now + corpseMs })),
      ])
    }, 0)
    const prune = window.setTimeout(() => {
      const now = performance.now()
      setCorpses(current => current.filter(corpse => corpse.until > now))
    }, corpseMs + 100)
    return () => {
      window.clearTimeout(add)
      window.clearTimeout(prune)
    }
  }, [cues.dying])

  const enemies = useMemo(() => {
    const alive = new Set(game.enemies.map(enemy => enemy.id))
    const fallen = [...cues.dying, ...corpses.map(corpse => corpse.enemy)].filter(
      (enemy, index, list) => !alive.has(enemy.id) && list.findIndex(other => other.id === enemy.id) === index,
    )
    return [...game.enemies, ...fallen]
  }, [game.enemies, cues.dying, corpses])
  useEffect(() => {
    new Set(game.enemies.map(enemy => monsterLook(enemy.kind).url).filter(url => url !== '')).forEach(url => useGLTF.preload(url))
  }, [game.enemies])
  const blocked = useMemo(
    () =>
      new Set(
        [game.stairs, game.merchant, game.waystone, game.totem, game.grave, ...game.chests, ...game.floorItems]
          .filter((spot): spot is TilePosition => spot !== undefined && spot !== null)
          .map(spot => `${spot.x},${spot.y}`),
      ),
    [game.stairs, game.merchant, game.waystone, game.totem, game.grave, game.chests, game.floorItems],
  )
  const visible = useMemo(() => visibleTiles(game.map, player.x, player.y), [game.map, player.x, player.y])
  const explored = useMemo(() => remember(layoutKey(game.id, game.floor, game.map), game.map, visible), [game.id, game.floor, game.map, visible])
  const columns = useMemo(() => Math.max(...game.map.map(row => row.length)), [game.map])
  const lit = useMemo(() => {
    const rows = game.map.length
    const grid = new Uint8Array(columns * rows)
    game.map.forEach((row, y) => {
      for (let x = 0; x < row.length; x++) if (row[x] !== '#' && explored.has(`${x},${y}`)) grid[y * columns + x] = 1
    })
    return (x: number, y: number) => x >= 0 && y >= 0 && x < columns && y < rows && grid[y * columns + x] === 1
  }, [game.map, explored, columns])

  return (
    <>
      <hemisphereLight args={[look.ambient, '#000000', 1]} />
      <CameraRig x={player.x} y={player.y} zoom={zoom} light={look.light} facing={player.facing} firstPerson={firstPerson} alive={player.health > 0} look={lookOffset} />
      {firstPerson && (
        <FirstPersonHands
          mainHand={gear.mainHandModel}
          offHand={gear.offHandModel}
          twoHanded={(mainHand?.twoHanded ?? false) && !mainHand?.isTwin}
          cue={cues.hero}
          alive={player.health > 0}
          sleeve={gear.outfit.body ? '#7d828c' : '#6b4f3a'}
        />
      )}
      <DungeonMap map={game.map} focusY={player.y} cutaway={!firstPerson} biome={game.biome} kit={kit} hole={game.stairs} blocked={blocked} />
      <Props game={game} kit={kit} visible={visible} />
      <Decor map={game.map} biome={game.biome} blocked={blocked} />
      <GridLines columns={columns} rows={game.map.length} lit={lit} />
      <TargetMarks x={player.x} y={player.y} facing={player.facing} enemies={game.enemies} visible={visible} />
      <Weather biome={game.biome} x={player.x} y={player.y} lit={lit} />
      <Shroud map={game.map} explored={explored} color={voidColor} focusY={firstPerson ? Infinity : player.y} />
      <Actor
        url={heroUrl(player.classId)}
        x={player.x}
        y={player.y}
        rotation={facingAngles[player.facing]}
        idle={heroIdleClip(mainHand)}
        cue={cues.hero}
        scale={heroScale}
        alive={player.health > 0}
        hidden={firstPerson}
        mainHand={gear.mainHandModel}
        offHand={gear.offHandModel}
        ghostly={player.ability?.kind === 'Invisibility' && player.ability.activeTurns > 0}
        outfit={gear.outfit}
      >
        <HeroRing />
      </Actor>
      {enemies.map(enemy => (
        <Suspense key={enemy.id} fallback={null}>
          <Monster
            enemy={enemy}
            game={game}
            cue={cues.enemies.get(enemy.id)}
            dying={!game.enemies.some(living => living.id === enemy.id)}
            seen={visible(enemy.x, enemy.y)}
            firstPerson={firstPerson}
          />
        </Suspense>
      ))}
      {game.merchant && (
        <Suspense fallback={null}>
          <Actor
            url={merchantUrl}
            x={game.merchant.x}
            y={game.merchant.y}
            rotation={0}
            idle="Idle"
            tint="#e8c87a"
            scale={heroScale}
            hidden={!visible(game.merchant.x, game.merchant.y)}
          />
        </Suspense>
      )}
      <Bursts
        bursts={[
          ...effects.filter(
            effect => !['coins', 'arrow', 'power', 'levelup'].includes(effect.kind) && !cues.trail.has(`${effect.x},${effect.y}`) && visible(effect.x, effect.y),
          ),
          ...cues.bursts.filter(burst => burst.kind !== 'levelup' && visible(burst.x, burst.y)),
        ]}
      />
      {auras.map(aura => (
        <LevelUpAura key={aura.burst.key} burst={aura.burst} x={player.x} y={player.y} />
      ))}
      <Projectiles projectiles={cues.projectiles} />
      <Arcs arcs={cues.arcs} />
      <Popups popups={cues.popups.filter(popup => visible(popup.x, popup.y))} />
    </>
  )
}

function HeroRing() {
  return (
    <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.08, 0]}>
      <ringGeometry args={[1.25, 1.45, 32]} />
      <meshBasicMaterial color="#ffd36a" transparent opacity={0.65} />
    </mesh>
  )
}

interface CameraRigProps {
  x: number
  y: number
  zoom: number
  light: string
  facing: Direction
  firstPerson: boolean
  alive: boolean
  look: RefObject<LookOffset>
}

const cameraOffset = new Vector3(0, 9.5, 12.5)
const lookAhead = new Vector3(0, 1.2, 0)

const eyeHeight = 2.7
const maxFrameDelta = 1 / 20
const eyeBack = 1.5
const fallenEye = 0.7

function CameraRig({ x, y, zoom, light, facing, firstPerson, alive, look }: CameraRigProps) {
  const { camera } = useThree()
  const sun = useRef<DirectionalLight>(null)
  const focus = useRef(new Vector3(x * cell, 0, y * cell))
  const placed = useRef(false)
  const shown = useRef({ yaw: 0, pitch: 0 })
  const lastGoal = useRef(facingAngles[facing])
  const eye = useRef(eyeHeight)
  const body = useRef(facingAngles[facing])


  useFrame(state => {
    const view = state.camera
    const fov = firstPerson ? 82 : 42
    if (!(view instanceof PerspectiveCamera) || view.fov === fov) return
    view.fov = fov
    view.near = firstPerson ? 0.05 : 0.5
    view.updateProjectionMatrix()
  })

  useFrame((_, frameDelta) => {
    const delta = Math.min(frameDelta, maxFrameDelta)
    const target = new Vector3(x * cell, 0, y * cell)
    if (!placed.current || focus.current.distanceTo(target) > cell * 9) {
      focus.current.copy(target)
      placed.current = true
    } else focus.current.lerp(target, Math.min(1, delta * 7))

    if (firstPerson) {
      const goal = facingAngles[facing]
      const view = shown.current
      if (goal !== lastGoal.current) {
        view.yaw -= Math.atan2(Math.sin(goal - lastGoal.current), Math.cos(goal - lastGoal.current))
        lastGoal.current = goal
      }
      const ease = Math.min(1, delta * 9)
      const aim = look.current.heading
      if (aim === null) {
        view.yaw += (look.current.yaw - view.yaw) * ease
        view.pitch += (look.current.pitch - view.pitch) * ease
      } else {
        const follow = Math.min(1, delta * 24)
        view.yaw += wrapAngle(aim - goal - view.yaw) * follow
        view.pitch += (look.current.pitch - view.pitch) * follow
      }
      eye.current += ((alive ? eyeHeight : fallenEye) - eye.current) * Math.min(1, delta * 3)
      body.current += wrapAngle(goal - body.current) * ease
      const bodyYaw = aim === null ? goal + view.yaw - look.current.yaw : body.current
      const back = new Vector3(Math.sin(bodyYaw), 0, Math.cos(bodyYaw))
      const heading = goal + view.yaw
      const tilt = (alive ? -0.11 : 0.2) + view.pitch
      camera.position.copy(focus.current).addScaledVector(back, -eyeBack).setY(eye.current)
      camera.lookAt(
        camera.position.clone().add(new Vector3(Math.sin(heading) * Math.cos(tilt), Math.sin(tilt), Math.cos(heading) * Math.cos(tilt))),
      )
    } else {
      camera.position.copy(focus.current).addScaledVector(cameraOffset, zoom)
      camera.lookAt(focus.current.clone().add(lookAhead))
    }

    if (sun.current) {
      sun.current.position.copy(focus.current).add(new Vector3(-8, 22, 10))
      sun.current.target.position.copy(focus.current)
      sun.current.target.updateMatrixWorld()
    }
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
      <HeroLight x={x} y={y} color={light} />
    </>
  )
}

function HeroLight({ x, y, color }: { x: number; y: number; color: string }) {
  const ref = useRef<Group>(null)

  useFrame(({ clock }, delta) => {
    const group = ref.current
    if (!group) return
    group.position.lerp(new Vector3(x * cell, 0, y * cell), Math.min(1, delta * 8))
    const light = group.children[0] as unknown as { intensity: number }
    light.intensity = 70 + Math.sin(clock.elapsedTime * 7) * 6
  })

  return (
    <group ref={ref}>
      <pointLight position={[0, 5, 1]} color={color} intensity={70} distance={34} decay={1.4} />
    </group>
  )
}

interface MonsterProps {
  enemy: EnemyState
  game: GameState
  cue: Cue | undefined
  dying: boolean
  seen: boolean
  firstPerson: boolean
}

function Monster({ enemy, game, cue, dying, seen, firstPerson }: MonsterProps) {
  const look = monsterLook(enemy.kind)
  const scale = look.scale
  const dx = game.player.x - enemy.x
  const dy = game.player.y - enemy.y
  const rotation = enemy.isAwake ? Math.atan2(dx, dy) : 0
  const idle = look.clips[enemy.isAwake ? 'Idle' : 'Sleep']
  const height = look.height
  const size = height / 2.7
  const modelCue = useMemo(() => (cue ? { ...cue, clip: look.clips[cue.clip as MonsterClip] ?? cue.clip } : undefined), [cue, look.clips])
  const showLabel = seen && !dying
  const nearCamera = !firstPerson && -dy >= 1 && -dy <= 2 && Math.abs(dx) <= 1

  return (
    <Actor
      url={look.url}
      build={look.build}
      x={enemy.x}
      y={enemy.y}
      rotation={rotation}
      idle={idle}
      walk={look.clips.Walk}
      cue={modelCue}
      tint={look.tint}
      scale={scale}
      mainHand={look.weapon}
      offHand={look.offHand}
      ghostly={enemy.kind === 'ghost'}
      hidden={!seen}
      sinking={dying}
      procedural={look.procedural}
    >
      {enemy.isBoss && !selfCrowned.has(enemy.kind) && !dying && <BossCrown height={height} size={size} />}
      {enemy.isBoss ? <BossAura scale={size} /> : enemy.rarity !== 'Common' && <RarityRing color={rarityColors[enemy.rarity] ?? '#ffffff'} size={size} />}
      {enemy.isFrozen && (
        <mesh position={[0, height / 2, 0]}>
          <boxGeometry args={[1.8 * size, height, 1.8 * size]} />
          <meshStandardMaterial color="#bfe8ff" transparent opacity={0.35} roughness={0.05} />
        </mesh>
      )}
      {showLabel && (
        <Html position={[0, nearCamera ? -0.4 : height + (enemy.isBoss ? 1.5 : 0.5), 0]} center zIndexRange={[20, 0]} className="scene3d-label-anchor">
          <div className={`scene3d-label rarity-${enemy.rarity.toLowerCase()}${enemy.isBoss ? ' boss' : ''}`}>
            <span className="scene3d-name">
              {enemy.isBoss && <Sprite sheet={sheets.items} index={crownSprite} className="scene3d-mark" />}
              {!enemy.isBoss &&
                Array.from({ length: rarityStars[enemy.rarity] }, (_, index) => (
                  <Sprite key={index} sheet={sheets.items} index={starSprites[enemy.rarity]} className="scene3d-mark" />
                ))}
              {enemy.name} <small>Lv{enemy.level}</small>
              {enemy.isBurning && <ElementIcon element="Fire" className="scene3d-mark" />}
              {enemy.isFrozen && <ElementIcon element="Ice" className="scene3d-mark" />}
              {enemy.venomStacks > 0 && <span className="scene3d-venom">☠{enemy.venomStacks}</span>}
            </span>
            <span className={`scene3d-health${enemy.weakness ? ` weak-${enemy.weakness.toLowerCase()}` : ''}`}>
              <span style={{ width: `${Math.max(0, (enemy.health / enemy.maxHealth) * 100)}%` }} />
            </span>
          </div>
        </Html>
      )}
    </Actor>
  )
}

function BossCrown({ height, size }: { height: number; size: number }) {
  const ref = useRef<Group>(null)

  useFrame(({ clock }) => {
    const crown = ref.current
    if (!crown) return
    crown.rotation.y = clock.elapsedTime * 0.9
    crown.position.y = height + 0.45 + Math.sin(clock.elapsedTime * 2.2) * 0.12
  })

  const radius = 0.42 * Math.max(1, size * 0.8)
  const spikes = 6

  return (
    <group ref={ref} position={[0, height + 0.45, 0]}>
      <mesh>
        <cylinderGeometry args={[radius, radius * 1.08, radius * 0.45, 16, 1, true]} />
        <meshStandardMaterial color="#f2c23a" emissive="#c88a10" emissiveIntensity={0.6} metalness={0.7} roughness={0.3} side={DoubleSide} />
      </mesh>
      {Array.from({ length: spikes }, (_, index) => {
        const angle = (index / spikes) * Math.PI * 2
        return (
          <group key={index} position={[Math.cos(angle) * radius, radius * 0.38, Math.sin(angle) * radius]}>
            <mesh>
              <coneGeometry args={[radius * 0.16, radius * 0.5, 4]} />
              <meshStandardMaterial color="#f2c23a" emissive="#c88a10" emissiveIntensity={0.6} metalness={0.7} roughness={0.3} />
            </mesh>
            <mesh position={[0, -radius * 0.32, 0]}>
              <octahedronGeometry args={[radius * 0.09]} />
              <meshStandardMaterial color={index % 2 === 0 ? '#ff2a4a' : '#3a8aff'} emissive={index % 2 === 0 ? '#ff2a4a' : '#3a8aff'} emissiveIntensity={0.8} />
            </mesh>
          </group>
        )
      })}
    </group>
  )
}

function BossAura({ scale }: { scale: number }) {
  const ref = useRef<Mesh>(null)

  useFrame(({ clock }) => {
    if (ref.current) {
      ref.current.rotation.z = clock.elapsedTime * 0.8
      ;(ref.current.material as MeshStandardMaterial).opacity = 0.45 + Math.sin(clock.elapsedTime * 3) * 0.15
    }
  })

  return (
    <mesh ref={ref} rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.1, 0]}>
      <ringGeometry args={[1.3 * scale, 1.6 * scale, 6]} />
      <meshStandardMaterial color="#ff3a3a" emissive="#ff2020" emissiveIntensity={1.2} transparent opacity={0.5} />
    </mesh>
  )
}

interface PropsProps {
  game: GameState
  kit: Record<string, KitPiece>
  visible: Visibility
}

function Piece({ piece, x, y, rotation = 0, scale = 1, lift = 0 }: { piece: KitPiece | undefined; x: number; y: number; rotation?: number; scale?: number; lift?: number }) {
  if (!piece) return null
  return (
    <mesh
      geometry={piece.geometry}
      material={piece.material}
      position={[x * cell, lift, y * cell]}
      rotation={[0, rotation, 0]}
      scale={scale}
      castShadow
      receiveShadow
    />
  )
}

function Props({ game, kit, visible }: PropsProps) {
  return (
    <group>
      {game.stairs && (
        <group>
          <Built build={buildStairs} x={game.stairs.x} y={game.stairs.y} />
          {!game.stairs.isOpen && <Piece piece={kit['stairs-sealed']} x={game.stairs.x} y={game.stairs.y} lift={0.04} />}
          {visible(game.stairs.x, game.stairs.y) && (
            <Glow x={game.stairs.x} y={game.stairs.y} color={game.stairs.isOpen ? '#7affb0' : '#ff6a4a'} height={game.stairs.isOpen ? -1.2 : 0.4} />
          )}
        </group>
      )}
      {game.chests.map(chest => (
        <Piece key={`chest-${chest.x}-${chest.y}`} piece={kit.chest} x={chest.x} y={chest.y} scale={1.25} />
      ))}
      {game.grave && (
        <>
          <Piece piece={kit.grave} x={game.grave.x} y={game.grave.y} scale={0.9} />
          <group position={[game.grave.x * cell, 0, game.grave.y * cell]}>
            <RarityRing color="#ff2a2a" size={1.35} />
            <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.07, 0]}>
              <circleGeometry args={[1.4, 24]} />
              <meshBasicMaterial color="#ff2a2a" transparent opacity={0.22} depthWrite={false} />
            </mesh>
            <LootBeam color="#ff3a3a" pulse />
          </group>
        </>
      )}
      {game.waystone && (
        <group>
          <Piece piece={kit.waystone} x={game.waystone.x} y={game.waystone.y} scale={0.7} />
          <FloatingCrystal x={game.waystone.x} y={game.waystone.y} color="#5ae0ff" height={3.5} />
        </group>
      )}
      {game.totem && (
        <group>
          <Built build={buildTotem} x={game.totem.x} y={game.totem.y} />
          <FloatingCrystal x={game.totem.x} y={game.totem.y} color="#ff4a6a" height={4} />
        </group>
      )}
      {game.floorItems.filter(floorItem => visible(floorItem.x, floorItem.y)).map(floorItem => (
        <FloorItem key={`item-${floorItem.x}-${floorItem.y}`} x={floorItem.x} y={floorItem.y} item={floorItem.item} />
      ))}
    </group>
  )
}

function Built({ build, x, y }: { build: () => Object3D; x: number; y: number }) {
  const object = useMemo(() => build(), [build])
  return <primitive object={object} position={[x * cell, 0, y * cell]} />
}

function FloatingCrystal({ x, y, color, height }: { x: number; y: number; color: string; height: number }) {
  const ref = useRef<Mesh>(null)

  useFrame(({ clock }) => {
    const mesh = ref.current
    if (!mesh) return
    mesh.rotation.y = clock.elapsedTime * 1.4
    mesh.position.y = height + Math.sin(clock.elapsedTime * 2) * 0.2
  })

  return (
    <mesh ref={ref} position={[x * cell, height, y * cell]} scale={[0.35, 0.6, 0.35]}>
      <octahedronGeometry args={[1, 0]} />
      <meshStandardMaterial color={color} emissive={color} emissiveIntensity={1.4} flatShading />
    </mesh>
  )
}

function Glow({ x, y, color, height }: { x: number; y: number; color: string; height: number }) {
  const ref = useRef<Mesh>(null)

  useFrame(({ clock }) => {
    if (ref.current) ref.current.scale.setScalar(1 + Math.sin(clock.elapsedTime * 2.5) * 0.12)
  })

  return (
    <mesh ref={ref} position={[x * cell, height, y * cell]}>
      <sphereGeometry args={[0.45, 16, 16]} />
      <meshBasicMaterial color={color} transparent opacity={0.85} />
    </mesh>
  )
}

const beamRarities = new Set(['Epic', 'Legendary', 'Exotic', 'Platinum'])

function LootBeam({ color, pulse }: { color: string; pulse: boolean }) {
  const ref = useRef<Mesh>(null)

  useFrame(({ clock }) => {
    const mesh = ref.current
    if (!mesh) return
    const wave = pulse ? 0.5 + Math.sin(clock.elapsedTime * 5) * 0.5 : 0.5 + Math.sin(clock.elapsedTime * 2) * 0.15
    ;(mesh.material as MeshBasicMaterial).opacity = 0.18 + wave * 0.22
    mesh.scale.set(1 + wave * 0.25, 1, 1 + wave * 0.25)
  })

  return (
    <mesh ref={ref} position={[0, 3.2, 0]}>
      <cylinderGeometry args={[0.35, 0.6, 6.4, 12, 1, true]} />
      <meshBasicMaterial color={color} transparent opacity={0.3} depthWrite={false} side={DoubleSide} />
    </mesh>
  )
}

function RarityRing({ color, size }: { color: string; size: number }) {
  const ref = useRef<Mesh>(null)

  useFrame(({ clock }) => {
    if (ref.current) ref.current.rotation.z = clock.elapsedTime * 0.6
  })

  return (
    <mesh ref={ref} rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.09, 0]}>
      <ringGeometry args={[1.05 * size, 1.3 * size, 24]} />
      <meshBasicMaterial color={color} transparent opacity={0.75} />
    </mesh>
  )
}

function FloorItem({ x, y, item }: { x: number; y: number; item: ItemInfo }) {
  const ref = useRef<Group>(null)
  const weapons = useKitObjects(modelUrls.weapons)
  const color = rarityColors[item.rarity] ?? '#ffffff'
  const model = useMemo(() => {
    const weapon = weaponModel(item)
    const source = weapon ? weapons[weapon] : undefined
    if (source) {
      const copy = source.clone(true)
      copy.rotation.z = Math.PI / 2.4
      copy.scale.setScalar(0.85)
      return copy
    }
    if (item.category === 'Bow') return buildBow()
    if (item.category === 'Spear') return buildSpear()
    return buildItem(item.category, color)
  }, [item, weapons, color])

  useFrame(({ clock }) => {
    if (!ref.current) return
    ref.current.rotation.y = clock.elapsedTime * 1.2
    ref.current.position.y = 0.5 + Math.sin(clock.elapsedTime * 2.4 + x) * 0.12
  })

  return (
    <group position={[x * cell, 0, y * cell]}>
      <group ref={ref}>
        <primitive object={model} />
      </group>
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0.07, 0]}>
        <circleGeometry args={[0.9, 24]} />
        <meshBasicMaterial color={color} transparent opacity={item.rarity === 'Common' ? 0.18 : 0.4} />
      </mesh>
      {beamRarities.has(item.rarity) && <LootBeam color={color} pulse={item.rarity === 'Exotic' || item.rarity === 'Platinum'} />}
    </group>
  )
}

function Popups({ popups }: { popups: Popup[] }) {
  return (
    <>
      {popups.map(popup => (
        <Html key={popup.key} position={[popup.x * cell, 3.4, popup.y * cell]} center zIndexRange={[30, 0]}>
          <span className={`scene3d-popup tone-${popup.tone}`} style={{ animationDelay: `${popup.delayMs}ms` }}>
            {popup.text}
          </span>
        </Html>
      ))}
    </>
  )
}

useGLTF.preload(modelUrls.dungeon)
useGLTF.preload(modelUrls.animations)
useGLTF.preload(modelUrls.weapons)
