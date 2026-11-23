import { useCallback, useEffect, useState } from 'react'

type LockableOrientation = ScreenOrientation & { lock?: (orientation: 'landscape') => Promise<void> }

const displayQuery = '(display-mode: fullscreen)'

function readFullscreen(): boolean {
  return document.fullscreenElement !== null
}

function readDisplayFullscreen(): boolean {
  return typeof window !== 'undefined' && window.matchMedia?.(displayQuery).matches === true
}

export function useFullscreen() {
  const supported = typeof document !== 'undefined' && document.fullscreenEnabled === true
  const [active, setActive] = useState(() => supported && readFullscreen())
  const [displayed, setDisplayed] = useState(readDisplayFullscreen)

  useEffect(() => {
    const media = window.matchMedia?.(displayQuery)
    if (!media) return

    const update = () => setDisplayed(media.matches)
    media.addEventListener('change', update)
    window.addEventListener('resize', update)
    return () => {
      media.removeEventListener('change', update)
      window.removeEventListener('resize', update)
    }
  }, [])

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

  return { supported, active, immersive: active || displayed, toggle }
}
