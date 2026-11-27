import type { HeroGearItem, HeroSummary } from '../api/accountApi'
import type { Difficulty } from '../api/types'

const storageKey = 'vaultrift.heroes'
const legacyKey = 'vaultrift.savedGame'

export interface SavedHero {
  id: string
  code: string
  name: string
  classId: string | null
  floor: number
  level: number
  difficulty: Difficulty
  isLost: boolean
  isConquered: boolean
  playedAt: number
  gear: HeroGearItem[]
}

const difficulties: Difficulty[] = ['Normal', 'Hard', 'Hardcore']

function parse(value: unknown, fallbackTime: number): SavedHero | null {
  if (typeof value !== 'object' || value === null) return null

  const hero = value as Partial<SavedHero>
  if (typeof hero.id !== 'string' || typeof hero.code !== 'string') return null

  return {
    id: hero.id,
    code: hero.code,
    name: hero.name ?? 'Hero',
    classId: hero.classId ?? null,
    floor: hero.floor ?? 1,
    level: hero.level ?? 1,
    difficulty: difficulties.find(option => option === hero.difficulty) ?? 'Normal',
    isLost: hero.isLost ?? false,
    isConquered: hero.isConquered ?? false,
    playedAt: hero.playedAt ?? fallbackTime,
    gear: [],
  }
}

function read(key: string): unknown {
  const raw = localStorage.getItem(key)
  return raw ? JSON.parse(raw) : null
}

export function localSaveCodes(): string[] {
  try {
    const stored = read(storageKey)
    const legacy = parse(read(legacyKey), 0)
    const heroes = Array.isArray(stored) ? stored.map((value: unknown) => parse(value, 0)) : []
    return [...heroes, legacy].filter((hero): hero is SavedHero => hero !== null).map(hero => hero.code)
  } catch {
    return []
  }
}

export function clearLocalHeroes() {
  try {
    localStorage.removeItem(storageKey)
    localStorage.removeItem(legacyKey)
  } catch {
    return
  }
}

export function heroFromSummary(summary: HeroSummary): SavedHero {
  return {
    id: summary.id,
    code: summary.code,
    name: summary.name,
    classId: summary.classId,
    floor: summary.floor,
    level: summary.level,
    difficulty: difficulties.find(option => option === summary.difficulty) ?? 'Normal',
    isLost: summary.isLost,
    isConquered: summary.isConquered,
    playedAt: Date.parse(summary.updatedAt) || 0,
    gear: summary.gear ?? [],
  }
}
