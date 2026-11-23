import { useLayoutEffect, useMemo, useRef } from 'react'
import { InstancedMesh, Object3D } from 'three'
import { cell } from './models'

type PartShape = 'cone' | 'cylinder' | 'sphere' | 'box' | 'octa'

interface Part {
  shape: PartShape
  color: string
  at: [number, number, number]
  size: [number, number, number]
  glow?: boolean
}

type DecorType = Part[]

const bones: DecorType = [
  { shape: 'cylinder', color: '#ece4d0', at: [0, 0.08, 0], size: [0.07, 0.9, 0.07] },
  { shape: 'sphere', color: '#ece4d0', at: [0.35, 0.18, 0.1], size: [0.2, 0.18, 0.2] },
]
const candle: DecorType = [
  { shape: 'cylinder', color: '#efe6c8', at: [0, 0.25, 0], size: [0.1, 0.5, 0.1] },
  { shape: 'sphere', color: '#ffb040', at: [0, 0.56, 0], size: [0.07, 0.11, 0.07], glow: true },
]
const grass: DecorType = [
  { shape: 'cone', color: '#5a8a3a', at: [0, 0.25, 0], size: [0.12, 0.5, 0.12] },
  { shape: 'cone', color: '#6a9a40', at: [0.16, 0.2, 0.08], size: [0.1, 0.4, 0.1] },
  { shape: 'cone', color: '#4a7a30', at: [-0.14, 0.18, -0.06], size: [0.1, 0.36, 0.1] },
]
const toadstool: DecorType = [
  { shape: 'cylinder', color: '#efe6d0', at: [0, 0.2, 0], size: [0.09, 0.4, 0.09] },
  { shape: 'sphere', color: '#d8402a', at: [0, 0.42, 0], size: [0.3, 0.16, 0.3] },
]
const cactus: DecorType = [
  { shape: 'cylinder', color: '#4a8a4a', at: [0, 0.7, 0], size: [0.22, 1.4, 0.22] },
  { shape: 'cylinder', color: '#4a8a4a', at: [0.26, 0.75, 0], size: [0.12, 0.5, 0.12] },
  { shape: 'cylinder', color: '#4a8a4a', at: [-0.24, 0.95, 0], size: [0.11, 0.45, 0.11] },
]
const sandstone: DecorType = [{ shape: 'sphere', color: '#d8b47a', at: [0, 0.12, 0], size: [0.5, 0.25, 0.4] }]
const snowdrift: DecorType = [{ shape: 'sphere', color: '#f4f8ff', at: [0, 0.05, 0], size: [0.7, 0.3, 0.5] }]
const iceSpike: DecorType = [
  { shape: 'cone', color: '#a8dcff', at: [0, 0.45, 0], size: [0.18, 0.9, 0.18], glow: true },
  { shape: 'cone', color: '#c8ecff', at: [0.2, 0.3, 0.1], size: [0.12, 0.6, 0.12] },
]
const urn: DecorType = [
  { shape: 'sphere', color: '#8a5a3a', at: [0, 0.32, 0], size: [0.28, 0.34, 0.28] },
  { shape: 'cylinder', color: '#7a4a2a', at: [0, 0.66, 0], size: [0.14, 0.14, 0.14] },
]
const reeds: DecorType = [
  { shape: 'cone', color: '#3a5a2a', at: [0, 0.5, 0], size: [0.06, 1, 0.06] },
  { shape: 'cone', color: '#4a6a2a', at: [0.14, 0.4, 0.06], size: [0.05, 0.8, 0.05] },
  { shape: 'cylinder', color: '#6a4a2a', at: [0, 0.95, 0], size: [0.06, 0.18, 0.06] },
]
const lily: DecorType = [{ shape: 'sphere', color: '#4a7a3a', at: [0, 0.04, 0], size: [0.5, 0.08, 0.5] }]
const magmaRock: DecorType = [
  { shape: 'octa', color: '#3a2a28', at: [0, 0.25, 0], size: [0.4, 0.3, 0.35] },
  { shape: 'octa', color: '#ff6a1a', at: [0.15, 0.4, 0.1], size: [0.1, 0.1, 0.1], glow: true },
]
const brokenColumn: DecorType = [
  { shape: 'cylinder', color: '#a8b0b0', at: [0, 0.35, 0], size: [0.3, 0.7, 0.3] },
  { shape: 'sphere', color: '#5a8a4a', at: [0.1, 0.72, 0], size: [0.3, 0.12, 0.3] },
]
const crystalCluster: DecorType = [
  { shape: 'octa', color: '#7ae8ff', at: [0, 0.45, 0], size: [0.18, 0.5, 0.18], glow: true },
  { shape: 'octa', color: '#9af0ff', at: [0.2, 0.3, 0.05], size: [0.12, 0.32, 0.12], glow: true },
]
const oreRock: DecorType = [
  { shape: 'octa', color: '#6a6470', at: [0, 0.22, 0], size: [0.38, 0.28, 0.34] },
  { shape: 'octa', color: '#f2c23a', at: [0.12, 0.38, 0.1], size: [0.08, 0.08, 0.08], glow: true },
]
const glowShroom: DecorType = [
  { shape: 'cylinder', color: '#e8e0f0', at: [0, 0.45, 0], size: [0.14, 0.9, 0.14] },
  { shape: 'sphere', color: '#b06aff', at: [0, 0.92, 0], size: [0.55, 0.25, 0.55], glow: true },
]
const cloud: DecorType = [
  { shape: 'sphere', color: '#ffffff', at: [0, 0.3, 0], size: [0.4, 0.3, 0.35] },
  { shape: 'sphere', color: '#f4f8ff', at: [0.32, 0.24, 0.05], size: [0.3, 0.24, 0.28] },
  { shape: 'sphere', color: '#f4f8ff', at: [-0.3, 0.22, -0.04], size: [0.28, 0.22, 0.26] },
]
const riftShard: DecorType = [
  { shape: 'octa', color: '#b06aff', at: [0, 0.7, 0], size: [0.2, 0.6, 0.2], glow: true },
  { shape: 'octa', color: '#7a3aff', at: [0.25, 0.3, 0.1], size: [0.1, 0.25, 0.1], glow: true },
]

