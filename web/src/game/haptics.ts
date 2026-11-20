const storageKey = 'vaultrift.haptics'
const tapMs = 10

let enabled = readSetting()
let iosSwitch: HTMLLabelElement | null = null

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

  if (typeof navigator.vibrate === 'function') {
    navigator.vibrate(tapMs)
    return
  }

  if (isAppleTouchDevice()) appleSwitch().click()
}

function isAppleTouchDevice(): boolean {
  return /iPhone|iPad|iPod/.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)
}

function appleSwitch(): HTMLLabelElement {
  if (iosSwitch) return iosSwitch

  const label = document.createElement('label')
  const input = document.createElement('input')
  input.type = 'checkbox'
  input.setAttribute('switch', '')
  label.append(input)
  label.setAttribute('aria-hidden', 'true')
  label.style.cssText = 'position:fixed;width:1px;height:1px;overflow:hidden;opacity:0;pointer-events:none;left:-10px;top:-10px'
  document.body.append(label)
  iosSwitch = label
  return label
}
