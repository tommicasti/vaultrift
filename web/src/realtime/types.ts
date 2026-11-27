import type { Difficulty, Element, EquippedItem, GameEvent, InventoryEntryState, ItemInfo, MerchantState, PlayerState, Rarity, TotemState, TrophyInfo, WaystoneState } from '../api/types'

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
  chests: LiveChest[]
  stairs?: LiveStairs | null
  worldId: string
  isShared: boolean
  heroSpeed: number
  heroRadius: number
  resumeToken: string
  sprintFactor: number
  currentSpeed: number
  fixtureRadius: number
  protocol?: number
}

export interface LiveChest {
  id: number
  x: number
  y: number
}

export interface LiveStairs {
  x: number
  y: number
  isOpen: boolean
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
  deepestFloor: number
  isConquered: boolean
  merchant?: MerchantState | null
  waystone?: WaystoneState | null
  totem?: TotemState | null
  graves: LiveGrave[]
}

export interface LiveGrave {
  id: number
  x: number
  y: number
  count: number
  ownerName: string
  isMine: boolean
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
  away: boolean
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
  | 'ChestLooted'
  | 'MimicSprang'
  | 'StairsUnsealed'
  | 'HeroAtStairs'
  | 'MerchantGreeted'
  | 'WaystoneTouched'
  | 'GraveDug'
  | 'GraveRecovered'
  | 'GraveLost'
  | 'TotemPrimed'
  | 'TotemTooExpensive'
  | 'TotemAwakened'
  | 'BossPhaseChanged'
  | 'TilesChanged'
  | 'HeroPulled'
  | 'HeroBlownAway'
  | 'HeroShardHit'
  | 'HeroScorched'
  | 'MonsterSummoned'
  | 'MonsterWithered'
  | 'BossSlain'
  | 'FloorCleared'
  | 'VaultConquered'
  | 'RewardGranted'
  | 'CrystalBroken'
  | 'ObstacleBroken'
  | 'MonsterShardHit'
  | 'MonsterBurnedByLava'
  | 'MonsterPricked'
  | 'MonsterEnvenomed'
  | 'HeroPoisoned'
  | 'HeroPoisonDamaged'
  | 'HeroLifeDrained'
  | 'HeroManaSiphoned'
  | 'ExoticTriggered'

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
  chestId?: number
  tiles?: [number, number][]
  tile?: string
  phase?: number
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
  newFloor?: LiveWelcome | null
  trophies?: TrophyInfo[] | null
  full?: boolean
  gone?: number[] | null
}
