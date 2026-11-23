import { useFrame } from '@react-three/fiber'
import { useLayoutEffect, useMemo, useRef } from 'react'
import { Color, DoubleSide, InstancedMesh, Matrix4, MeshStandardMaterial, Object3D, Quaternion, ShaderMaterial, Shape, ShapeGeometry, Vector3, type Material } from 'three'
import type { KitPiece } from './kit'
import { biomeLook, cell } from './models'

interface DungeonMapProps {
  map: string[]
  blocked?: ReadonlySet<string>
  hole?: { x: number; y: number }
  focusY: number
  cutaway?: boolean
  biome: string | undefined
  kit: Record<string, KitPiece>
}

interface Placement {
  x: number
  z: number
  rotation?: number
  scaleY?: number
  scale?: number
  lift?: number
}

const wallCutaway = 0.28

const furniturePieces: [string, number][] = [
  ['crates', 0.8],
  ['barrels-small', 1],
  ['keg', 0.55],
  ['table', 0.85],
  ['trunk', 1.3],
  ['box', 0.9],
  ['candles', 1.2],
  ['weapon-rack', 1],
  ['stool', 1],
]

const furnitureSides = [
  { dx: 0, dy: -1, rotation: 0 },
  { dx: -1, dy: 0, rotation: Math.PI / 2 },
  { dx: 1, dy: 0, rotation: -Math.PI / 2 },
]
const offsets = [
  { dx: 0, dy: -1 },
  { dx: 0, dy: 1 },
  { dx: -1, dy: 0 },
  { dx: 1, dy: 0 },
]

function hash(x: number, y: number): number {
  let value = (x * 374761393 + y * 668265263) ^ 0x5bf03635
  value = Math.imul(value ^ (value >>> 13), 1274126177)
  return ((value ^ (value >>> 16)) >>> 0) / 4294967295
}

function isWall(map: string[], x: number, y: number): boolean {
  const symbol = map[y]?.[x]
  return symbol === undefined || symbol === '#'
}

