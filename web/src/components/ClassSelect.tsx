import { useEffect, useState } from 'react'
import { getClasses } from '../api/gamesApi'
import type { ClassInfo, Difficulty } from '../api/types'
import { abilityDescriptions, abilityNames, describeModifier } from '../game/items'
import { classSprite } from '../game/sprites'
import { Sprite } from './Sprite'

const maxNameLength = 16
const nameStorageKey = 'vaultrift.heroName'
const difficultyStorageKey = 'vaultrift.difficulty'

const difficulties: { id: Difficulty; name: string; description: string }[] = [
  { id: 'Normal', name: 'Normal', description: 'Fall and you rise at the start of the floor, a little poorer.' },
  { id: 'Hard', name: 'Hard', description: 'Monsters are two levels stronger. Fall and your gear stays in a grave: walk over it to take it back.' },
  { id: 'Hardcore', name: 'Hardcore', description: 'Hard, and you only get one life. Fall once and the run is over.' },
]

function loadDifficulty(): Difficulty {
  try {
    const saved = localStorage.getItem(difficultyStorageKey)
    return difficulties.find(option => option.id === saved)?.id ?? 'Normal'
  } catch {
    return 'Normal'
  }
}

function saveDifficulty(difficulty: Difficulty) {
  try {
    localStorage.setItem(difficultyStorageKey, difficulty)
  } catch {
    return
  }
}

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
  onChoose: (classId: string, name: string, difficulty: Difficulty) => void
}

export function ClassSelect({ disabled, onChoose }: ClassSelectProps) {
  const [classes, setClasses] = useState<ClassInfo[] | null>(null)
  const [failed, setFailed] = useState(false)
  const [name, setName] = useState(loadSavedName)
  const [difficulty, setDifficulty] = useState<Difficulty>(loadDifficulty)

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
    saveDifficulty(difficulty)
    onChoose(classId, trimmed, difficulty)
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
          placeholder="Leave empty to use the class name"
          autoComplete="off"
          spellCheck={false}
          onChange={event => setName(event.target.value)}
        />
      </label>
      <fieldset className="difficulty-picker">
        <legend className="label">Difficulty</legend>
        {difficulties.map(option => (
          <label key={option.id} className={option.id === difficulty ? `difficulty-option active difficulty-${option.id.toLowerCase()}` : `difficulty-option difficulty-${option.id.toLowerCase()}`}>
            <input
              type="radio"
              name="difficulty"
              value={option.id}
              checked={option.id === difficulty}
              onChange={() => setDifficulty(option.id)}
            />
            <strong>{option.name}</strong>
            <span>{option.description}</span>
          </label>
        ))}
      </fieldset>
      <div className="class-select">
        {classes.map(heroClass => {
          const portrait = classSprite(heroClass.id)

          return (
            <article key={heroClass.id} className="class-card">
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
