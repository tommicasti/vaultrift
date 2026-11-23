import type { HeroSummary } from '../api/accountApi'
import type { Difficulty, GameState } from '../api/types'

const storageKey = 'vaultrift.heroes'
const legacyKey = 'vaultrift.savedGame'
const maxHeroes = 20

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
  }
}

function read(key: string): unknown {
  const raw = localStorage.getItem(key)
  return raw ? JSON.parse(raw) : null
}

function write(heroes: SavedHero[]) {
  localStorage.setItem(storageKey, JSON.stringify(heroes))
}

function migrateLegacy(): SavedHero[] {
  const legacy = parse(read(legacyKey), Date.now())
  const heroes = legacy ? [legacy] : []
  write(heroes)
  localStorage.removeItem(legacyKey)
  return heroes
}

export function loadHeroes(): SavedHero[] {
  try {
    const stored = read(storageKey)
    if (!Array.isArray(stored)) return migrateLegacy()

    return stored
      .map((value: unknown) => parse(value, 0))
      .filter((hero): hero is SavedHero => hero !== null)
      .sort((left, right) => right.playedAt - left.playedAt)
  } catch {
    return []
  }
}

export function rememberHero(game: GameState) {
  const hero: SavedHero = {
    id: game.id,
    code: game.saveCode,
    name: game.player.name,
    classId: game.player.classId ?? null,
    floor: game.floor,
    level: game.player.level,
    difficulty: game.difficulty,
    isLost: game.isLost,
    isConquered: game.isConquered,
    playedAt: Date.now(),
  }

  try {
    const others = loadHeroes().filter(saved => saved.id !== hero.id && saved.code !== hero.code)
    write([hero, ...others].slice(0, maxHeroes))
  } catch {
    return
  }
}

export function forgetHero(id: string) {
  try {
    write(loadHeroes().filter(hero => hero.id !== id))
  } catch {
    return
  }
}

export function mergeHeroes(summaries: HeroSummary[]) {
  try {
    const local = loadHeroes()
    const known = new Map(local.map(hero => [hero.id, hero]))

    for (const summary of summaries) {
      const playedAt = Date.parse(summary.updatedAt) || 0
      const existing = known.get(summary.id)
      known.set(summary.id, {
        id: summary.id,
        code: summary.code,
        name: summary.name,
        classId: summary.classId,
        floor: summary.floor,
        level: summary.level,
        difficulty: difficulties.find(option => option === summary.difficulty) ?? 'Normal',
        isLost: summary.isLost,
        isConquered: summary.isConquered,
        playedAt: Math.max(playedAt, existing?.playedAt ?? 0),
      })
    }

    write([...known.values()].sort((left, right) => right.playedAt - left.playedAt).slice(0, maxHeroes))
  } catch {
    return
  }
}
