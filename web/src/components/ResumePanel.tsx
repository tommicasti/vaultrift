import { useState, type FormEvent } from 'react'
import type { SavedGame } from '../game/savedGame'
import { classSprite } from '../game/sprites'
import { Sprite } from './Sprite'

const codeLength = 9

interface ResumePanelProps {
  saved: SavedGame | null
  disabled: boolean
  onContinue: (saved: SavedGame) => void
  onLoadCode: (code: string) => void
}

export function ResumePanel({ saved, disabled, onContinue, onLoadCode }: ResumePanelProps) {
  const [code, setCode] = useState('')
  const portrait = saved?.classId ? classSprite(saved.classId) : null
  const cleaned = code.trim()

  function submit(event: FormEvent) {
    event.preventDefault()
    if (cleaned.length > 0) onLoadCode(cleaned)
  }

  return (
    <section className="resume-panel">
      {saved && (
        <div className="resume-card">
          {portrait && <Sprite sheet={portrait.sheet} index={portrait.index} className="resume-portrait" label={saved.name} />}
          <div className="resume-details">
            <strong>{saved.name}</strong>
            <span className="label">
              Level {saved.level} · Floor {saved.floor}
            </span>
            <span className="label">Save code {saved.code}</span>
          </div>
          <button className="primary" disabled={disabled} onClick={() => onContinue(saved)}>
            Continue
          </button>
        </div>
      )}
      <form className="code-form" onSubmit={submit}>
        <label className="label" htmlFor="save-code">
          Playing on another device? Enter your save code
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
            Load
          </button>
        </div>
      </form>
    </section>
  )
}
