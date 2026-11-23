import { useFrame, useThree } from '@react-three/fiber'
import { useEffect, useMemo, useRef } from 'react'
import { BoxGeometry, CylinderGeometry, Group, Mesh, MeshStandardMaterial, Object3D } from 'three'
import type { Cue } from './Actor'
import { buildBow, buildSpear } from './creatures'
import { useKitObjects } from './kit'
import { modelUrls } from './models'

interface FirstPersonHandsProps {
  mainHand?: string
  offHand?: string
  twoHanded: boolean
  cue?: Cue
  alive: boolean
  sleeve: string
}

type Motion = 'swing' | 'thrust' | 'shoot' | 'cast' | 'block' | 'hurt'

const motions: Record<string, Motion> = {
  '1H_Melee_Attack_Chop': 'swing',
  '2H_Melee_Attack_Slice': 'swing',
  '2H_Melee_Attack_Chop': 'swing',
  Unarmed_Melee_Attack_Punch_A: 'thrust',
  '1H_Melee_Attack_Stab': 'thrust',
  '2H_Melee_Attack_Stab': 'thrust',
  Dualwield_Melee_Attack_Stab: 'thrust',
  '2H_Ranged_Shoot': 'shoot',
  Spellcast_Shoot: 'cast',
  Spellcast_Raise: 'cast',
  Use_Item: 'cast',
  PickUp: 'cast',
  Interact: 'cast',
  Block_Hit: 'block',
  Hit_A: 'hurt',
}

const durations: Record<Motion, number> = { swing: 340, thrust: 280, shoot: 420, cast: 420, block: 300, hurt: 260 }

const built: Record<string, () => Object3D> = { bow: buildBow, spear: buildSpear }

const rightRest = { x: 0.34, y: -0.36, z: -0.74 }
const leftRest = { x: -0.36, y: -0.38, z: -0.76 }

export function FirstPersonHands({ mainHand, offHand, twoHanded, cue, alive, sleeve }: FirstPersonHandsProps) {
  const { camera } = useThree()
  const weapons = useKitObjects(modelUrls.weapons)
  const rig = useRef<Group>(null)
  const right = useRef<Group>(null)
  const left = useRef<Group>(null)
  const motion = useRef<{ kind: Motion; start: number } | null>(null)
  const lowered = useRef(0)
  const holdsBow = mainHand === 'bow'

  const arm = useMemo(() => {
    const skin = new MeshStandardMaterial({ color: sleeve, roughness: 0.8 })
    const glove = new MeshStandardMaterial({ color: '#5a3a24', roughness: 0.9 })
    return { sleeve: new CylinderGeometry(0.05, 0.06, 0.45, 8), fist: new BoxGeometry(0.09, 0.08, 0.1), skin, glove }
  }, [sleeve])

  const pieces = useMemo(() => {
    const make = (name: string | undefined) => {
      if (!name) return undefined
      const source = weapons[name] ?? built[name]?.()
      if (!source) return undefined
      const weapon = source.clone(true)
      weapon.position.set(0, 0, 0)
      weapon.traverse(child => {
        if (child instanceof Mesh) child.castShadow = false
      })
      return weapon
    }
    return { main: make(mainHand), off: make(offHand) }
  }, [weapons, mainHand, offHand])

  useEffect(() => {
    if (!cue) return
    const kind = motions[cue.clip]
    if (kind) motion.current = { kind, start: performance.now() + cue.delayMs }
  }, [cue])

  useFrame(({ clock }, delta) => {
    const group = rig.current
    if (!group || !right.current || !left.current) return

    group.position.copy(camera.position)
    group.quaternion.copy(camera.quaternion)

    lowered.current += ((alive ? 0 : 1) - lowered.current) * Math.min(1, delta * 4)
    const bob = Math.sin(clock.elapsedTime * 2.2) * 0.012
    const drop = lowered.current * 0.9

    const current = motion.current
    const elapsed = current ? (performance.now() - current.start) / durations[current.kind] : -1
    const active = current && elapsed >= 0 && elapsed <= 1 ? current.kind : undefined
    const phase = active ? Math.sin(Math.min(1, Math.max(0, elapsed)) * Math.PI) : 0
    if (current && elapsed > 1) motion.current = null

    const r = right.current
    const l = left.current
    r.position.set(rightRest.x, rightRest.y + bob - drop, rightRest.z)
    r.rotation.set(0, 0, 0)
    l.position.set(leftRest.x, leftRest.y + bob - drop, leftRest.z)
    l.rotation.set(0, 0, 0)

    switch (active) {
      case 'swing':
        r.rotation.set(-phase * 1.3, 0, phase * 0.6)
        r.position.x -= phase * 0.28
        r.position.y += phase * 0.12
        if (twoHanded) l.position.x += phase * 0.3
        break
      case 'thrust':
        r.position.z -= phase * 0.42
        r.position.x -= phase * 0.1
        if (offHand && !twoHanded) l.position.z -= Math.sin(Math.min(1, Math.max(0, elapsed - 0.25)) * Math.PI) * 0.35
        break
      case 'shoot':
        l.position.z -= phase * 0.12
        r.position.z += phase * 0.32
        break
      case 'cast':
        r.position.y += phase * 0.22
        r.position.z -= phase * 0.25
        l.position.y += phase * 0.22
        l.position.z -= phase * 0.25
        break
      case 'block':
        l.position.x += phase * 0.3
        l.position.y += phase * 0.2
        break
      case 'hurt':
        group.position.addScaledVector(camera.up, -phase * 0.06)
        r.position.y -= phase * 0.08
        l.position.y -= phase * 0.08
        break
    }
  })

  const mainSide = holdsBow ? undefined : pieces.main
  const offSide = holdsBow ? pieces.main : pieces.off

  return (
    <group ref={rig}>
      <group ref={right}>
        <Arm arm={arm} />
        {mainSide && (
          <group position={[0, 0.02, -0.02]} rotation={[-0.55, 0, 0.18]} scale={0.34}>
            <primitive object={mainSide} />
          </group>
        )}
      </group>
      <group ref={left}>
        <Arm arm={arm} />
        {offSide && (
          <group position={[0, 0.02, -0.02]} rotation={holdsBow ? [0, Math.PI / 2, 0] : [-0.5, 0, -0.18]} scale={holdsBow ? 0.34 : 0.3}>
            <primitive object={offSide} />
          </group>
        )}
      </group>
    </group>
  )
}

interface ArmParts {
  sleeve: CylinderGeometry
  fist: BoxGeometry
  skin: MeshStandardMaterial
  glove: MeshStandardMaterial
}

function Arm({ arm }: { arm: ArmParts }) {
  return (
    <>
      <mesh geometry={arm.sleeve} material={arm.skin} position={[0, -0.05, 0.24]} rotation={[Math.PI / 2.3, 0, 0]} />
      <mesh geometry={arm.fist} material={arm.glove} />
    </>
  )
}
