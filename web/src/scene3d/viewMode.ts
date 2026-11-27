const storageKey = 'vaultrift.view'

export type ViewMode = '2d' | '3d'

export function loadViewMode(): ViewMode {
  try {
    return localStorage.getItem(storageKey) === '3d' ? '3d' : '2d'
  } catch {
    return '2d'
  }
}

export function saveViewMode(mode: ViewMode) {
  try {
    localStorage.setItem(storageKey, mode)
  } catch {
    return
  }
}

const zoomKey = 'vaultrift.zoom3d'

export function loadZoom(fallback: number): number {
  try {
    const saved = Number(localStorage.getItem(zoomKey))
    return Number.isFinite(saved) && saved > 0 ? saved : fallback
  } catch {
    return fallback
  }
}

export function saveZoom(zoom: number) {
  try {
    localStorage.setItem(zoomKey, zoom.toFixed(3))
  } catch {
    return
  }
}

export type CameraMode = 'third' | 'first'

const cameraKey = 'vaultrift.camera3d'

export function loadCameraMode(): CameraMode {
  try {
    return localStorage.getItem(cameraKey) === 'first' ? 'first' : 'third'
  } catch {
    return 'third'
  }
}

export function saveCameraMode(mode: CameraMode) {
  try {
    localStorage.setItem(cameraKey, mode)
  } catch {
    return
  }
}

export function detectLowPower(): boolean {
  if (typeof window === 'undefined') return false
  const coarse = window.matchMedia?.('(pointer: coarse)').matches ?? false
  const cores = navigator.hardwareConcurrency ?? 8
  return coarse || cores <= 4
}

export function supportsWebGL(): boolean {
  if (typeof document === 'undefined') return true
  try {
    const canvas = document.createElement('canvas')
    return Boolean(canvas.getContext('webgl2') ?? canvas.getContext('webgl'))
  } catch {
    return false
  }
}

export function canTouchLook(): boolean {
  return typeof window !== 'undefined' && (window.matchMedia?.('(pointer: coarse)').matches ?? false)
}

export function canMouseLook(): boolean {
  if (typeof document === 'undefined') return false
  return 'pointerLockElement' in document && (window.matchMedia?.('(pointer: fine)').matches ?? false)
}
