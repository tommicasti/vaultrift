import { useGLTF } from '@react-three/drei'
import { useFrame } from '@react-three/fiber'
import { useCallback, useEffect, useMemo, useRef, type ReactNode } from 'react'
import { AnimationAction, AnimationClip, AnimationMixer, Color, Group, LoopOnce, LoopRepeat, Mesh, MeshStandardMaterial, Object3D, Vector3 } from 'three'
import { clone } from 'three/examples/jsm/utils/SkeletonUtils.js'
import { buildBow, buildSpear } from './creatures'
import { useKitObjects } from './kit'
import { cell, modelUrls } from './models'

export interface Cue {
  id: number
  clip: string
  delayMs: number
  hold?: boolean
}

interface ActorBaseProps {
  x: number
  y: number
  rotation: number
  idle: string
  walk?: string
  cue?: Cue
  tint?: string
  scale?: number
  mainHand?: string
  offHand?: string
  ghostly?: boolean
  outfit?: Outfit
  alive?: boolean
  sinking?: boolean
  hidden?: boolean
  procedural?: boolean
  children?: ReactNode
}

export interface Outfit {
  head: boolean
  cloak: string | null
  body: boolean
  feet: boolean
}

const originals = new WeakMap<Mesh, MeshStandardMaterial>()
const bareCloth = new Color('#b9ab92')

function dress(mesh: Mesh, material: MeshStandardMaterial, outfit: Outfit) {
  const name = mesh.name
  if (/_(Hat|Helmet)$/.test(name)) mesh.visible = outfit.head
  if (/_Cape$/.test(name)) {
    mesh.visible = outfit.cloak !== null
    if (outfit.cloak && outfit.cloak !== '') material.color = new Color('#ffffff').lerp(new Color(outfit.cloak), 0.7)
  }
  if (/_Body$/.test(name) && !outfit.body) material.color = bareCloth.clone()
  if (/_Leg(Left|Right)$/.test(name) && !outfit.feet) material.color = bareCloth.clone()
}

interface ActorProps extends ActorBaseProps {
  url?: string
  build?: () => Object3D
}

interface ActorBodyProps extends ActorBaseProps {
  source: Object3D
  clips: AnimationClip[]
}

const proceduralSeconds: Record<string, number> = {
  Attack: 0.45,
  Hit: 0.35,
  Death: 0.9,
  Wake: 0.5,
  Spawn: 0.5,
}

function jawOpening(name: string, t: number, progress: number): number {
  switch (name) {
    case 'Sleep':
      return 0
    case 'Walk':
      return 0.35 * Math.abs(Math.sin(t * 14))
    case 'Attack':
      return 1.1 * Math.sin(progress * Math.PI)
    case 'Hit':
      return 0.45 * (1 - progress)
    case 'Death':
      return 1.2 * progress
    case 'Wake':
    case 'Spawn':
      return 0.9 * Math.sin(progress * Math.PI)
    default:
      return 0.12 + 0.12 * Math.abs(Math.sin(t * 3))
  }
}

interface Parts {
  lid?: Object3D
  legs: Object3D[]
  claws: Object3D[]
  tail?: Object3D
}

function findParts(body: Object3D): Parts {
  const parts: Parts = { lid: body.getObjectByName('chest_lid'), legs: [], claws: [], tail: body.getObjectByName('tail') }
  body.traverse(child => {
    if (child.userData.leg !== undefined) parts.legs.push(child)
    if (child.userData.claw !== undefined) parts.claws.push(child)
  })
  return parts
}

function animateParts(parts: Parts, name: string, t: number, progress: number) {
  parts.legs.forEach((leg, index) => {
    const base = leg.userData.yaw as number
    const lift = leg.getObjectByName('hip')
    const phase = index % 2 === 0 ? 0 : Math.PI
    let swing = Math.sin(t * 2 + index) * 0.03
    let raise = 0

    if (name === 'Walk') {
      swing = Math.sin(t * 16 + phase) * 0.3
      raise = Math.max(0, Math.sin(t * 16 + phase)) * 0.35
    } else if (name === 'Attack' && index < 2) raise = Math.sin(progress * Math.PI) * 0.6
    else if (name === 'Death') raise = -progress * 0.9
    else if (name === 'Sleep') raise = -0.25

    leg.rotation.set(0, base + swing, 0)
    if (lift) lift.rotation.set(0, 0, (lift.userData.base as number) + raise)
  })

  parts.claws.forEach(claw => {
    let open = 0.15 + Math.abs(Math.sin(t * 3)) * 0.1
    if (name === 'Attack') open = progress < 0.5 ? 0.7 : 0.05
    else if (name === 'Hit') open = 0.5
    else if (name === 'Death' || name === 'Sleep') open = 0
    claw.rotation.set(0, (claw.userData.claw as number) * open, 0)
  })

  if (parts.tail) {
    let strike = Math.sin(t * 2) * 0.05
    if (name === 'Attack') strike = Math.sin(progress * Math.PI) * 0.9
    else if (name === 'Death') strike = -progress * 0.8
    if (parts.tail.userData.sway) parts.tail.rotation.set(0.35 + strike * 0.4, Math.sin(t * (name === 'Walk' ? 9 : 2.4)) * 0.45, 0)
    else parts.tail.rotation.set(strike, 0, 0)
  }
}