export function DungeonMap({ map, focusY, cutaway = true, biome, kit, hole, blocked }: DungeonMapProps) {
  const look = biomeLook(biome)
  const holeX = hole?.x
  const holeY = hole?.y

  const layout = useMemo(() => {
    const floors: Placement[] = []
    const rocks: Placement[] = []
    const lava: Placement[] = []
    const water: Placement[] = []
    const spores: Placement[] = []
    const crystals: Placement[] = []
    const currents: Placement[] = []
    const barrels: Placement[] = []
    const rubble: Placement[] = []
    const walls: Placement[] = []
    const cracked: Placement[] = []
    const torches: Placement[] = []
    const shelves: Placement[] = []
    const windows: Placement[] = []
    const arches: Placement[] = []
    const banners: Placement[] = []
    const furniture: Record<string, Placement[]> = Object.fromEntries(furniturePieces.map(([name]) => [name, []]))
    const corners = new Map<string, Placement>()

    map.forEach((row, y) => {
      for (let x = 0; x < row.length; x++) {
        const symbol = row[x]
        if (symbol === '#') continue

        const spot = { x: x * cell, z: y * cell }
        const roll = hash(x, y)

        const isHole = holeX === x && holeY === y

        if (symbol === 'L') lava.push(spot)
        else if (symbol === '~') water.push(spot)
        else if (!isHole && roll < 0.12) rocks.push(spot)
        else if (!isHole) floors.push(spot)

        if (symbol === 'S') spores.push({ ...spot, rotation: roll * Math.PI * 2 })
        if (symbol === 'C') crystals.push({ ...spot, rotation: roll * Math.PI * 2, scale: 0.8 + roll * 0.4 })
        if (symbol === 'o') (roll < 0.5 ? barrels : rubble).push({ ...spot, rotation: Math.floor(roll * 4) * (Math.PI / 2), scale: roll < 0.5 ? 1.25 : 0.6 })
        if (symbol === '.' && !isHole && !blocked?.has(`${x},${y}`) && hash(x * 7, y * 3) < 0.13) {
          const side = furnitureSides.find(({ dx, dy }) => isWall(map, x + dx, y + dy) && !isWall(map, x + dy, y + dx) && !isWall(map, x - dy, y - dx))
          if (side) {
            const [name, scale] = furniturePieces[Math.floor(hash(x * 11, y * 17) * furniturePieces.length)]
            furniture[name].push({ x: (x + side.dx * 0.3) * cell, z: (y + side.dy * 0.3) * cell, rotation: side.rotation, scale })
          }
        }
        if ('^v<>'.includes(symbol)) currents.push({ ...spot, rotation: { '^': Math.PI, v: 0, '<': -Math.PI / 2, '>': Math.PI / 2 }[symbol] })

        for (const { dx, dy } of offsets) {
          if (!isWall(map, x + dx, y + dy)) continue

          const towardCamera = cutaway && dy === 1
          const placement: Placement = {
            x: (x + dx / 2) * cell,
            z: (y + dy / 2) * cell,
            rotation: dx === 0 ? 0 : Math.PI / 2,
            scaleY: towardCamera ? wallCutaway : 1,
          }
          const variant = hash(x * 3 + dx, y * 7 + dy)
          const hasTorch = dy === -1 && hash(x, y * 13) < 0.12
          if (dy === -1 && !hasTorch && variant < 0.1) shelves.push(placement)
          else if (dy === -1 && !hasTorch && variant < 0.17) windows.push(placement)
          else if (dy === -1 && !hasTorch && variant < 0.22) arches.push(placement)
          else (variant < 0.37 ? cracked : walls).push(placement)

          if (hasTorch) torches.push({ x: x * cell, z: (y - 0.5) * cell + 0.5, rotation: 0, lift: 2.4 })
          else if (dy === -1 && variant >= 0.22 && hash(x * 5, y * 11) < 0.12) banners.push({ ...placement })

          const ends =
            dx === 0
              ? [[x - 0.5, y + dy / 2], [x + 0.5, y + dy / 2]]
              : [[x + dx / 2, y - 0.5], [x + dx / 2, y + 0.5]]
          for (const [cx, cz] of ends) {
            const key = `${cx},${cz}`
            const lowered = towardCamera || (corners.get(key)?.scaleY ?? 1) < 1
            corners.set(key, { x: cx * cell, z: cz * cell, scaleY: lowered ? wallCutaway : 1, scale: 0.55 })
          }
        }
      }
    })

    return { floors, rocks, lava, water, spores, crystals, currents, barrels, rubble, walls, cracked, torches, shelves, windows, arches, banners, furniture, pillars: [...corners.values()] }
  }, [map, holeX, holeY, blocked, cutaway])

  const cut = useMemo(() => {
    const edge = cutaway ? (focusY + 0.6) * cell : Infinity
    const lower = (placements: Placement[]) =>
      placements.map(placement => (placement.z > edge ? { ...placement, scaleY: wallCutaway } : placement))
    const above = (placements: Placement[]) => placements.filter(placement => placement.z <= edge)
    return {
      walls: [...lower(layout.walls), ...lower(layout.shelves.filter(placement => placement.z > edge)), ...lower(layout.windows.filter(placement => placement.z > edge)), ...lower(layout.arches.filter(placement => placement.z > edge))],
      cracked: lower(layout.cracked),
      pillars: lower(layout.pillars),
      torches: above(layout.torches),
      shelves: above(layout.shelves),
      windows: above(layout.windows),
      arches: above(layout.arches),
      banners: above(layout.banners),
    }
  }, [layout, focusY, cutaway])

  const tinted = useMemo(() => {
    const tint = (piece: KitPiece | undefined, color: string): Material | undefined => {
      if (!piece) return undefined
      const material = (piece.material as MeshStandardMaterial).clone()
      material.color = new Color(color)
      return material
    }

    return {
      floor: tint(kit.floor, look.floor),
      rocks: tint(kit['floor-rocks'], look.floor),
      wall: tint(kit.wall, look.wall),
      cracked: tint(kit['wall-cracked'], look.wall),
      pillar: tint(kit.pillar, look.wall),
      shelves: tint(kit['wall-shelves'], look.wall),
      window: tint(kit['wall-window'], look.wall),
      arched: tint(kit['wall-arched'], look.wall),
    }
  }, [kit, look])

  return (
    <group>
      <Instances piece={kit.floor} material={tinted.floor} placements={layout.floors} receiveShadow />
      <Instances piece={kit['floor-rocks']} material={tinted.rocks} placements={layout.rocks} receiveShadow />
      <Instances piece={kit.wall} material={tinted.wall} placements={cut.walls} castShadow receiveShadow />
      <Instances piece={kit['wall-cracked']} material={tinted.cracked} placements={cut.cracked} castShadow receiveShadow />
      <Instances piece={kit.pillar} material={tinted.pillar} placements={cut.pillars} castShadow />
      <Instances piece={kit.barrel} placements={layout.barrels} castShadow receiveShadow />
      <Instances piece={kit.rubble} placements={layout.rubble} castShadow receiveShadow />
      <Instances piece={kit['wall-shelves']} material={tinted.shelves} placements={cut.shelves} castShadow receiveShadow />
      <Instances piece={kit['wall-window']} material={tinted.window} placements={cut.windows} castShadow receiveShadow />
      <Instances piece={kit['wall-arched']} material={tinted.arched} placements={cut.arches} castShadow receiveShadow />
      <Instances piece={kit['banner-wall']} placements={cut.banners} />
      {furniturePieces.map(([name]) => (
        <Instances key={name} piece={kit[name]} placements={layout.furniture[name]} castShadow receiveShadow />
      ))}
      <Instances piece={kit.torch} placements={cut.torches} />
      <Flames placements={cut.torches} />
      <Liquid placements={layout.lava} dark="#7a1000" bright="#ffc23a" speed={0.9} depth={0.35} />
      <Embers placements={layout.lava} />
      <Liquid placements={layout.water} dark="#0a2f6a" bright="#5ac0ff" opacity={0.85} speed={0.6} depth={0.25} />
      <Crystals placements={layout.crystals} />
      <Spores placements={layout.spores} />
      <Currents placements={layout.currents} />
    </group>
  )
}

