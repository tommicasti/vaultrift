import type { BurstKind } from './liveEffects'
import { Html } from '@react-three/drei'
import { Canvas, useFrame, useThree } from '@react-three/fiber'
import { Suspense, useCallback, useEffect, useMemo, useRef, useState, type RefObject } from 'react'
import { DirectionalLight, Group, Mesh, MeshBasicMaterial, PCFShadowMap, PerspectiveCamera, Vector3 } from 'three'
import type { EquippedItem, ItemInfo, MerchantState, TotemState, WaystoneState } from '../api/types'
import { Actor, type Cue, type Tracked } from '../scene3d/Actor'
import { Decor } from '../scene3d/Decor'
import { DungeonMap } from '../scene3d/DungeonMap'
import { FirstPersonHands } from '../scene3d/FirstPersonHands'
import { heroGear, type HeroGear } from '../scene3d/heroLook'
import { BossAura, BossCrown, Built, FloatingCrystal, FloorItem, Glow, LootBeam, Piece, RarityRing } from '../scene3d/Scene3D'
import { buildStairs, buildTotem } from '../scene3d/creatures'
import { useKit } from '../scene3d/kit'
import { biomeLook, cell, heroIdleClip, heroUrl, merchantUrl, modelUrls, monsterLook, rarityColors, selfCrowned } from '../scene3d/models'
import { canMouseLook, canTouchLook, detectLowPower, loadZoom, saveZoom, type CameraMode } from '../scene3d/viewMode'
import { renderDelaySeconds, type FrameBuffer } from './interpolation'
import { Bursts, LevelUpAura } from '../scene3d/Effects3D'
import { GridLines } from '../scene3d/Grid'
import { Weather } from '../scene3d/Weather'
import { ElementIcon } from '../components/ElementIcon'
import { Sprite } from '../components/Sprite'
import { crownSprite, rarityStars, sheets, starSprites } from '../game/sprites'
import { Shroud } from '../scene3d/Shroud'
import type { Vision } from './topDownLayout'
import type { MoveInput } from './input'
import { CompassTracker, PartyMarkers } from './PartyCompass'
import { bossMarkerId } from './compass'
import type { Predictor } from './prediction'
import type { Element } from '../api/types'
import type { LiveChest, LiveGrave, LiveHero, LiveItem, LiveMonster, LiveProjectile, LiveStairs, LiveWelcome } from './types'
import './live.css'

export interface Popup {
  key: number
  x: number
  y: number
  text: string
  kind: 'damage' | 'critical' | 'miss' | 'hurt' | 'reward' | 'level' | 'burn' | 'frozen'
  until: number
  levelUpHero?: number
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

export type SparkKind = BurstKind

export interface Spark {
  key: number
  x: number
  y: number
  kind: SparkKind
  start: number
}

export interface Lunge {
  at: number
  x: number
  y: number
}

export type MovingProjectile = LiveProjectile & { moving: boolean }

export interface Corpse {
  monster: LiveMonster
  until: number
}

export interface Look {
  yaw: number
  pitch: number
}

export interface Prediction {
  predictor: Predictor
  move: RefObject<MoveInput>
  roundTrip: RefObject<number>
}

interface LiveSceneProps {
  hidden?: boolean
  welcome: LiveWelcome
  map: string[]
  merchant: MerchantState | null
  waystone: WaystoneState | null
  totem: TotemState | null
  graves: LiveGrave[]
  equipment: EquippedItem[]
  party: ReadonlyMap<number, EquippedItem[]>
  view: CameraMode
  look: { current: Look }
  onViewChange: (view: CameraMode) => void
  items: LiveItem[]
  chests: LiveChest[]
  stairs: LiveStairs | null
  projectiles: MovingProjectile[]
  projectileBuffer: FrameBuffer<MovingProjectile>
  blasts: Blast[]
  sparks: Spark[]
  heroes: LiveHero[]
  monsters: LiveMonster[]
  metBosses: ReadonlySet<number>
  vision: Vision
  corpses: Corpse[]
  heroBuffer: FrameBuffer<LiveHero>
  prediction: Prediction
  monsterBuffer: FrameBuffer<LiveMonster>
  heroCues: Map<number, Cue>
  monsterCues: Map<number, LiveCue>
  popups: Popup[]
}

const heroScale = 1.3
const heroRunPace = 7
const voidColor = '#000000'
const cameraOffset = new Vector3(0, 9.5, 12.5)
const overheadOffset = new Vector3(0, 14.6, 3.6)
const lookAhead = new Vector3(0, 1.2, 0)
const noBlocks: ReadonlySet<string> = new Set()

const minZoom = 0.6
const maxZoom = 2.6
const zoomStep = 1.15
const lowPower = detectLowPower()
const mouseLook = canMouseLook()
const touchLook = canTouchLook()
const eyeHeight = 2.7
const fallenEye = 0.7
const maxPitch = 1.1

function clampZoom(zoom: number): number {
  return Math.min(maxZoom, Math.max(minZoom, zoom))
}

export function LiveScene(props: LiveSceneProps) {
  const container = useRef<HTMLDivElement>(null)
  const markers = useRef(new Map<number, HTMLDivElement>())
  const others = props.heroes.filter(hero => hero.id !== props.welcome.heroId)
  const bosses = props.monsters.filter(monster => monster.isBoss && props.metBosses.has(monster.id))
  const [zoom, setZoomState] = useState(() => clampZoom(loadZoom(1)))
  const { view, onViewChange } = props
  const modes = useRef({ view, onViewChange, zoom })

  useEffect(() => {
    modes.current = { view, onViewChange, zoom }
  }, [view, onViewChange, zoom])

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
      const zoomingIn = event.deltaY < 0
      const { view: current, onViewChange: change, zoom } = modes.current
      if (current === 'first') {
        if (!zoomingIn) change('third')
        return
      }
      if (zoomingIn && zoom <= minZoom + 0.001 && mouseLook) {
        change('first')
        return
      }
      const value = clampZoom(zoom * (zoomingIn ? 1 / 1.08 : 1.08))
      saveZoom(value)
      setZoomState(value)
    }

