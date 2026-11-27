import { useFrame, useThree } from '@react-three/fiber'
import type { RefObject } from 'react'
import { Vector3 } from 'three'
import { cell } from '../scene3d/models'
import type { Tracked } from '../scene3d/Actor'
import { Sprite } from '../components/Sprite'
import { crownSprite, sheets } from '../game/sprites'
import { bossMarkerId, compassSpot } from './compass'
import type { LiveHero, LiveMonster } from './types'

export interface CompassTarget {
  id: number
  track: () => Tracked
}

interface CompassTrackerProps {
  targets: CompassTarget[]
  me: () => Tracked
  markers: RefObject<Map<number, HTMLDivElement>>
}

const point = new Vector3()

export function CompassTracker({ targets, me, markers }: CompassTrackerProps) {
  const { size } = useThree()

  useFrame(({ camera }) => {
    const elements = markers.current
    if (!elements) return
    const mine = me()

    for (const target of targets) {
      const element = elements.get(target.id)
      if (!element) continue
      const spot = target.track()
      point.set(spot.x * cell, 1.6, spot.y * cell).project(camera)
      const placed = compassSpot(point.x, point.y, point.z > 1, size.width, size.height)

      if (!placed.visible) {
        element.style.display = 'none'
        continue
      }

      element.style.display = 'flex'
      element.style.transform = `translate(${placed.x}px, ${placed.y}px) translate(-50%, -50%)`
      const arrow = element.firstElementChild as HTMLElement | null
      if (arrow) arrow.style.transform = `rotate(${placed.angle}rad)`
      const distance = element.lastElementChild as HTMLElement | null
      if (distance) distance.textContent = `${Math.round(Math.hypot(spot.x - mine.x, spot.y - mine.y))} m`
    }
  })

  return null
}

interface PartyMarkersProps {
  heroes: LiveHero[]
  bosses: LiveMonster[]
  markers: RefObject<Map<number, HTMLDivElement>>
}

export function PartyMarkers({ heroes, bosses, markers }: PartyMarkersProps) {
  const register = (id: number) => (element: HTMLDivElement | null) => {
    const map = markers.current
    if (!map) return
    if (element) map.set(id, element)
    else map.delete(id)
  }

  return (
    <div className="party-compass" aria-hidden="true">
      {heroes.map(hero => (
        <div key={hero.id} className={hero.dead ? 'party-marker fallen' : 'party-marker'} ref={register(hero.id)}>
          <span className="party-arrow">➤</span>
          <span className="party-name">{hero.name}</span>
          <span className="party-distance" />
        </div>
      ))}
      {bosses.map(boss => (
        <div key={`boss-${boss.id}`} className="party-marker boss-marker" ref={register(bossMarkerId(boss))}>
          <span className="party-arrow">➤</span>
          <Sprite sheet={sheets.items} index={crownSprite} className="boss-crown" />
          <span className="party-name">{boss.name}</span>
          <span className="party-distance" />
        </div>
      ))}
    </div>
  )
}