interface InstancesProps {
  piece: KitPiece | undefined
  material?: Material
  placements: Placement[]
  castShadow?: boolean
  receiveShadow?: boolean
}

const dummy = new Object3D()

function applyPlacements(mesh: InstancedMesh, placements: Placement[], baseY = 0) {
  placements.forEach((placement, index) => {
    const scale = placement.scale ?? 1
    dummy.position.set(placement.x, baseY + (placement.lift ?? 0), placement.z)
    dummy.rotation.set(0, placement.rotation ?? 0, 0)
    dummy.scale.set(scale, scale * (placement.scaleY ?? 1), scale)
    dummy.updateMatrix()
    mesh.setMatrixAt(index, dummy.matrix)
  })
  mesh.instanceMatrix.needsUpdate = true
  mesh.computeBoundingSphere()
}

function Instances({ piece, material, placements, castShadow, receiveShadow }: InstancesProps) {
  const ref = useRef<InstancedMesh>(null)

  useLayoutEffect(() => {
    if (ref.current) applyPlacements(ref.current, placements)
  }, [placements])

  if (!piece || placements.length === 0) return null

  return (
    <instancedMesh
      key={`${placements.length}-${(material ?? piece.material).uuid}`}
      ref={ref}
      args={[piece.geometry, material ?? piece.material, placements.length]}
      castShadow={castShadow}
      receiveShadow={receiveShadow}
    />
  )
}

interface LiquidProps {
  placements: Placement[]
  dark: string
  bright: string
  opacity?: number
  speed: number
  depth: number
}

const liquidVertex = `
  varying vec3 vWorld;
  void main() {
    vec4 local = vec4(position, 1.0);
    #ifdef USE_INSTANCING
      local = instanceMatrix * local;
    #endif
    vec4 world = modelMatrix * local;
    vWorld = world.xyz;
    gl_Position = projectionMatrix * viewMatrix * world;
  }
`