    const touches = new Map<number, { x: number; y: number }>()
    let pinch: { distance: number; zoom: number } | null = null
    const spread = () => {
      const [first, second] = [...touches.values()]
      return Math.hypot(first.x - second.x, first.y - second.y)
    }
    const handleDown = (event: PointerEvent) => {
      if (event.pointerType !== 'touch' || event.target !== element.querySelector('canvas')) return
      touches.set(event.pointerId, { x: event.clientX, y: event.clientY })
      if (touches.size === 2) pinch = { distance: spread(), zoom: modes.current.zoom }
    }
    const handleMove = (event: PointerEvent) => {
      if (!touches.has(event.pointerId)) return
      touches.set(event.pointerId, { x: event.clientX, y: event.clientY })
      const start = pinch
      if (touches.size !== 2 || !start || start.distance < 1) return
      const value = clampZoom(start.zoom * (start.distance / Math.max(1, spread())))
      saveZoom(value)
      setZoomState(value)
    }
    const handleUp = (event: PointerEvent) => {
      touches.delete(event.pointerId)
      if (touches.size < 2) pinch = null
    }

    element.addEventListener('wheel', handleWheel, { passive: false })
    element.addEventListener('pointerdown', handleDown)
    window.addEventListener('pointermove', handleMove)
    window.addEventListener('pointerup', handleUp)
    window.addEventListener('pointercancel', handleUp)
    return () => {
      element.removeEventListener('wheel', handleWheel)
      element.removeEventListener('pointerdown', handleDown)
      window.removeEventListener('pointermove', handleMove)
      window.removeEventListener('pointerup', handleUp)
      window.removeEventListener('pointercancel', handleUp)
    }
  }, [])

  const firstPerson = view === 'first'

  return (
    <div ref={container} className="scene3d live-scene" hidden={props.hidden}>
      <Canvas frameloop={props.hidden ? 'never' : 'always'} shadows={lowPower ? false : { type: PCFShadowMap }} dpr={lowPower ? [1, 1.5] : [1, 2]} camera={{ fov: 42, near: 0.5, far: 260, position: [0, 30, 30] }}>
        <color attach="background" args={[voidColor]} />
        <Suspense fallback={null}>
          <LiveWorld {...props} zoom={zoom} markers={markers} />
        </Suspense>
      </Canvas>
      <PartyMarkers heroes={others} bosses={bosses} markers={markers} />
      <div className="scene3d-vignette" />
      {firstPerson && <div className="live-crosshair" />}
      <div className="scene3d-zoom" onPointerDown={event => event.stopPropagation()}>
        {(mouseLook || touchLook) && (
          <button
            type="button"
            className="live-view"
            aria-label={firstPerson ? 'Third person' : 'First person'}
            title="V"
            onClick={() => onViewChange(firstPerson ? 'third' : 'first')}
          >
            {firstPerson ? '3P' : '1P'}
          </button>
        )}
        {!firstPerson && (
          <>
            <button type="button" aria-label="Zoom in" disabled={zoom <= minZoom} onClick={() => setZoom(zoom / zoomStep)}>
              +
            </button>
            <button type="button" aria-label="Zoom out" disabled={zoom >= maxZoom} onClick={() => setZoom(zoom * zoomStep)}>
              −
            </button>
          </>
        )}
      </div>
    </div>
  )
}