interface Motion {
  name: string
  start: number
}

function applyMotion(pose: Group, parts: Parts, motion: Motion, moving: boolean, size: number, now: number) {
  const lid = parts.lid
  const t = (now - motion.start) / 1000
  const name = moving && !(motion.name in proceduralSeconds) ? 'Walk' : motion.name
  const progress = Math.min(1, (t * clipSpeed(name)) / (proceduralSeconds[name] ?? 1))
  pose.position.set(0, 0, 0)
  pose.rotation.set(0, 0, 0)
  pose.scale.set(1, 1, 1)
  if (lid) lid.rotation.x = -jawOpening(name, t, progress)
  animateParts(parts, name, t, progress)

  switch (name) {
    case 'Sleep':
      if (!lid) pose.scale.set(1.04, 0.86 + Math.sin(t * 1.3) * 0.02, 1.04)
      break
    case 'Walk':
      pose.position.y = Math.abs(Math.sin(t * 14)) * 0.08 * size
      pose.rotation.z = Math.sin(t * 14) * 0.08
      break
    case 'Attack': {
      const lunge = progress < 0.4 ? Math.sin((progress / 0.4) * (Math.PI / 2)) : Math.cos(((progress - 0.4) / 0.6) * (Math.PI / 2))
      pose.position.z = lunge * 0.3 * size
      pose.rotation.x = lunge * 0.3
      pose.scale.set(1 + lunge * 0.06, 1 - lunge * 0.08, 1 + lunge * 0.1)
      break
    }
    case 'Hit':
      pose.position.x = Math.sin(t * 60) * 0.05 * size * (1 - progress)
      pose.scale.set(1 + 0.08 * (1 - progress), 1 - 0.1 * (1 - progress), 1 + 0.08 * (1 - progress))
      break
    case 'Death':
      pose.rotation.z = progress * Math.PI * 0.92
      pose.position.y = (Math.sin(progress * Math.PI) * 0.25 + progress * 0.7) * size
      pose.scale.setScalar(1 - progress * 0.15)
      break
    case 'Wake':
    case 'Spawn':
      pose.position.y = Math.sin(progress * Math.PI) * 0.3 * size
      if (name === 'Spawn') pose.scale.setScalar(0.4 + progress * 0.6)
      break
    default:
      pose.scale.set(1 - Math.sin(t * 2.2) * 0.02, 1 + Math.sin(t * 2.2) * 0.04, 1 - Math.sin(t * 2.2) * 0.02)
      pose.rotation.y = Math.sin(t * 0.9) * 0.06
  }
}

const builtWeapons: Record<string, () => Object3D> = { bow: buildBow, spear: buildSpear }

const sinkDelayMs = 350
const sinkSpeed = 3.6

function clipSpeed(name: string): number {
  if (/Attack|Shoot|Spellcast|Punch/.test(name)) return 1.7
  if (/Hit/.test(name)) return 1.4
  if (/Death/.test(name)) return 2.6
  return 1
}

const stepSpeed = cell * 6.5
const turnSpeed = 14
const teleportDistance = cell * 9
const maxFrameDelta = 1 / 20
const fadeSeconds = 0.18

export function Actor({ url, build, ...rest }: ActorProps) {
  if (build) return <BuiltActor build={build} {...rest} />
  if (!url) return null
  return <GltfActor url={url} {...rest} />
}

function GltfActor({ url, ...rest }: ActorBaseProps & { url: string }) {
  const { scene, animations: ownClips } = useGLTF(url)
  const { animations: sharedClips } = useGLTF(modelUrls.animations)
  return <ActorBody source={scene} clips={ownClips.length > 0 ? ownClips : sharedClips} {...rest} />
}

function BuiltActor({ build, ...rest }: ActorBaseProps & { build: () => Object3D }) {
  const source = useMemo(() => build(), [build])
  return <ActorBody source={source} clips={[]} {...rest} procedural />
}

