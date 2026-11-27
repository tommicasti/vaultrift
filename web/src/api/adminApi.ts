import type { EquipmentSlot, Rarity } from './types'

export interface ReportItem {
  name: string
  rarity: Rarity
  quantity: number
  slot?: EquipmentSlot | null
}

export interface ReportTrophy {
  name: string
  description: string
  unlockedAt: string
}

export interface ReportHero {
  name: string
  className: string
  level: number
  coins: number
  playSeconds: number
  falls: number
  online: boolean
  updatedAt: string
  equipment: ReportItem[]
  backpackItems: number
}

export interface ReportWorld {
  className: string
  difficulty: string
  biome?: string | null
  floor: number
  deepestFloor: number
  conquered: boolean
  open: boolean
  playSeconds: number
  updatedAt: string
}

export interface ReportTurnHero {
  name: string
  className: string
  level: number
  floor: number
  difficulty: string
  turns: number
  coins: number
  deaths: number
  playSeconds: number
  lost: boolean
  conquered: boolean
  updatedAt: string
}

export interface PlayerReport {
  userName: string
  joinedAt: string
  lastSeen?: string | null
  online: boolean
  playSeconds: number
  trophies: ReportTrophy[]
  heroes: ReportHero[]
  worlds: ReportWorld[]
  deposit: { items: number; treasures: ReportItem[] }
  turnHeroes: ReportTurnHero[]
}

export interface PlayersReport {
  generatedAt: string
  players: PlayerReport[]
}

export async function getPlayers(): Promise<PlayersReport> {
  const response = await fetch('/api/admin/players', { credentials: 'same-origin' })
  if (!response.ok) throw new Error(response.status === 404 ? 'This page is only for the vault keepers.' : 'The vault did not answer. Try again.')
  return (await response.json()) as PlayersReport
}
