import type { Direction, GameState, TurnResponse } from './types'

async function post<T>(path: string, body?: unknown): Promise<T> {
  const response = await fetch(`/api/games${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`)
  }

  return (await response.json()) as T
}

export function createGame(): Promise<GameState> {
  return post<GameState>('')
}

export function move(gameId: string, direction: Direction): Promise<TurnResponse> {
  return post<TurnResponse>(`/${gameId}/move`, { direction })
}

export function attack(gameId: string): Promise<TurnResponse> {
  return post<TurnResponse>(`/${gameId}/attack`)
}