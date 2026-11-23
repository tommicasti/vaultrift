const storageKey = 'vaultrift.haptics'
const tapMs = 30

let enabled = readSetting()

function readSetting(): boolean {
  try {
    return localStorage.getItem(storageKey) !== 'off'
  } catch {
    return true
  }
}

export function hapticsEnabled(): boolean {
  return enabled
}

export function setHapticsEnabled(value: boolean) {
  enabled = value

  try {
    localStorage.setItem(storageKey, value ? 'on' : 'off')
  } catch {
    return
  }
}

export function hapticsSupported(): boolean {
  return typeof navigator.vibrate === 'function' || isAppleTouchDevice()
}

export function tap() {
  if (!enabled) return

  try {
    if (typeof navigator.vibrate === 'function' && navigator.vibrate(tapMs)) return
    if (isAppleTouchDevice()) flipAppleSwitch()
  } catch {
    return
  }
}

function isAppleTouchDevice(): boolean {
  return /iPhone|iPad|iPod/.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)
}

function flipAppleSwitch() {
  const label = document.createElement('label')
  const input = document.createElement('input')
  input.type = 'checkbox'
  input.setAttribute('switch', '')
  label.append(input)
  label.setAttribute('aria-hidden', 'true')
  label.style.display = 'none'
  document.head.append(label)
  label.click()
  label.remove()
}