const liquidFragment = `
  uniform float time;
  uniform float speed;
  uniform vec3 dark;
  uniform vec3 bright;
  uniform float opacity;
  varying vec3 vWorld;
  void main() {
    vec2 p = vWorld.xz * 0.55;
    float t = time * speed;
    float n = sin(p.x * 1.7 + t) * sin(p.y * 1.3 - t * 0.8);
    n += 0.5 * sin((p.x + p.y) * 3.1 + t * 1.9);
    n += 0.25 * sin((p.x - p.y) * 6.3 - t * 2.7);
    n += 0.35 * sin(p.x * 2.3 - p.y * 4.1 + t * 0.6 + sin(p.y * 1.1));
    float glow = smoothstep(-0.9, 1.2, n);
    vec3 color = mix(dark, bright, glow);
    gl_FragColor = vec4(color, opacity);
  }
`

function Liquid({ placements, dark, bright, opacity = 1, speed, depth }: LiquidProps) {
  const ref = useRef<InstancedMesh>(null)
  const material = useMemo(
    () =>
      new ShaderMaterial({
        uniforms: {
          time: { value: 0 },
          speed: { value: speed },
          dark: { value: new Color(dark) },
          bright: { value: new Color(bright) },
          opacity: { value: opacity },
        },
        vertexShader: liquidVertex,
        fragmentShader: liquidFragment,
        transparent: opacity < 1,
      }),
    [dark, bright, opacity, speed],
  )

  useLayoutEffect(() => {
    if (ref.current) applyPlacements(ref.current, placements, -depth)
  }, [placements, depth])

  useFrame(({ clock }) => {
    const mesh = ref.current
    if (mesh) (mesh.material as ShaderMaterial).uniforms.time.value = clock.elapsedTime
  })

  if (placements.length === 0) return null

  return (
    <instancedMesh key={`${placements.length}-${material.uuid}`} ref={ref} args={[undefined, material, placements.length]}>
      <boxGeometry args={[cell, 0.2, cell]} />
    </instancedMesh>
  )
}

const emberCount = 3

function Embers({ placements }: { placements: Placement[] }) {
  const ref = useRef<InstancedMesh>(null)
  const count = Math.min(placements.length * emberCount, 240)

  useFrame(({ clock }) => {
    const mesh = ref.current
    if (!mesh) return

    for (let index = 0; index < count; index++) {
      const spot = placements[index % placements.length]
      const seed = hash(index, placements.length)
      const life = (clock.elapsedTime * (0.35 + seed * 0.3) + seed * 7) % 1
      dummy.position.set(spot.x + (seed - 0.5) * cell * 0.8, life * 3.2, spot.z + (hash(seed * 999, index) - 0.5) * cell * 0.8)
      dummy.rotation.set(0, 0, 0)
      dummy.scale.setScalar(0.12 * (1 - life))
      dummy.updateMatrix()
      mesh.setMatrixAt(index, dummy.matrix)
    }
    mesh.instanceMatrix.needsUpdate = true
  })

  if (count === 0) return null

  return (
    <instancedMesh key={count} ref={ref} args={[undefined, undefined, count]} frustumCulled={false}>
      <icosahedronGeometry args={[1, 0]} />
      <meshBasicMaterial color="#ffb040" />
    </instancedMesh>
  )
}

function Crystals({ placements }: { placements: Placement[] }) {
  const ref = useRef<InstancedMesh>(null)

  useLayoutEffect(() => {
    if (ref.current) applyPlacements(ref.current, placements.map(spot => ({ ...spot, lift: 1.3 })))
  }, [placements])

  if (placements.length === 0) return null

  return (
    <instancedMesh key={placements.length} ref={ref} args={[undefined, undefined, placements.length]} castShadow>
      <octahedronGeometry args={[1.3, 0]} />
      <meshStandardMaterial color="#8fe6ff" emissive="#3ab8ff" emissiveIntensity={0.8} roughness={0.1} metalness={0.2} />
    </instancedMesh>
  )
}

function Spores({ placements }: { placements: Placement[] }) {
  const ref = useRef<InstancedMesh>(null)

  useLayoutEffect(() => {
    if (ref.current) applyPlacements(ref.current, placements.map(spot => ({ ...spot, lift: 0.12 })))
  }, [placements])

  if (placements.length === 0) return null

  return (
    <instancedMesh key={placements.length} ref={ref} args={[undefined, undefined, placements.length]}>
      <cylinderGeometry args={[1.6, 1.6, 0.05, 14]} />
      <meshStandardMaterial color="#9ad84a" emissive="#5a9a1a" emissiveIntensity={0.6} transparent opacity={0.55} />
    </instancedMesh>
  )
}

