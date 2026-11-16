import { useEffect, useState } from 'react'
import { getClasses } from '../api/gamesApi'
import type { ClassInfo } from '../api/types'
import { describeModifier } from '../game/items'
import { classSprite, sheets } from '../game/sprites'
import { Sprite } from './Sprite'

interface ClassSelectProps {
  disabled: boolean
  onChoose: (classId: string) => void
}

export function ClassSelect({ disabled, onChoose }: ClassSelectProps) {
  const [classes, setClasses] = useState<ClassInfo[] | null>(null)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    let ignore = false

    getClasses()
      .then(result => {
        if (!ignore) setClasses(result)
      })
      .catch(() => {
        if (!ignore) setFailed(true)
      })

    return () => {
      ignore = true
    }
  }, [])

  if (failed) return <p className="error">Could not load the heroes. Is the API running?</p>

  if (!classes) return <p className="label">Loading heroes...</p>

  return (
    <div className="class-select">
      {classes.map(heroClass => (
        <article key={heroClass.id} className="class-card">
          <Sprite sheet={sheets.dungeon} index={classSprite(heroClass.id)} className="class-portrait" label={heroClass.name} />
          <h2>{heroClass.name}</h2>
          <p className="label">{heroClass.description}</p>
          <dl className="combat-stats">
            <dt>Health</dt>
            <dd>{heroClass.maxHealth}</dd>
            <dt>Attack</dt>
            <dd>{heroClass.attack}</dd>
            <dt>Defense</dt>
            <dd>{heroClass.defense}</dd>
            <dt>Evasion</dt>
            <dd>{heroClass.evasion}%</dd>
          </dl>
          <p className="class-mastery">
            {heroClass.masteryWeapon} mastery: {describeModifier(heroClass.masteryBonus)}
          </p>
          <button className="primary" disabled={disabled} onClick={() => onChoose(heroClass.id)}>
            Choose {heroClass.name}
          </button>
        </article>
      ))}
    </div>
  )
}