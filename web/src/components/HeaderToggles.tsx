import { useState } from 'react'
import { hapticsEnabled, hapticsSupported, setHapticsEnabled, tap } from '../game/haptics'
import { useFullscreen } from '../game/useFullscreen'

export function HeaderToggles() {
  const fullscreen = useFullscreen()
  const [vibration, setVibration] = useState(hapticsEnabled)
  const touch = typeof window !== 'undefined' && window.matchMedia('(pointer: coarse)').matches

  function toggleVibration() {
    const next = !vibration
    setHapticsEnabled(next)
    setVibration(next)
    if (next) tap()
  }

  if (!fullscreen.supported && !(touch && hapticsSupported())) return null

  return (
    <span className="header-toggles">
      {touch && hapticsSupported() && (
        <button
          className={vibration ? 'icon-toggle on' : 'icon-toggle'}
          aria-pressed={vibration}
          title={vibration ? 'Vibration on' : 'Vibration off'}
          onClick={toggleVibration}
        >
          <span aria-hidden="true">{vibration ? '📳' : '📴'}</span>
          <span className="visually-hidden">Vibration</span>
        </button>
      )}
      {fullscreen.supported && (
        <button
          className="icon-toggle"
          aria-pressed={fullscreen.active}
          title={fullscreen.active ? 'Exit full screen' : 'Full screen'}
          onClick={() => void fullscreen.toggle()}
        >
          <span aria-hidden="true">{fullscreen.active ? '🗗' : '⛶'}</span>
          <span className="visually-hidden">{fullscreen.active ? 'Exit full screen' : 'Full screen'}</span>
        </button>
      )}
    </span>
  )
}
