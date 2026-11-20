import { useCallback, useEffect, useState } from 'react'

type LockableOrientation = ScreenOrientation & { lock?: (orientation: 'landscape') => Promise<void> }

function readFullscreen(): boolean {
  return document.fullscreenElement !== null
}

export function useFullscreen() {
  const supported = typeof document !== 'undefined' && document.fullscreenEnabled === true
  const [active, setActive] = useState(() => supported && readFullscreen())

  useEffect(() => {
    if (!supported) return

    const update = () => setActive(readFullscreen())
    document.addEventListener('fullscreenchange', update)
    return () => document.removeEventListener('fullscreenchange', update)
  }, [supported])

  const toggle = useCallback(async () => {
    if (!supported) return

    try {
      if (readFullscreen()) {
        await document.exitFullscreen()
        return
      }

      await document.documentElement.requestFullscreen({ navigationUI: 'hide' })

      if (window.matchMedia('(pointer: coarse)').matches)
        await (screen.orientation as LockableOrientation).lock?.('landscape').catch(() => undefined)
    } catch {
      setActive(readFullscreen())
    }
  }, [supported])

  return { supported, active, toggle }
}
