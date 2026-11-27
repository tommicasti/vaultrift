import type { Difficulty, EquipmentSlot, ItemInfo, TrophyInfo } from './types'

export interface HeroSummary {
  id: string
  code: string
  name: string
  classId: string
  floor: number
  level: number
  difficulty: Difficulty
  isLost: boolean
  isConquered: boolean
  playSeconds: number
  updatedAt: string
  gear?: HeroGearItem[]
}

export interface HeroGearItem {
  slot: EquipmentSlot
  item: ItemInfo
}

export interface AccountInfo {
  userName: string
  hasEmail: boolean
  heroes: HeroSummary[]
  trophies: TrophyInfo[]
  claimed: number
  isAdmin?: boolean
}

export class AccountError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

const fallbackMessages: Record<number, string> = {
  401: 'Wrong name or password.',
  409: 'That name is already taken.',
  429: 'Too many attempts. Wait a minute and try again.',
}

async function readDetail(response: Response): Promise<string | undefined> {
  try {
    return ((await response.json()) as { detail?: string }).detail
  } catch {
    return undefined
  }
}

async function send(method: 'GET' | 'POST' | 'DELETE', path: string, body?: unknown): Promise<Response> {
  const response = await fetch(`/api/account${path}`, {
    method,
    headers: { 'Content-Type': 'application/json' },
    credentials: 'same-origin',
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  if (response.ok) return response

  const detail = await readDetail(response)
  throw new AccountError(response.status, detail ?? fallbackMessages[response.status] ?? 'Something went wrong. Try again.')
}

export async function getAccount(): Promise<AccountInfo | null> {
  const response = await send('GET', '')
  return response.status === 204 ? null : ((await response.json()) as AccountInfo)
}

export async function register(userName: string, password: string, email: string, codes: string[]): Promise<AccountInfo> {
  const response = await send('POST', '/register', { userName, password, email: email.trim() === '' ? null : email.trim(), codes })
  return (await response.json()) as AccountInfo
}

export async function login(userName: string, password: string, codes: string[]): Promise<AccountInfo> {
  const response = await send('POST', '/login', { userName, password, codes })
  return (await response.json()) as AccountInfo
}

export async function claimCodes(codes: string[]): Promise<AccountInfo> {
  const response = await send('POST', '/claim', { codes })
  return (await response.json()) as AccountInfo
}

export async function forgetHero(gameId: string): Promise<AccountInfo> {
  const response = await send('DELETE', `/games/${gameId}`)
  return (await response.json()) as AccountInfo
}

export async function logout(): Promise<void> {
  await send('POST', '/logout', {})
}