function ActorBody({ source, clips: animations, x, y, rotation, idle, walk = 'Walking_A', cue, tint, scale = 1, mainHand, offHand, ghostly, hidden, procedural, outfit, alive = true, sinking = false, children }: ActorBodyProps) {
  const weapons = useKitObjects(modelUrls.weapons)
  const root = useRef<Group>(null)
  const pose = useRef<Group>(null)
  const motion = useRef<Motion>({ name: idle, start: 0 })
  const body = useMemo(() => clone(source), [source])
  const mixer = useMemo(() => new AnimationMixer(body), [body])
  const parts = useMemo(() => findParts(body), [body])
  const actions = useMemo(() => {
    const map = new Map<string, AnimationAction>()
    for (const clip of animations) map.set(clip.name, mixer.clipAction(clip))
    return map
  }, [animations, mixer])
  const state = useRef({ base: idle, current: null as AnimationAction | null, busyUntil: 0, held: false })
  const placed = useRef(false)
  const sinkStart = useRef(0)
  const target = useMemo(() => new Vector3(x * cell, 0, y * cell), [x, y])

  useEffect(() => {
    body.traverse(child => {
      if (!(child instanceof Mesh)) return

      child.castShadow = true
      child.receiveShadow = true
      if (!originals.has(child)) originals.set(child, child.material as MeshStandardMaterial)
      const material = (originals.get(child) as MeshStandardMaterial).clone()
      if (outfit) dress(child, material, outfit)
      if (tint) material.color = new Color('#ffffff').lerp(new Color(tint), 0.65)
      if (ghostly) {
        material.transparent = true
        material.opacity = 0.55
        material.emissive = new Color(tint ?? '#ffffff')
        material.emissiveIntensity = 0.35
      }
      child.material = material
    })
  }, [body, tint, ghostly, outfit])

  useEffect(() => {
    const attached: Object3D[] = []
    const attach = (boneName: string, modelName: string | undefined) => {
      const bone = body.getObjectByName(boneName)
      const source = modelName ? (weapons[modelName] ?? builtWeapons[modelName]?.()) : undefined
      if (!bone || !source) return

      const weapon = source.clone(true)
      weapon.position.set(0, 0, 0)
      weapon.traverse(child => {
        if (child instanceof Mesh) child.castShadow = true
      })
      bone.add(weapon)
      attached.push(weapon)
    }

    attach('handslotr', mainHand)
    attach('handslotl', offHand)
    return () => attached.forEach(weapon => weapon.removeFromParent())
  }, [body, weapons, mainHand, offHand])

  const play = useCallback(
    (name: string, once: boolean) => {
      const action = actions.get(name)
      if (!action) return null

      const previous = state.current.current
      action.reset()
      action.timeScale = clipSpeed(name)
      action.setLoop(once ? LoopOnce : LoopRepeat, once ? 1 : Infinity)
      action.clampWhenFinished = once
      action.enabled = true
      action.setEffectiveWeight(1)
      action.play()
      if (previous && previous !== action) previous.crossFadeTo(action, fadeSeconds, false)
      state.current.current = action
      return action
    },
    [actions],
  )

  useEffect(() => () => {
    mixer.stopAllAction()
  }, [mixer])

  useEffect(() => {
    state.current.base = idle
    if (state.current.held || performance.now() <= state.current.busyUntil) return
    if (procedural) motion.current = { name: idle, start: performance.now() }
    else play(idle, false)
  }, [idle, play, procedural])

  useEffect(() => {
    if (!alive || !state.current.held) return
    state.current.held = false
    state.current.busyUntil = 0
    if (procedural) motion.current = { name: state.current.base, start: performance.now() }
    else play(state.current.base, false)
  }, [alive, play, procedural])

  useEffect(() => {
    if (!cue) return

    const timer = window.setTimeout(() => {
      if (procedural) {
        const now = performance.now()
        motion.current = { name: cue.clip, start: now }
        state.current.held = cue.hold === true
        state.current.busyUntil = now + ((proceduralSeconds[cue.clip] ?? 0.5) * 1000) / clipSpeed(cue.clip)
        return
      }

      const action = play(cue.clip, true)
      if (!action) return

      state.current.held = cue.hold === true
      state.current.busyUntil = performance.now() + (action.getClip().duration * 1000) / Math.max(action.timeScale, 0.01)
    }, cue.delayMs)

    return () => window.clearTimeout(timer)
  }, [cue, play, procedural])

  useFrame((_, frameDelta) => {
    const delta = Math.min(frameDelta, maxFrameDelta)
    const group = root.current
    if (!group) return

    if (!placed.current) {
      group.position.copy(target)
      placed.current = true
    }

    const distance = group.position.distanceTo(target)
    const moving = distance > 0.05

    if (distance > teleportDistance) group.position.copy(target)
    else if (moving) group.position.add(target.clone().sub(group.position).clampLength(0, stepSpeed * Math.max(1, distance / cell) * delta))

    let turn = rotation - group.rotation.y
    turn = Math.atan2(Math.sin(turn), Math.cos(turn))
    group.rotation.y += turn * Math.min(1, turnSpeed * delta)

    const now = performance.now()
    const current = state.current

    if (sinking && sinkStart.current === 0) sinkStart.current = now + sinkDelayMs
    const sunk = sinking && now > sinkStart.current ? ((now - sinkStart.current) / 1000) * sinkSpeed * scale : 0

    if (procedural) {
      if (!current.held && now > current.busyUntil && motion.current.name in proceduralSeconds)
        motion.current = { name: current.base, start: now }
      if (pose.current) {
        applyMotion(pose.current, parts, motion.current, moving, scale, now)
        pose.current.position.y -= sunk
      }
      return
    }

    if (pose.current) pose.current.position.y = -sunk

    if (!current.held && now > current.busyUntil) {
      const wanted = moving ? walk : current.base
      if (current.current !== actions.get(wanted)) play(wanted, false)
    }

    mixer.update(delta)
  })

  return (
    <group ref={root} visible={!hidden}>
      <group ref={pose}>
        <primitive object={body} scale={scale} />
      </group>
      {children}
    </group>
  )
}
