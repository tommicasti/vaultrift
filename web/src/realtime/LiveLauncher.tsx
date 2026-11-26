import { useEffect, useState } from 'react'
import { getClasses } from '../api/gamesApi'
import type { ClassInfo } from '../api/types'
import type { LiveMode } from './LiveGame'

interface LiveLauncherProps {
  disabled: boolean
  onLaunch: (mode: LiveMode, classId: string) => void
}

const codeLength = 6

export function LiveLauncher({ disabled, onLaunch }: LiveLauncherProps) {
  const [classes, setClasses] = useState<ClassInfo[]>([])
  const [classId, setClassId] = useState<string | null>(null)
  const [code, setCode] = useState('')

  useEffect(() => {
    let active = true
    getClasses()
      .then(list => {
        if (active) setClasses(list)
      })
      .catch(() => undefined)
    return () => {
      active = false
    }
  }, [])

  if (classes.length === 0) return null

  const chosen = classId ?? classes[0].id
  const cleanCode = code.replace(/[^a-z0-9]/gi, '').toUpperCase()

  return (
    <section className="live-launcher">
      <p className="label">Real-time prototype · up to 6 heroes</p>
      <div className="live-classes" role="radiogroup" aria-label="Class">
        {classes.map(heroClass => (
          <button
            key={heroClass.id}
            type="button"
            role="radio"
            aria-checked={heroClass.id === chosen}
            className={heroClass.id === chosen ? 'selected' : undefined}
            disabled={disabled}
            onClick={() => setClassId(heroClass.id)}
          >
            {heroClass.name}
          </button>
        ))}
      </div>
      <div className="live-entry">
        <button className="primary" disabled={disabled} onClick={() => onLaunch({ kind: 'create' }, chosen)}>
          Create a world
        </button>
        <form
          onSubmit={event => {
            event.preventDefault()
            if (cleanCode.length === codeLength) onLaunch({ kind: 'join', code: cleanCode }, chosen)
          }}
        >
          <input
            aria-label="World code"
            placeholder="World code"
            maxLength={codeLength + 2}
            value={code}
            onChange={event => setCode(event.target.value)}
            autoCapitalize="characters"
            spellCheck={false}
          />
          <button type="submit" disabled={disabled || cleanCode.length !== codeLength}>
            Join
          </button>
        </form>
      </div>
    </section>
  )
}