const cobweb: DecorType = [{ shape: 'octa', color: '#e8e8f0', at: [0, 1.6, 0], size: [0.5, 0.5, 0.04] }]
const bush: DecorType = [
  { shape: 'sphere', color: '#4a7a34', at: [0, 0.3, 0], size: [0.45, 0.35, 0.4] },
  { shape: 'sphere', color: '#5a8a3a', at: [0.25, 0.22, 0.1], size: [0.3, 0.25, 0.3] },
]
const skull: DecorType = [
  { shape: 'sphere', color: '#ece4d0', at: [0, 0.16, 0], size: [0.2, 0.18, 0.22] },
  { shape: 'box', color: '#ece4d0', at: [0, 0.06, 0.1], size: [0.16, 0.08, 0.1] },
]
const pine: DecorType = [
  { shape: 'cylinder', color: '#6a4a2a', at: [0, 0.2, 0], size: [0.08, 0.4, 0.08] },
  { shape: 'cone', color: '#3a6a5a', at: [0, 0.75, 0], size: [0.4, 0.8, 0.4] },
  { shape: 'cone', color: '#f4f8ff', at: [0, 1.05, 0], size: [0.22, 0.35, 0.22] },
]
const armorStand: DecorType = [
  { shape: 'cylinder', color: '#5a4a3a', at: [0, 0.5, 0], size: [0.05, 1, 0.05] },
  { shape: 'sphere', color: '#a8acb4', at: [0, 1.05, 0], size: [0.3, 0.32, 0.22] },
  { shape: 'sphere', color: '#a8acb4', at: [0, 1.42, 0], size: [0.14, 0.14, 0.14] },
]
const obsidian: DecorType = [
  { shape: 'cone', color: '#1a1420', at: [0, 0.45, 0], size: [0.2, 0.9, 0.2] },
  { shape: 'cone', color: '#2a1a28', at: [0.2, 0.3, 0.1], size: [0.14, 0.6, 0.14] },
]
const rubble: DecorType = [
  { shape: 'octa', color: '#8a9090', at: [0, 0.12, 0], size: [0.25, 0.18, 0.22] },
  { shape: 'octa', color: '#7a8080', at: [0.28, 0.08, 0.12], size: [0.16, 0.12, 0.15] },
]
const crate: DecorType = [
  { shape: 'box', color: '#8a6a3a', at: [0, 0.3, 0], size: [0.6, 0.6, 0.6] },
  { shape: 'sphere', color: '#ffc060', at: [0, 0.75, 0], size: [0.1, 0.14, 0.1], glow: true },
]
const tinyShrooms: DecorType = [
  { shape: 'sphere', color: '#7ae0ff', at: [0, 0.18, 0], size: [0.16, 0.08, 0.16], glow: true },
  { shape: 'sphere', color: '#ff8ae0', at: [0.2, 0.12, 0.1], size: [0.12, 0.06, 0.12], glow: true },
  { shape: 'cylinder', color: '#efe6d0', at: [0, 0.08, 0], size: [0.03, 0.16, 0.03] },
]
const skyCrystal: DecorType = [{ shape: 'octa', color: '#bfe8ff', at: [0, 1.4, 0], size: [0.18, 0.32, 0.18], glow: true }]
const voidOrb: DecorType = [{ shape: 'sphere', color: '#2a0a4a', at: [0, 1.2, 0], size: [0.28, 0.28, 0.28] }]

