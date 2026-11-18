import type { ClassInfo, Direction, GameState, TurnResponse } from './types'

async function request<T>(method: 'GET' | 'POST', path: string, body?: unknown): Promise<T> {
  const response = await fetch(`/api${path}`, {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`)
  }

  return (await response.json()) as T
}

export function getClasses(): Promise<ClassInfo[]> {
  return request<ClassInfo[]>('GET', '/classes')
}

export function createGame(classId: string, name: string): Promise<GameState> {
  return request<GameState>('POST', '/games', { classId, name })
}

export function move(gameId: string, direction: Direction): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/move`, { direction })
}

export function attack(gameId: string): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/attack`)
}

export function equip(gameId: string, entryId: number): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/equip`, { entryId })
}

export function consumeItem(gameId: string, entryId: number): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/use`, { entryId })
}

export function setQuickSlot(gameId: string, entryId: number): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/quick-slot`, { entryId })
}

export function consumeQuickSlot(gameId: string): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/quick-slot/use`)
}