import { useGLTF } from '@react-three/drei'
import { useMemo } from 'react'
import { BufferGeometry, Float32BufferAttribute, Material, Matrix4, Mesh, Object3D } from 'three'
import { mergeGeometries } from 'three/examples/jsm/utils/BufferGeometryUtils.js'

export interface KitPiece {
  geometry: BufferGeometry
  material: Material
}

export type Kit = Record<string, KitPiece>

export function useKit(url: string): Kit {
  const { scene } = useGLTF(url)

  return useMemo(() => {
    const kit: Kit = {}
    scene.updateMatrixWorld(true)

    for (const holder of scene.children) {
      const piece = flatten(holder)
      if (piece) kit[holder.name] = piece
    }

    return kit
  }, [scene])
}

export function useKitObjects(url: string): Record<string, Object3D> {
  const { scene } = useGLTF(url)

  return useMemo(() => Object.fromEntries(scene.children.map(child => [child.name, child])), [scene])
}

function flatten(holder: Object3D): KitPiece | null {
  const inverse = new Matrix4().copy(holder.matrixWorld).invert()
  const geometries: BufferGeometry[] = []
  let material: Material | null = null

  holder.traverse(child => {
    if (!(child instanceof Mesh)) return

    const geometry = toFloat(child.geometry as BufferGeometry)
    geometry.applyMatrix4(new Matrix4().multiplyMatrices(inverse, child.matrixWorld))
    geometries.push(geometry)
    material ??= Array.isArray(child.material) ? child.material[0] : child.material
  })

  if (geometries.length === 0 || !material) return null

  const geometry = geometries.length === 1 ? geometries[0] : mergeGeometries(geometries.map(normalize))
  return geometry ? { geometry, material } : null
}

function toFloat(source: BufferGeometry): BufferGeometry {
  const geometry = new BufferGeometry()

  for (const name of ['position', 'normal', 'uv']) {
    const attribute = source.getAttribute(name)
    if (!attribute) continue

    const values = new Float32Array(attribute.count * attribute.itemSize)
    for (let index = 0; index < attribute.count; index++)
      for (let component = 0; component < attribute.itemSize; component++)
        values[index * attribute.itemSize + component] = attribute.getComponent(index, component)

    geometry.setAttribute(name, new Float32BufferAttribute(values, attribute.itemSize))
  }

  if (source.index) geometry.setIndex(Array.from(source.index.array))
  return geometry
}

function normalize(geometry: BufferGeometry): BufferGeometry {
  return geometry.index ? geometry.toNonIndexed() : geometry
}
