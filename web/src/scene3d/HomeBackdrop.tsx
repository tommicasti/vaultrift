import { Canvas, useFrame, useThree } from '@react-three/fiber'
import { Suspense, useCallback, useMemo, useRef } from 'react'
import { AdditiveBlending, Group, MeshBasicMaterial, Object3D, PCFShadowMap, PointLight, Vector3 } from 'three'
import type { EquippedItem } from '../api/types'
import { Actor } from './Actor'
import { Decor } from './Decor'
import { DungeonMap } from './DungeonMap'
import { heroGear } from './heroLook'
import { useKit } from './kit'
import { biomeLook, cell, heroUrl, modelUrls } from './models'
import { detectLowPower } from './viewMode'

export interface HomeBackdropProps {
  biome: string
  classId: string
  gear: readonly Pick<EquippedItem, 'slot' | 'item'>[]
}

const room = ['#########', '#.......#', '#.......#', '#.......#', '#.......#', '#.......#', '#########']
const fire = { x: 4, y: 3 }
const seat = { x: 2.6, y: 3.15 }
const clear = new Set(['3,2', '4,2', '5,2', '2,3', '3,3', '4,3', '5,3', '6,3', '2,4', '3,4', '4,4', '5,4', '3,5', '4,5'])
const lowPower = detectLowPower()
const focus = new Vector3(fire.x * cell - 2.6, 1.3, fire.y * cell + 0.2)

export default function HomeBackdrop(props: HomeBackdropProps) {
  return (
    <Canvas
      className="home-backdrop"
      shadows={lowPower ? false : { type: PCFShadowMap }}
      dpr={lowPower ? [1, 1.5] : [1, 2]}
      camera={{ fov: 46, near: 0.3, far: 120, position: [fire.x * cell + 4, 6, fire.y * cell + 12] }}
    >
      <Suspense fallback={null}>
        <Hideout {...props} />
      </Suspense>
    </Canvas>
  )
}

function Hideout({ biome, classId, gear }: HomeBackdropProps) {
  const kit = useKit(modelUrls.dungeon)
  const look = biomeLook(biome)
  const worn = useMemo(() => heroGear(gear), [gear])

  return (
    <>
      <color attach="background" args={[look.fog]} />
      <fog attach="fog" args={[look.fog, 18, 46]} />
      <hemisphereLight args={[look.ambient, '#000000', 0.45]} />
      <SlowCamera />
      <DungeonMap map={room} focusY={room.length + 2} biome={biome} kit={kit} blocked={clear} />
      <Decor map={room} biome={biome} blocked={clear} />
      <Campfire x={fire.x * cell} z={fire.y * cell} />
      <Log x={seat.x * cell - Math.sin(facingFire) * 0.15} z={seat.y * cell - Math.cos(facingFire) * 0.15} angle={facingFire} />
      <SeatedHero classId={classId} mainHand={worn.mainHandModel} offHand={worn.offHandModel} outfit={worn.outfit} />
    </>
  )
}

function SlowCamera() {
  const { camera, size } = useThree()

  useFrame(({ clock }) => {
    const t = clock.elapsedTime
    const portrait = Math.min(2.3, Math.max(1, 1.2 / (size.width / Math.max(1, size.height))))
    const angle = 0.32 + Math.sin(t * 0.07) * 0.1
    const distance = 10.5 * portrait
    camera.position.set(focus.x + Math.sin(angle) * distance, 5.2 + Math.sin(t * 0.11) * 0.25, focus.z + Math.cos(angle) * distance)
    camera.lookAt(focus.x - (portrait - 1) * 1.2, focus.y - (portrait - 1) * 0.8, focus.z)
  })

  return null
}

interface SeatedHeroProps {
  classId: string
  mainHand?: string
  offHand?: string
  outfit: ReturnType<typeof heroGear>['outfit']
}

const sitDrop = 0.08
const thighLift = -1.5
const kneeBend = 1.45
const facingFire = Math.atan2(fire.x - seat.x, fire.y - seat.y)
const restPose = new WeakMap<Object3D, number>()

function bend(bone: Object3D | undefined, by: number) {
  if (!bone) return
  if (!restPose.has(bone)) restPose.set(bone, bone.rotation.x)
  bone.rotation.x = (restPose.get(bone) ?? 0) + by
}

function SeatedHero({ classId, mainHand, offHand, outfit }: SeatedHeroProps) {
  const posed = useCallback((body: Object3D) => {
    body.position.y = -sitDrop
    for (const side of ['l', 'r']) {
      bend(body.getObjectByName(`upperleg${side}`), thighLift)
      bend(body.getObjectByName(`lowerleg${side}`), kneeBend)
    }
  }, [])

  return (
    <Actor
      url={heroUrl(classId)}
      x={seat.x}
      y={seat.y}
      rotation={facingFire}
      idle="Idle"
      mainHand={mainHand}
      offHand={offHand}
      outfit={outfit}
      posed={posed}
    />
  )
}

