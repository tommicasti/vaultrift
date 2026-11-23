import { Canvas, useFrame } from '@react-three/fiber'
import { Suspense, useMemo, useRef } from 'react'
import { Group } from 'three'
import type { PlayerState } from '../api/types'
import { Actor } from './Actor'
import { heroGear } from './heroLook'
import { heroIdleClip, heroUrl } from './models'

interface HeroPortrait3DProps {
  player: PlayerState
}

export default function HeroPortrait3D({ player }: HeroPortrait3DProps) {
  return (
    <Canvas className="hero-portrait-3d" dpr={[1, 2]} camera={{ fov: 30, position: [0, 2.2, 7.2] }}>
      <hemisphereLight args={['#ffffff', '#3a3040', 1.6]} />
      <directionalLight position={[3, 6, 5]} intensity={1.4} />
      <Suspense fallback={null}>
        <Turntable player={player} />
      </Suspense>
    </Canvas>
  )
}

function Turntable({ player }: HeroPortrait3DProps) {
  const spin = useRef<Group>(null)
  const gear = useMemo(() => heroGear(player.equipment), [player.equipment])

  useFrame(({ camera }, delta) => {
    if (spin.current) spin.current.rotation.y += delta * 0.5
    camera.lookAt(0, 1.15, 0)
  })

  return (
    <group ref={spin}>
      <Actor
        url={heroUrl(player.classId)}
        x={0}
        y={0}
        rotation={0}
        idle={heroIdleClip(gear.mainHand)}
        mainHand={gear.mainHandModel}
        offHand={gear.offHandModel}
        outfit={gear.outfit}
      />
    </group>
  )
}
