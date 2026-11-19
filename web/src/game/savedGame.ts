import type { GameState } from '../api/types'

const storageKey = 'vaultrift.savedGame'

export interface SavedGame {
  id: string
  code: string
  name: string
  classId: string | null
  floor: number
  level: number
}

export function loadSavedGame(): SavedGame | null {
  try {
    const raw = localStorage.getItem(storageKey)
    if (!raw) return null

    const saved = JSON.parse(raw) as Partial<SavedGame>
    if (typeof saved.id !== 'string' || typeof saved.code !== 'string') return null

    return {
      id: saved.id,
      code: saved.code,
      name: saved.name ?? 'Hero',
      classId: saved.classId ?? null,
      floor: saved.floor ?? 1,
      level: saved.level ?? 1,
    }
  } catch {
    return null
  }
}

export function rememberGame(game: GameState) {
  const saved: SavedGame = {
    id: game.id,
    code: game.saveCode,
    name: game.player.name,
    classId: game.player.classId ?? null,
    floor: game.floor,
    level: game.player.level,
  }

  try {
    localStorage.setItem(storageKey, JSON.stringify(saved))
  } catch {
    return
  }
}

export function forgetSavedGame() {
  try {
    localStorage.removeItem(storageKey)
  } catch {
    return
  }
}
