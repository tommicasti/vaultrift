import type { Difficulty, Element, EquippedItem, GameEvent, InventoryEntryState, ItemInfo, PlayerState, Rarity } from '../api/types'

export interface LiveWelcome {
  heroId: number
  classId: string
  heroName: string
  biome?: string
  map: string[]
  equipment: EquippedItem[]
  ticksPerSecond: number
  monsterWindupSeconds: number
  monsterStrikeReach: number
  hero: LiveHeroState
  items: LiveItem[]
  roomCode: string
  isHost: boolean
  maxHeroes: number
  party: LivePartyMember[]
}

export interface LivePartyMember {
  heroId: number
  name: string
  classId: string
  isHost: boolean
  equipment: EquippedItem[]
}

export interface LiveHeroState {
  player: PlayerState
  inventory: InventoryEntryState[]
  inventoryCapacity: number
  floor: number
  floorCount: number
  difficulty: Difficulty
  deaths: number
}

export interface LiveItem {
  id: number
  x: number
  y: number
  item: ItemInfo
}

export interface LiveActionResponse {
  events: GameEvent[]
  hero: LiveHeroState
}

export interface LiveHero {
  id: number
  name: string
  classId: string
  x: number
  y: number
  facing: number
  health: number
  maxHealth: number
  mana: number
  maxMana: number
  moving: boolean
  dead: boolean
  attack: AttackStyle
  reach: number
  arcDegrees: number
  invisible: boolean
  bulwark: boolean
  fury: boolean
}

export type AttackStyle = 'Strike' | 'Stab' | 'Sweep' | 'Thrust' | 'Shoot' | 'Cast'

export interface LiveProjectile {
  id: number
  ownerId: number
  kind: 'Arrow' | 'Spell'
  x: number
  y: number
  facing: number
  element?: Element | null
}

export interface LiveMonster {
  id: number
  kind: string
  name: string
  rarity: Rarity
  level: number
  isBoss: boolean
  x: number
  y: number
  facing: number
  radius: number
  health: number
  maxHealth: number
  awake: boolean
  moving: boolean
  burning: boolean
  frozen: boolean
}

export type LiveEventType =
  | 'HeroSwung'
  | 'MonsterHit'
  | 'MonsterSlain'
  | 'MonsterWoke'
  | 'MonsterWindup'
  | 'HeroHit'
  | 'HeroFell'
  | 'HeroRespawned'
  | 'HeroRewarded'
  | 'LootDropped'
  | 'LootPickedUp'
  | 'BackpackFull'
  | 'BoostFaded'
  | 'AbilityActivated'
  | 'AbilityFaded'
  | 'MonsterIgnited'
  | 'MonsterScorched'
  | 'MonsterFrozen'
  | 'MonsterPushed'
  | 'SpellUnleashed'
  | 'ShotFired'

export interface LiveEvent {
  type: LiveEventType
  heroId?: number
  monsterId?: number
  amount?: number
  critical: boolean
  missed: boolean
  experience?: number
  levels?: number
  itemId?: number
  itemName?: string
  ability?: string
  spellId?: string
  element?: Element | null
  x?: number
  y?: number
  radius?: number
}

export interface LiveSnapshot {
  tick: number
  time: number
  heroes: LiveHero[]
  monsters: LiveMonster[]
  events: LiveEvent[]
  projectiles: LiveProjectile[]
  hero?: LiveHeroState | null
  items?: LiveItem[] | null
  party?: LivePartyMember[] | null
}
