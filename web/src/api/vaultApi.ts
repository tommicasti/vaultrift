import type { HeroGearItem } from './accountApi'
import type { Difficulty, EquipmentSlot, EquippedItem, InventoryEntryState, ItemInfo } from './types'

export interface VaultHero {
  id: string
  name: string
  classId: string
  level: number
  played: Difficulty[]
  playSeconds: number
  inWorld: boolean
  updatedAt: string
  gear: HeroGearItem[]
}

export interface VaultWorld {
  id: string
  difficulty: Difficulty
  floor: number
  deepestFloor: number
  floorCount: number
  biome?: string | null
  playSeconds: number
  isOpen: boolean
  createdAt: string
  updatedAt: string
  isConquered: boolean
  graveHeroIds: string[]
}

export interface VaultInfo {
  heroes: VaultHero[]
  worlds: VaultWorld[]
}

export class VaultError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

async function send<T>(method: 'GET' | 'POST' | 'DELETE', path: string, body?: unknown): Promise<T> {
  const response = await fetch(`/api/vault${path}`, {
    method,
    headers: { 'Content-Type': 'application/json' },
    credentials: 'same-origin',
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  if (!response.ok) {
    let detail: string | undefined
    try {
      detail = ((await response.json()) as { detail?: string }).detail
    } catch {
      detail = undefined
    }
    throw new VaultError(response.status, detail ?? `The vault did not answer (error ${response.status}). Try again in a moment.`)
  }

  return (await response.json()) as T
}

export function getVault(): Promise<VaultInfo> {
  return send<VaultInfo>('GET', '')
}

export function createHero(classId: string, name: string): Promise<VaultHero> {
  return send<VaultHero>('POST', '/heroes', { classId, name })
}

export function deleteHero(heroId: string): Promise<VaultInfo> {
  return send<VaultInfo>('DELETE', `/heroes/${heroId}`)
}

export function createWorld(difficulty: Difficulty, classId: string): Promise<VaultWorld> {
  return send<VaultWorld>('POST', '/worlds', { difficulty, classId })
}

export function deleteWorld(worldId: string): Promise<VaultInfo> {
  return send<VaultInfo>('DELETE', `/worlds/${worldId}`)
}

export interface DepositSlot {
  index: number
  item: ItemInfo
  quantity: number
}

export interface DepositInfo {
  capacity: number
  items: DepositSlot[]
  backpack: InventoryEntryState[]
  backpackCapacity: number
  equipment: EquippedItem[]
}

async function depositCall(method: 'GET' | 'POST', path: string): Promise<DepositInfo> {
  const deposit = await send<DepositInfo>(method, path)
  if (!Array.isArray(deposit.equipment)) throw new Error('The server is an older version than the page: restart the server, then reload this page.')
  return deposit
}

export function getDeposit(heroId: string): Promise<DepositInfo> {
  return depositCall('GET', `/deposit/${heroId}`)
}

export function storeInDeposit(heroId: string, entryId: number): Promise<DepositInfo> {
  return depositCall('POST', `/deposit/${heroId}/store/${entryId}`)
}

export function takeFromDeposit(heroId: string, index: number): Promise<DepositInfo> {
  return depositCall('POST', `/deposit/${heroId}/take/${index}`)
}

export function storeEquipped(heroId: string, slot: EquipmentSlot): Promise<DepositInfo> {
  return depositCall('POST', `/deposit/${heroId}/store-equipped/${slot}`)
}

export function equipFromVault(heroId: string, entryId: number): Promise<DepositInfo> {
  return depositCall('POST', `/deposit/${heroId}/equip/${entryId}`)
}

export function unequipInVault(heroId: string, slot: EquipmentSlot): Promise<DepositInfo> {
  return depositCall('POST', `/deposit/${heroId}/unequip/${slot}`)
}
