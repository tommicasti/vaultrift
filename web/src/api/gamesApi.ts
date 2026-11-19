import type { ClassInfo, Direction, EquipmentSlot, GameState, ItemMark, StatPoint, TurnResponse } from './types'

export class ApiError extends Error {
  readonly status: number

  constructor(status: number) {
    super(`Request failed with status ${status}`)
    this.status = status
  }
}

async function request<T>(method: 'GET' | 'POST', path: string, body?: unknown): Promise<T> {
  const response = await fetch(`/api${path}`, {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  if (!response.ok) {
    throw new ApiError(response.status)
  }

  return (await response.json()) as T
}

export function getClasses(): Promise<ClassInfo[]> {
  return request<ClassInfo[]>('GET', '/classes')
}

export function createGame(classId: string, name: string): Promise<GameState> {
  return request<GameState>('POST', '/games', { classId, name })
}

export function getGame(gameId: string): Promise<GameState> {
  return request<GameState>('GET', `/games/${gameId}`)
}

export function getGameByCode(code: string): Promise<GameState> {
  return request<GameState>('GET', `/games/code/${encodeURIComponent(code)}`)
}

export function move(gameId: string, direction: Direction): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/move`, { direction })
}

export function attack(gameId: string): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/attack`)
}

export function equip(gameId: string, entryId: number, hand?: EquipmentSlot): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/equip`, { entryId, hand })
}

export function activateAbility(gameId: string): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/ability`)
}

export function unequip(gameId: string, slot: EquipmentSlot): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/unequip`, { slot })
}

export function spendPoint(gameId: string, stat: StatPoint): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/stats`, { stat })
}

export function discardItem(gameId: string, entryId: number): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/discard`, { entryId })
}

export function discardJunk(gameId: string): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/discard-junk`)
}

export function markItem(gameId: string, entryId: number, mark: ItemMark): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/mark`, { entryId, mark })
}

export function consumeItem(gameId: string, entryId: number): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/use`, { entryId })
}

export function setQuickSlot(gameId: string, entryId: number, slot: number): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/quick-slot`, { entryId, slot })
}

export function consumeQuickSlot(gameId: string, slot: number): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/quick-slot/use`, { slot })
}

export function selectSpell(gameId: string, index: number): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/spell`, { index })
}

export function learnSpell(gameId: string, entryId: number, key: number): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/learn`, { entryId, key })
}
export function buyOffer(gameId: string, offerId: number): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/buy`, { offerId })
}

export function buyBackpackUpgrade(gameId: string): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/buy-upgrade`)
}

export function sellItem(gameId: string, entryId: number): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/sell`, { entryId })
}

export function sellJunk(gameId: string): Promise<TurnResponse> {
  return request<TurnResponse>('POST', `/games/${gameId}/sell-junk`)
}