const biomeDecor: Record<string, DecorType[]> = {
  crypt: [bones, candle, cobweb, skull],
  forest: [grass, toadstool, bush],
  desert: [cactus, sandstone, skull],
  snow: [snowdrift, iceSpike, pine],
  castle: [urn, candle, armorStand],
  swamp: [reeds, lily, toadstool],
  volcano: [magmaRock, obsidian],
  ruins: [brokenColumn, grass, rubble],
  mines: [crystalCluster, oreRock, crate],
  fungal: [glowShroom, toadstool, tinyShrooms],
  sky: [cloud, skyCrystal],
  rift: [riftShard, voidOrb],
}

interface DecorProps {
  map: string[]
  biome: string | undefined
  blocked: ReadonlySet<string>
}

interface Spot {
  x: number
  z: number
  rotation: number
  scale: number
}

const sides = [
  [0, -1],
  [0, 1],
  [-1, 0],
  [1, 0],
]

function hash(x: number, y: number, salt: number): number {
  let value = (x * 73856093) ^ (y * 19349663) ^ (salt * 83492791)
  value = Math.imul(value ^ (value >>> 13), 1274126177)
  return ((value ^ (value >>> 16)) >>> 0) / 4294967295
}

export function Decor({ map, biome, blocked }: DecorProps) {
  const types = biomeDecor[biome ?? ''] ?? biomeDecor.crypt

  const spots = useMemo(() => {
    const byType: Spot[][] = types.map(() => [])
    map.forEach((row, y) => {
      for (let x = 0; x < row.length; x++) {
        if (row[x] !== '.' || blocked.has(`${x},${y}`) || hash(x, y, 1) > 0.3) continue

        const wall = sides.find(([dx, dy]) => (map[y + dy]?.[x + dx] ?? '#') === '#')
        if (!wall) continue

        const type = Math.floor(hash(x, y, 2) * types.length)
        byType[type].push({
          x: (x + wall[0] * 0.33) * cell,
          z: (y + wall[1] * 0.33) * cell,
          rotation: hash(x, y, 3) * Math.PI * 2,
          scale: 1.4 + hash(x, y, 4) * 0.8,
        })
      }
    })
    return byType
  }, [map, blocked, types])

  return (
    <>
      {types.map((type, typeIndex) =>
        type.map((part, partIndex) => <PartInstances key={`${typeIndex}-${partIndex}`} part={part} spots={spots[typeIndex]} />),
      )}
    </>
  )
}

const dummy = new Object3D()
const anchor = new Object3D()

function PartInstances({ part, spots }: { part: Part; spots: Spot[] }) {
  const ref = useRef<InstancedMesh>(null)

  useLayoutEffect(() => {
    const mesh = ref.current
    if (!mesh) return

    spots.forEach((spot, index) => {
      anchor.position.set(spot.x, 0, spot.z)
      anchor.rotation.set(0, spot.rotation, 0)
      anchor.scale.setScalar(spot.scale)
      anchor.updateMatrix()
      dummy.position.set(...part.at)
      dummy.rotation.set(0, 0, 0)
      dummy.scale.set(...part.size)
      dummy.updateMatrix()
      mesh.setMatrixAt(index, anchor.matrix.clone().multiply(dummy.matrix))
    })
    mesh.instanceMatrix.needsUpdate = true
    mesh.computeBoundingSphere()
  }, [spots, part])

  if (spots.length === 0) return null

  return (
    <instancedMesh key={spots.length} ref={ref} args={[undefined, undefined, spots.length]} castShadow receiveShadow>
      {part.shape === 'cone' && <coneGeometry args={[1, 1, 6]} />}
      {part.shape === 'cylinder' && <cylinderGeometry args={[1, 1, 1, 7]} />}
      {part.shape === 'sphere' && <icosahedronGeometry args={[1, 1]} />}
      {part.shape === 'box' && <boxGeometry args={[1, 1, 1]} />}
      {part.shape === 'octa' && <octahedronGeometry args={[1, 0]} />}
      <meshStandardMaterial
        color={part.color}
        flatShading
        roughness={0.8}
        emissive={part.glow ? part.color : '#000000'}
        emissiveIntensity={part.glow ? 0.9 : 0}
      />
    </instancedMesh>
  )
}