function LiveWorld({
  welcome,
  map,
  merchant,
  waystone,
  totem,
  graves,
  equipment,
  party,
  view,
  look: viewLook,
  items,
  chests,
  stairs,
  projectiles,
  projectileBuffer,
  blasts,
  sparks,
  heroes,
  monsters,
  metBosses,
  vision,
  corpses,
  heroBuffer,
  monsterBuffer,
  prediction,
  heroCues,
  monsterCues,
  popups,
  zoom,
  markers,
}: LiveSceneProps & { zoom: number; markers: RefObject<Map<number, HTMLDivElement>> }) {
  const kit = useKit(modelUrls.dungeon)
  const look = biomeLook(welcome.biome)
  const gear = useMemo(() => heroGear(equipment), [equipment])
  const me = heroes.find(hero => hero.id === welcome.heroId)
  const focusY = map.length + 2
  const firstPerson = view === 'first'
  const columns = useMemo(() => Math.max(0, ...map.map(row => row.length)), [map])
  const lit = useCallback((x: number, y: number) => map[y]?.[x] !== '#' && vision.explored.has(`${x},${y}`), [map, vision.explored])
  const myWeapon = equipment.find(equipped => equipped.slot === 'MainHand')?.item

  const sampled = () =>
    heroBuffer.sample(welcome.heroId, performance.now()) ?? {
      x: me?.x ?? 0,
      y: me?.y ?? 0,
      moving: false,
    }

  const follow = () => {
    const { predictor, move } = prediction
    if (!predictor.isReady || me?.dead) return sampled()
    const input = move.current
    const moving = input.x !== 0 || input.y !== 0
    const aiming = input.aimX !== 0 || input.aimY !== 0
    const facing = aiming ? Math.atan2(input.aimX, input.aimY) : moving ? Math.atan2(input.x, input.y) : undefined
    return { x: predictor.x, y: predictor.y, moving, facing, predicted: true }
  }

  const { size } = useThree()
  const reach = zoom * portraitFactor(size.width / Math.max(1, size.height))

  return (
    <>
      <fog attach="fog" args={firstPerson ? [voidColor, 20, 56] : [voidColor, 34 * reach, 90 * reach]} />
      <hemisphereLight args={[look.ambient, '#000000', 1]} />
      <CompassTracker
        markers={markers}
        me={follow}
        targets={[
          ...heroes
            .filter(hero => hero.id !== welcome.heroId)
            .map(hero => ({ id: hero.id, track: () => heroBuffer.sample(hero.id, performance.now()) ?? { x: hero.x, y: hero.y, moving: false } })),
          ...monsters
            .filter(monster => monster.isBoss && metBosses.has(monster.id))
            .map(monster => ({
              id: bossMarkerId(monster),
              track: () => monsterBuffer.sample(monster.id, performance.now()) ?? { x: monster.x, y: monster.y, moving: false },
            })),
        ]}
      />
      <FollowCamera follow={follow} light={look.light} zoom={zoom} firstPerson={firstPerson} alive={!(me?.dead ?? false)} look={viewLook} />
      {firstPerson && (
        <FirstPersonHands
          mainHand={gear.mainHandModel}
          offHand={gear.offHandModel}
          twoHanded={(myWeapon?.twoHanded ?? false) && !myWeapon?.isTwin}
          cue={heroCues.get(welcome.heroId)}
          alive={!(me?.dead ?? false)}
          sleeve={gear.outfit.body ? '#7d828c' : '#6b4f3a'}
        />
      )}
      <DungeonMap map={map} focusY={focusY} cutaway={!firstPerson} biome={welcome.biome} kit={kit} hole={stairs ?? undefined} blocked={noBlocks} />
      {chests.map(chest => (
        <Piece key={chest.id} piece={kit.chest} x={chest.x} y={chest.y} scale={1.25} />
      ))}
      {stairs && (
        <>
          <Built build={buildStairs} x={stairs.x} y={stairs.y} />
          {!stairs.isOpen && <Piece piece={kit['stairs-sealed']} x={stairs.x} y={stairs.y} lift={0.04} />}
          <Glow x={stairs.x} y={stairs.y} color={stairs.isOpen ? '#7affb0' : '#ff6a4a'} height={stairs.isOpen ? -1.2 : 0.4} />
        </>
      )}
      <Decor map={map} biome={welcome.biome} blocked={noBlocks} />
      <GridLines columns={columns} rows={map.length} lit={lit} />
      <Shroud map={map} explored={vision.explored} color={voidColor} focusY={firstPerson ? Infinity : focusY} />
      <Weather biome={welcome.biome} x={Math.round(me?.x ?? 0)} y={Math.round(me?.y ?? 0)} lit={lit} />
      {merchant && (
        <Suspense fallback={null}>
          <Actor url={merchantUrl} x={merchant.x} y={merchant.y} rotation={0} idle="Idle" tint="#e8c87a" scale={heroScale} />
        </Suspense>
      )}
      {waystone && (
        <group>
          <Piece piece={kit.waystone} x={waystone.x} y={waystone.y} scale={0.7} />
          <FloatingCrystal x={waystone.x} y={waystone.y} color="#5ae0ff" height={3.5} />
        </group>
      )}
      {totem && (
        <group>
          <Built build={buildTotem} x={totem.x} y={totem.y} />
          <FloatingCrystal x={totem.x} y={totem.y} color="#ff4a6a" height={4} />
        </group>
      )}
      {graves.map(grave => (
        <group key={grave.id}>
          <Piece piece={kit.grave} x={grave.x} y={grave.y} scale={0.9} />
          <Glow x={grave.x} y={grave.y} color={grave.isMine ? '#ff3a3a' : '#9a8aa0'} height={0.4} />
          {grave.isMine && (
            <group position={[grave.x * cell, 0, grave.y * cell]}>
              <RarityRing color="#ff2a2a" size={1.35} />
              <LootBeam color="#ff3a3a" pulse />
            </group>
          )}
          <Html position={[grave.x * cell, 3, grave.y * cell]} center zIndexRange={[20, 0]}>
            <div className="live-name">{grave.isMine ? 'Your grave' : `${grave.ownerName}'s grave`}</div>
          </Html>
        </group>
      ))}
      {me && !me.dead && !firstPerson && (
        <AimArc
          follow={follow}
          facing={me.facing}
          reach={me.reach}
          arcDegrees={me.arcDegrees}
          ranged={me.attack === 'Shoot' || me.attack === 'Cast'}
          monsters={monsters}
          monsterBuffer={monsterBuffer}
        />
      )}
      {heroes
        .filter(hero => !firstPerson || hero.id !== welcome.heroId)
        .map(hero => (
          <HeroActor
            key={hero.id}
            hero={hero}
            label={hero.id === welcome.heroId ? undefined : hero.away ? `${hero.name} · reconnecting` : hero.name}
            mainHand={(hero.id === welcome.heroId ? equipment : party.get(hero.id))?.find(equipped => equipped.slot === 'MainHand')?.item}
            gear={hero.id === welcome.heroId ? gear : heroGear(party.get(hero.id) ?? [])}
            cue={heroCues.get(hero.id)}
            track={
              hero.id === welcome.heroId
                ? follow
                : () =>
                    heroBuffer.sample(hero.id, performance.now()) ?? {
                      x: hero.x,
                      y: hero.y,
                      moving: false,
                    }
            }
          />
        ))}
      {monsters.filter(monster => vision.sight(Math.round(monster.x), Math.round(monster.y))).map(monster => (
        <Suspense key={monster.id} fallback={null}>
          <MonsterActor
            monster={monster}
            cue={monsterCues.get(monster.id)}
            strikeReach={welcome.monsterStrikeReach}
            dying={false}
            met={metBosses.has(monster.id)}
            track={() =>
              monsterBuffer.sample(monster.id, performance.now()) ?? {
                x: monster.x,
                y: monster.y,
                moving: false,
              }
            }
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
            met={false}
            track={() => ({
              x: corpse.monster.x,
              y: corpse.monster.y,
              moving: false,
            })}
          />
        </Suspense>
      ))}
      {projectiles.map(projectile => (
        <Projectile
          key={projectile.id}
          projectile={projectile}
          track={() => projectileBuffer.sample(projectile.id, performance.now(), projectile.ownerId === welcome.heroId ? renderDelaySeconds : 0) ?? projectile}
        />
      ))}
      {blasts.map(blast => (
        <BlastRing key={blast.key} blast={blast} />
      ))}
      <Bursts bursts={sparks.map(spark => ({ key: `spark-${spark.key}`, x: spark.x, y: spark.y, kind: spark.kind, delayMs: 0 }))} />
      {items.map(item => (
        <FloorItem key={item.id} x={item.x} y={item.y} item={item.item} />
      ))}
      {popups
        .filter(popup => popup.levelUpHero !== undefined)
        .map(popup => {
          const hero = heroes.find(candidate => candidate.id === popup.levelUpHero)
          return hero ? <LevelUpAura key={`aura-${popup.key}`} burst={{ key: `aura-${popup.key}`, x: hero.x, y: hero.y, kind: 'levelup', delayMs: 0 }} x={hero.x} y={hero.y} /> : null
        })}
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
      walk="Running_A"
      walkPace={heroRunPace}
      cue={cue}
      scale={heroScale}
      alive={!hero.dead}
      mainHand={gear.mainHandModel}
      offHand={gear.offHandModel}
      outfit={gear.outfit}
      ghostly={hero.invisible || hero.away}
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
  met: boolean
  track: () => Tracked
}

function MonsterActor({ monster, cue, strikeReach, dying, met, track }: MonsterActorProps) {
  const look = useMemo(() => monsterLook(monster.kind), [monster.kind])
  const modelCue = useMemo(() => {
    if (!cue) return undefined
    if (cue.clip === 'Windup')
      return {
        ...cue,
        clip: look.procedural ? 'Charge' : look.clips.Attack,
        durationMs: look.procedural ? cue.durationMs : (cue.durationMs ?? 0) / gltfStrikeAt,
      }
    return {
      ...cue,
      clip: look.clips[cue.clip as keyof typeof look.clips] ?? cue.clip,
    }
  }, [cue, look])
  const windup = cue?.clip === 'Windup' && cue.durationMs ? { start: cue.at, ms: cue.durationMs } : undefined
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
      {!dying && windup && <WindupMark key={`mark-${cue?.id}`} start={windup.start} ms={windup.ms} height={look.height} />}
      {!dying && monster.isBoss && !selfCrowned.has(monster.kind) && <BossCrown height={look.height} size={size} />}
      {!dying && (monster.isBoss ? <BossAura scale={size} /> : monster.rarity !== 'Common' && <RarityRing color={rarityColors[monster.rarity] ?? '#ffffff'} size={size} />)}
      {!dying && (!monster.isBoss || met) && (
        <Html position={[0, look.height + (monster.isBoss ? 1.5 : 0.5), 0]} center zIndexRange={[20, 0]} className="scene3d-label-anchor">
          <div className={`scene3d-label rarity-${monster.rarity.toLowerCase()}${monster.isBoss ? ' boss' : ''}`}>
            <span className="scene3d-name">
              {monster.isBoss && <Sprite sheet={sheets.items} index={crownSprite} className="scene3d-mark" />}
              {!monster.isBoss &&
                Array.from({ length: rarityStars[monster.rarity] }, (_, index) => (
                  <Sprite key={index} sheet={sheets.items} index={starSprites[monster.rarity]} className="scene3d-mark" />
                ))}
              {monster.name} <small>Lv{monster.level}</small>
              {monster.burning && <ElementIcon element="Fire" className="scene3d-mark" />}
              {monster.frozen && <ElementIcon element="Ice" className="scene3d-mark" />}
            </span>
            <span className="scene3d-health">
              <span style={{ width: `${Math.max(0, (monster.health / monster.maxHealth) * 100)}%` }} />
            </span>
          </div>
        </Html>
      )}
    </Actor>
  )
}