const flowAxis = new Vector3(0, 1, 0)
const flowRotation = new Quaternion()
const flowMatrix = new Matrix4()

const chevronsPerTile = 2

const chevron = (() => {
  const shape = new Shape()
  shape.moveTo(-0.95, -0.55)
  shape.lineTo(0, 0.3)
  shape.lineTo(0.95, -0.55)
  shape.lineTo(0.95, -0.05)
  shape.lineTo(0, 0.8)
  shape.lineTo(-0.95, -0.05)
  shape.closePath()
  return new ShapeGeometry(shape)
})()

function Currents({ placements }: { placements: Placement[] }) {
  const arrows = useRef<InstancedMesh>(null)
  const lanes = useRef<InstancedMesh>(null)

  useLayoutEffect(() => {
    const mesh = lanes.current
    if (!mesh) return
    placements.forEach((placement, index) => {
      dummy.position.set(placement.x, 0.09, placement.z)
      dummy.rotation.set(-Math.PI / 2, 0, placement.rotation ?? 0)
      dummy.scale.set(1, 1, 1)
      dummy.updateMatrix()
      mesh.setMatrixAt(index, dummy.matrix)
    })
    mesh.instanceMatrix.needsUpdate = true
  }, [placements])

  useFrame(({ clock }) => {
    const mesh = arrows.current
    if (!mesh) return

    placements.forEach((placement, index) => {
      const angle = placement.rotation ?? 0
      for (let copy = 0; copy < chevronsPerTile; copy++) {
        const phase = (clock.elapsedTime * 0.9 + copy / chevronsPerTile + hash(placement.x, placement.z) * 0.2) % 1
        const travel = (phase - 0.5) * cell
        const size = Math.sin(phase * Math.PI) * 0.9
        flowRotation.setFromAxisAngle(flowAxis, angle)
        dummy.quaternion.copy(flowRotation)
        dummy.rotateX(Math.PI / 2)
        dummy.position.set(placement.x + Math.sin(angle) * travel, 0.12, placement.z + Math.cos(angle) * travel)
        dummy.scale.set(size, size, size)
        dummy.updateMatrix()
        flowMatrix.copy(dummy.matrix)
        mesh.setMatrixAt(index * chevronsPerTile + copy, flowMatrix)
      }
    })
    mesh.instanceMatrix.needsUpdate = true
  })

  if (placements.length === 0) return null

  return (
    <>
      <instancedMesh key={`lane${placements.length}`} ref={lanes} args={[undefined, undefined, placements.length]} frustumCulled={false}>
        <planeGeometry args={[cell * 0.92, cell * 0.92]} />
        <meshBasicMaterial color="#7fd8ff" transparent opacity={0.16} depthWrite={false} />
      </instancedMesh>
      <instancedMesh key={`flow${placements.length}`} ref={arrows} args={[chevron, undefined, placements.length * chevronsPerTile]} frustumCulled={false}>
        <meshBasicMaterial color="#e8faff" transparent opacity={0.85} side={DoubleSide} depthWrite={false} />
      </instancedMesh>
    </>
  )
}

function Flames({ placements }: { placements: Placement[] }) {
  const ref = useRef<InstancedMesh>(null)

  useFrame(({ clock }) => {
    const mesh = ref.current
    if (!mesh) return

    placements.forEach((placement, index) => {
      const flicker = 0.85 + Math.sin(clock.elapsedTime * 11 + placement.x) * 0.15
      dummy.position.set(placement.x, 3.1, placement.z + 0.45)
      dummy.rotation.set(0, 0, 0)
      dummy.scale.set(flicker, flicker * 1.3, flicker)
      dummy.updateMatrix()
      mesh.setMatrixAt(index, dummy.matrix)
    })
    mesh.instanceMatrix.needsUpdate = true
  })

  if (placements.length === 0) return null

  return (
    <instancedMesh key={placements.length} ref={ref} args={[undefined, undefined, placements.length]} frustumCulled={false}>
      <sphereGeometry args={[0.22, 8, 8]} />
      <meshBasicMaterial color="#ffb040" />
    </instancedMesh>
  )
}
