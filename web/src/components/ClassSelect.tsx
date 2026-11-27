import { useEffect, useState } from 'react'
import { getClasses } from '../api/gamesApi'
import type { ClassInfo } from '../api/types'
import { abilityDescriptions, abilityNames, describeModifier } from '../game/items'
import { randomHeroName } from '../game/heroNames'
import { classSprite } from '../game/sprites'
import { Sprite } from './Sprite'

const maxNameLength = 16
const nameStorageKey = 'vaultrift.heroName'
function loadSavedName(): string {
  try {
    return localStorage.getItem(nameStorageKey) ?? ''
  } catch {
    return ''
  }
}

function saveName(name: string) {
  try {
    localStorage.setItem(nameStorageKey, name)
  } catch {
    return
  }
}

interface ClassSelectProps {
  disabled: boolean
  onChoose: (classId: string, name: string) => void
  onPreview?: (classId: string) => void
}

export function ClassSelect({ disabled, onChoose, onPreview }: ClassSelectProps) {
  const [classes, setClasses] = useState<ClassInfo[] | null>(null)
  const [failed, setFailed] = useState(false)
  const [name, setName] = useState(loadSavedName)

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

  function choose(classId: string) {
    const trimmed = name.trim()
    saveName(trimmed)
    onChoose(classId, trimmed || randomHeroName())
  }

  if (failed) return <p className="error">Could not load the heroes. Is the API running?</p>

  if (!classes) return <p className="label">Loading heroes...</p>

  return (
    <>
      <label className="hero-name-field">
        <span className="label">Name your hero</span>
        <input
          value={name}
          maxLength={maxNameLength}
          placeholder="Leave empty for a random name"
          autoComplete="off"
          spellCheck={false}
          onChange={event => setName(event.target.value)}
        />
      </label>
      <div className="class-select">
        {classes.map(heroClass => {
          const portrait = classSprite(heroClass.id)

          return (
            <article
              key={heroClass.id}
              className="class-card"
              onPointerEnter={() => onPreview?.(heroClass.id)}
              onFocus={() => onPreview?.(heroClass.id)}
            >
              <Sprite sheet={portrait.sheet} index={portrait.index} className="class-portrait" label={heroClass.name} />
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
                <dt>Critical</dt>
                <dd>{heroClass.critical}%</dd>
                <dt>Mana</dt>
                <dd>{heroClass.maxMana}</dd>
              </dl>
              <div className="class-perks">
                <p className="class-mastery">
                  {heroClass.masteryWeapon} mastery: {describeModifier(heroClass.masteryBonus)}
                </p>
                {heroClass.speed === 'Fast' && <p className="class-mastery">Swift: one free step every turn</p>}
                {heroClass.ability && (
                  <p className="class-ability">
                    <strong>
                      {abilityNames[heroClass.ability]} (E, every {heroClass.abilityCooldown} turns):
                    </strong>{' '}
                    {abilityDescriptions[heroClass.ability]}
                  </p>
                )}
              </div>
              <button className="primary" disabled={disabled} onClick={() => choose(heroClass.id)}>
                Choose {heroClass.name}
              </button>
            </article>
          )
        })}
      </div>
    </>
  )
}