const gltfStrikeAt = 0.5
const blastMs = 550
const elementColors: Record<Element, string> = {
  Fire: '#ff7a2a',
  Ice: '#8fe0ff',
  Lightning: '#ffe45a',
  Wind: '#b6ffcf',
}

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
interface AimArcProps {
  follow: () => Tracked
  facing: number
  reach: number
  arcDegrees: number
  ranged: boolean
  monsters: LiveMonster[]
  monsterBuffer: FrameBuffer<LiveMonster>
}

function AimArc({ follow, facing, reach, arcDegrees, ranged, monsters, monsterBuffer }: AimArcProps) {
  const arc = useRef<Group>(null)
  const mark = useRef<Group>(null)
  const half = (arcDegrees / 2) * (Math.PI / 180)
  const radius = (reach + 0.35) * cell

  useFrame(() => {
    const spot = follow()
    const now = performance.now()
    let best: { x: number; y: number; distance: number } | null = null
    for (const monster of monsters) {
      const place = monsterBuffer.sample(monster.id, now) ?? monster
      const distance = Math.hypot(place.x - spot.x, place.y - spot.y)
      if (distance > reach + monster.radius) continue
      if (!best || distance < best.distance) best = { x: place.x, y: place.y, distance }
    }

    const angle = best && best.distance > 1e-3 ? Math.atan2(best.x - spot.x, best.y - spot.y) : facing

    if (arc.current) {
      arc.current.position.set(spot.x * cell, 0.07, spot.y * cell)
      arc.current.rotation.set(0, angle, 0)
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

function WindupMark({ start, ms, height }: { start: number; ms: number; height: number }) {
  const [shown, setShown] = useState(false)

  useEffect(() => {
    const wait = Math.max(0, start - performance.now())
    const showing = window.setTimeout(() => setShown(true), wait)
    const hiding = window.setTimeout(() => setShown(false), wait + ms)
    return () => {
      window.clearTimeout(showing)
      window.clearTimeout(hiding)
    }
  }, [start, ms])

  if (!shown) return null
  return (
    <Html position={[0, height + 1.2, 0]} center zIndexRange={[25, 0]}>
      <div className="live-windup">!</div>
    </Html>
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

interface FollowCameraProps {
  follow: () => Tracked
  light: string
  zoom: number
  firstPerson: boolean
  alive: boolean
  look: { current: Look }
}

function FollowCamera({ follow, light, zoom, firstPerson, alive, look }: FollowCameraProps) {
  const { camera, size } = useThree()
  const sun = useRef<DirectionalLight>(null)
  const glow = useRef<Group>(null)
  const focus = useRef<Vector3 | null>(null)
  const eye = useRef(eyeHeight)

  useFrame(state => {
    const view = state.camera
    const fov = firstPerson ? 82 : 42
    if (!(view instanceof PerspectiveCamera) || view.fov === fov) return
    view.fov = fov
    view.near = firstPerson ? 0.05 : 0.5
    view.updateProjectionMatrix()
  })

  useFrame((_, delta) => {
    const spot = follow()
    const target = new Vector3(spot.x * cell, 0, spot.y * cell)
    if (firstPerson) focus.current = target.clone()
    else if (!focus.current || focus.current.distanceTo(target) > cell * 9) focus.current = target.clone()
    else focus.current.lerp(target, Math.min(1, delta * 10))

    if (firstPerson) {
      eye.current += ((alive ? eyeHeight : fallenEye) - eye.current) * Math.min(1, delta * 3)
      const { yaw } = look.current
      const pitch = Math.max(-maxPitch, Math.min(maxPitch, look.current.pitch)) - 0.08
      camera.position.copy(focus.current).setY(eye.current)
      camera.lookAt(camera.position.clone().add(new Vector3(Math.sin(yaw) * Math.cos(pitch), Math.sin(pitch), Math.cos(yaw) * Math.cos(pitch))))
    } else {
      const aspect = size.width / Math.max(1, size.height)
      camera.position.copy(focus.current).addScaledVector(offsetFor(aspect), zoom * portraitFactor(aspect))
      camera.lookAt(focus.current.clone().add(lookAhead))
    }

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
        castShadow={!lowPower}
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

function portraitFactor(aspect: number): number {
  return Math.min(2.4, Math.max(1, 1.25 / aspect))
}

function offsetFor(aspect: number): Vector3 {
  const overhead = Math.min(1, Math.max(0, (1.25 / aspect - 1) / 1.2))
  return cameraOffset.clone().lerp(overheadOffset, overhead)
}

