import { useSyncExternalStore } from 'react'

const compactLandscapeQuery = '(orientation: landscape) and (max-height: 500px)'

function subscribe(onChange: () => void) {
  const media = window.matchMedia(compactLandscapeQuery)
  media.addEventListener('change', onChange)
  return () => media.removeEventListener('change', onChange)
}

function getSnapshot() {
  return window.matchMedia(compactLandscapeQuery).matches
}

export function useCompactLandscape(): boolean {
  return useSyncExternalStore(subscribe, getSnapshot)
}