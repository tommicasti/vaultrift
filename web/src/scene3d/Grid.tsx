import { useEffect, useMemo } from 'react'
import { DataTexture, DoubleSide, NearestFilter, RedFormat, ShaderMaterial, UnsignedByteType } from 'three'
import type { Direction, EnemyState } from '../api/types'
import { cell } from './models'

const gridVertex = `
  varying vec2 vPlane;
  void main() {
    vec4 world = modelMatrix * vec4(position, 1.0);
    vPlane = world.xz;
    gl_Position = projectionMatrix * viewMatrix * world;
  }
`

const gridFragment = `
  uniform float cellSize;
  uniform sampler2D floorMask;
  uniform vec2 gridSize;
  varying vec2 vPlane;
  void main() {
    vec2 tile = floor((vPlane + cellSize * 0.5) / cellSize);
    float lit = texture2D(floorMask, (tile + 0.5) / gridSize).r;
    if (lit < 0.5) discard;
    vec2 local = mod(vPlane + cellSize * 0.5, cellSize);
    vec2 edge = min(local, cellSize - local);
    float line = 1.0 - smoothstep(0.03, 0.09, min(edge.x, edge.y));
    gl_FragColor = vec4(0.0, 0.0, 0.0, line * 0.32);
  }
`

interface GridLinesProps {
  columns: number
  rows: number
  lit: (x: number, y: number) => boolean
}

export function GridLines({ columns, rows, lit }: GridLinesProps) {
  const mask = useMemo(() => {
    const data = new Uint8Array(columns * rows)
    for (let y = 0; y < rows; y++) for (let x = 0; x < columns; x++) data[y * columns + x] = lit(x, y) ? 255 : 0
    const texture = new DataTexture(data, columns, rows, RedFormat, UnsignedByteType)
    texture.magFilter = NearestFilter
    texture.minFilter = NearestFilter
    texture.needsUpdate = true
    return texture
  }, [columns, rows, lit])

  useEffect(() => () => mask.dispose(), [mask])

  const material = useMemo(
    () =>
      new ShaderMaterial({
        uniforms: { cellSize: { value: cell }, floorMask: { value: mask }, gridSize: { value: [columns, rows] } },
        vertexShader: gridVertex,
        fragmentShader: gridFragment,
        transparent: true,
        depthWrite: false,
        side: DoubleSide,
      }),
    [mask, columns, rows],
  )

  useEffect(() => () => material.dispose(), [material])

  return (
    <mesh material={material} rotation={[-Math.PI / 2, 0, 0]} position={[((columns - 1) * cell) / 2, 0.07, ((rows - 1) * cell) / 2]}>
      <planeGeometry args={[columns * cell, rows * cell]} />
    </mesh>
  )
}

const steps: Record<Direction, [number, number]> = {
  Up: [0, -1],
  Down: [0, 1],
  Left: [-1, 0],
  Right: [1, 0],
}

interface TargetMarksProps {
  x: number
  y: number
  facing: Direction
  enemies: EnemyState[]
  visible: (x: number, y: number) => boolean
}

export function TargetMarks({ x, y, facing, enemies, visible }: TargetMarksProps) {
  const [dx, dy] = steps[facing]
  const front = { x: x + dx, y: y + dy }
  const threats = enemies.filter(enemy => Math.abs(enemy.x - x) + Math.abs(enemy.y - y) === 1 && visible(enemy.x, enemy.y))
  const frontHasEnemy = threats.some(enemy => enemy.x === front.x && enemy.y === front.y)

  return (
    <>
      {!frontHasEnemy && <Square x={front.x} y={front.y} color="#ffffff" opacity={0.35} />}
      {threats.map(enemy => (
        <Square
          key={enemy.id}
          x={enemy.x}
          y={enemy.y}
          color="#ff3a2a"
          opacity={enemy.x === front.x && enemy.y === front.y ? 0.95 : 0.55}
        />
      ))}
    </>
  )
}

const half = (cell / 2) * Math.SQRT2

function Square({ x, y, color, opacity }: { x: number; y: number; color: string; opacity: number }) {
  return (
    <mesh rotation={[-Math.PI / 2, 0, 0]} position={[x * cell, 0.09, y * cell]}>
      <ringGeometry args={[half * 0.82, half * 0.92, 4, 1, Math.PI / 4]} />
      <meshBasicMaterial color={color} transparent opacity={opacity} depthWrite={false} />
    </mesh>
  )
}
