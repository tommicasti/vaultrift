import { useState, type FormEvent } from 'react'
import type { SavedHero } from '../game/savedHeroes'
import { classSprite } from '../game/sprites'
import { Sprite } from './Sprite'

const codeLength = 9

const difficultyLabels = {
  Normal: 'Normal',
  Hard: 'Hard',
  Hardcore: 'Hardcore',
} as const

interface ResumePanelProps {
  heroes: SavedHero[]
  disabled: boolean
  onContinue: (hero: SavedHero) => void
  onForget: (hero: SavedHero) => void
  onClaimCode: (code: string) => void
}

function heroStatus(hero: SavedHero) {
  if (hero.isLost) return `Fell on floor ${hero.floor}`
  if (hero.isConquered) return 'Conquered the vault'
  return `Floor ${hero.floor}`
}

export function ResumePanel({ heroes, disabled, onContinue, onForget, onClaimCode }: ResumePanelProps) {
  const [code, setCode] = useState('')
  const cleaned = code.trim()

  function submit(event: FormEvent) {
    event.preventDefault()
    if (cleaned.length === 0) return
    onClaimCode(cleaned)
    setCode('')
  }

  return (
    <section className="resume-panel">
      {heroes.length > 0 && (
        <>
          <h2 className="heroes-title">Your heroes</h2>
          <ul className="heroes-list">
            {heroes.map(hero => {
              const portrait = hero.classId ? classSprite(hero.classId) : null
              return (
                <li key={hero.id} className={hero.isLost ? 'resume-card lost' : 'resume-card'}>
                  {portrait && <Sprite sheet={portrait.sheet} index={portrait.index} className="resume-portrait" label={hero.name} />}
                  <div className="resume-details">
                    <strong>{hero.name}</strong>
                    <span className="label">
                      Level {hero.level} · {heroStatus(hero)}
                    </span>
                    <span className="label">
                      <span className={`difficulty-badge difficulty-${hero.difficulty.toLowerCase()}`}>{difficultyLabels[hero.difficulty]}</span>
                    </span>
                  </div>
                  <div className="resume-actions">
                    <button className={hero.isLost ? undefined : 'primary'} disabled={disabled} onClick={() => onContinue(hero)}>
                      {hero.isLost ? 'View' : 'Continue'}
                    </button>
                    <button className="forget-button" disabled={disabled} aria-label={`Forget ${hero.name}`} title="Remove from your account" onClick={() => onForget(hero)}>
                      ×
                    </button>
                  </div>
                </li>
              )
            })}
          </ul>
        </>
      )}
      <form className="code-form" onSubmit={submit}>
        <label className="label" htmlFor="save-code">
          Have a save code from before accounts? Link it to your account
        </label>
        <div className="code-form-row">
          <input
            id="save-code"
            value={code}
            maxLength={codeLength + 2}
            placeholder="XXXX-XXXX"
            autoComplete="off"
            autoCapitalize="characters"
            spellCheck={false}
            onChange={event => setCode(event.target.value.toUpperCase())}
          />
          <button type="submit" disabled={disabled || cleaned.length === 0}>
            Link
          </button>
        </div>
      </form>
    </section>
  )
}