function Log({ x, z, angle }: { x: number; z: number; angle: number }) {
  return (
    <mesh position={[x, 0.32, z]} rotation={[0, angle, Math.PI / 2]} castShadow receiveShadow>
      <cylinderGeometry args={[0.34, 0.38, 2.6, 10]} />
      <meshStandardMaterial color="#5a3d26" roughness={0.95} />
    </mesh>
  )
}

const stones = Array.from({ length: 10 }, (_, index) => {
  const angle = (index / 10) * Math.PI * 2
  return { x: Math.cos(angle) * 1.15, z: Math.sin(angle) * 1.15, size: 0.22 + ((index * 7) % 5) * 0.03 }
})

const logs = Array.from({ length: 5 }, (_, index) => (index / 5) * Math.PI * 2)

const embers = Array.from({ length: 22 }, (_, index) => ({
  phase: (index * 0.137) % 1,
  angle: index * 2.4,
  radius: 0.15 + ((index * 13) % 7) * 0.06,
  speed: 0.35 + ((index * 5) % 4) * 0.1,
}))

function Campfire({ x, z }: { x: number; z: number }) {
  const light = useRef<PointLight>(null)
  const flames = useRef<Group>(null)
  const sparks = useRef<Group>(null)
  const flameMaterials = useMemo(
    () =>
      ['#ff5a14', '#ff9a2a', '#ffe08a'].map(
        color => new MeshBasicMaterial({ color, transparent: true, opacity: 0.85, blending: AdditiveBlending, depthWrite: false }),
      ),
    [],
  )

  useFrame(({ clock }) => {
    const t = clock.elapsedTime
    const flicker = 0.82 + Math.sin(t * 9.1) * 0.08 + Math.sin(t * 23.7) * 0.06 + Math.sin(t * 3.3) * 0.05
    if (light.current) light.current.intensity = 160 * flicker
    flames.current?.children.forEach((child, index) => {
      const sway = Math.sin(t * (5 + index * 1.7) + index) * 0.12
      child.scale.set(1 + sway * 0.4, flicker * (1 + Math.sin(t * (11 + index * 3)) * 0.12), 1 - sway * 0.3)
      child.rotation.y = t * (0.8 + index * 0.4)
    })
    sparks.current?.children.forEach((child, index) => {
      const ember = embers[index]
      const progress = (t * ember.speed + ember.phase) % 1
      child.position.set(Math.cos(ember.angle + progress * 2) * ember.radius, 0.5 + progress * 4.2, Math.sin(ember.angle + progress * 2) * ember.radius)
      child.scale.setScalar(1 - progress)
    })
  })

  return (
    <group position={[x, 0, z]}>
      {stones.map((stone, index) => (
        <mesh key={index} position={[stone.x, stone.size * 0.6, stone.z]} castShadow receiveShadow>
          <dodecahedronGeometry args={[stone.size, 0]} />
          <meshStandardMaterial color="#6a6560" roughness={1} />
        </mesh>
      ))}
      {logs.map((angle, index) => (
        <mesh key={index} position={[Math.cos(angle) * 0.35, 0.25, Math.sin(angle) * 0.35]} rotation={[0, -angle, Math.PI / 2 - 0.35]} castShadow>
          <cylinderGeometry args={[0.13, 0.16, 1.3, 7]} />
          <meshStandardMaterial color="#4a3020" roughness={1} emissive="#ff4a10" emissiveIntensity={0.18} />
        </mesh>
      ))}
      <group ref={flames} position={[0, 0.35, 0]}>
        {flameMaterials.map((material, index) => (
          <mesh key={index} material={material} position={[0, (0.9 - index * 0.2) * 0.5, 0]}>
            <coneGeometry args={[0.55 - index * 0.14, 1.9 - index * 0.45, 7, 1, true]} />
          </mesh>
        ))}
      </group>
      <group ref={sparks}>
        {embers.map((_, index) => (
          <Ember key={index} />
        ))}
      </group>
      <pointLight ref={light} position={[0, 1.6, 0]} color="#ff9a48" distance={42} decay={1.6} castShadow={!lowPower} shadow-mapSize={[512, 512]} />
    </group>
  )
}

function Ember() {
  return (
    <mesh>
      <sphereGeometry args={[0.045, 5, 5]} />
      <meshBasicMaterial color="#ffb050" />
    </mesh>
  )
}
