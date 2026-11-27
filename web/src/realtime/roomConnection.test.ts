import { describe, expect, it } from 'vitest'
import { cleanHubMessage, expandMonsters, liveProtocol, reconnectDelays } from './roomConnection'
import type { LiveMonster, LiveSnapshot } from './types'

describe('cleanHubMessage', () => {
  it('keeps only the reason the server gave', () => {
    expect(cleanHubMessage("An unexpected error occurred invoking 'Resume' on the server. HubException: Your place in that world is gone.")).toBe(
      'Your place in that world is gone.',
    )
  })

  it('leaves plain messages alone', () => {
    expect(cleanHubMessage('The connection to the vault was lost.')).toBe('The connection to the vault was lost.')
  })
})

describe('reconnectDelays', () => {
  it('gives up before the server stops keeping the hero', () => {
    expect(reconnectDelays.reduce((total, delay) => total + delay, 0)).toBeLessThan(30000)
  })
})

describe('liveProtocol', () => {
  it('matches the server hub', async () => {
    const { readFileSync } = await import('node:fs')
    const hub = readFileSync(new URL('../../../src/Vaultrift.Api/Realtime/RoomHub.cs', import.meta.url), 'utf8')
    expect(hub).toContain(`public const int Protocol = ${liveProtocol};`)
  })
})

describe('expandMonsters', () => {
  const monster = (id: number, x: number) => ({ id, x }) as LiveMonster
  const snapshot = (monsters: LiveMonster[], full: boolean, gone?: number[]) => ({ monsters, full, gone }) as LiveSnapshot

  it('keeps the monsters a delta leaves out, updates the ones it carries and drops the gone ones', () => {
    const known = new Map<number, LiveMonster>()
    expandMonsters(known, snapshot([monster(1, 1), monster(2, 2), monster(3, 3)], true))
    const after = expandMonsters(known, snapshot([monster(2, 5)], false, [3]))
    expect(after.map(item => [item.id, item.x])).toEqual([
      [1, 1],
      [2, 5],
    ])
  })

  it('starts over on a full snapshot', () => {
    const known = new Map<number, LiveMonster>([[9, monster(9, 9)]])
    expect(expandMonsters(known, snapshot([monster(1, 1)], true)).map(item => item.id)).toEqual([1])
  })
})
