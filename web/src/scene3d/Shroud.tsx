import { useLayoutEffect, useMemo, useRef } from 'react'
import { InstancedMesh, Object3D } from 'three'
import { cell } from './models'

interface ShroudProps {
  map: string[]
  explored: ReadonlySet<string>
  color: string
  focusY: number
}

const height = 4.4
const loweredHeight = 1.3

const neighbours = [
  [0, -1],
  [0, 1],
  [-1, 0],
  [1, 0],
  [-1, -1],
  [1, -1],
  [-1, 1],
  [1, 1],
]

function isOpen(map: string[], x: number, y: number): boolean {
  const symbol = map[y]?.[x]
  return symbol !== undefined && symbol !== '#'
}

export function Shroud({ map, explored, color, focusY }: ShroudProps) {
  const cells = useMemo(() => {
    const dark: [number, number][] = []

    map.forEach((row, y) => {
      for (let x = 0; x < row.length; x++) {
        const open = isOpen(map, x, y)
        const bordersOpen = neighbours.some(([dx, dy]) => isOpen(map, x + dx, y + dy))
        if (!open && !bordersOpen) continue

        const remembered = open
          ? explored.has(`${x},${y}`)
          : neighbours.some(([dx, dy]) => explored.has(`${x + dx},${y + dy}`))
        if (!remembered) dark.push([x, y])
      }
    })

    return dark
  }, [map, explored])

  return (
    <Blocks cells={cells} color={color} focusY={focusY} />
  )
}

const dummy = new Object3D()

function Blocks({ cells, color, focusY }: { cells: [number, number][]; color: string; focusY: number }) {
  const ref = useRef<InstancedMesh>(null)
  const capacity = Math.max(64, Math.ceil(cells.length / 256) * 256)

  useLayoutEffect(() => {
    const mesh = ref.current
    if (!mesh) return

    cells.forEach(([x, y], index) => {
      const blockHeight = y > focusY ? loweredHeight : height
      dummy.position.set(x * cell, blockHeight / 2 - 0.05, y * cell)
      dummy.scale.set(1, blockHeight / height, 1)
      dummy.updateMatrix()
      mesh.setMatrixAt(index, dummy.matrix)
    })
    mesh.count = cells.length
    mesh.instanceMatrix.needsUpdate = true
    mesh.computeBoundingSphere()
  }, [cells, focusY])

  return (
    <instancedMesh key={capacity} ref={ref} args={[undefined, undefined, capacity]} frustumCulled={false}>
      <boxGeometry args={[cell + 0.02, height, cell + 0.02]} />
      <meshBasicMaterial color={color} fog={false} />
    </instancedMesh>
  )
}
