export type Direction = 'Up' | 'Down' | 'Left' | 'Right'

export type Rarity = 'Common' | 'Rare' | 'Epic' | 'Legendary' | 'Exotic' | 'Platinum'

export type ExoticPower =
  | 'Riftrender'
  | 'Oathbound'
  | 'HeadsmansMoon'
  | 'Worldsplitter'
  | 'RiftNeedle'
  | 'HuntressPromise'
  | 'WidowsKiss'
  | 'Shadowstep'
  | 'LastWall'
  | 'StarfallLance'
  | 'Prism'
  | 'Echo'

export type ItemCategory =
  | 'Sword'
  | 'Axe'
  | 'Bow'
  | 'Dagger'
  | 'Spear'
  | 'Shield'
  | 'Head'
  | 'Body'
  | 'Feet'
  | 'Ring'
  | 'Cloak'
  | 'ClassMod'
  | 'Potion'
  | 'Staff'
  | 'Scroll'
  | 'Rune'

export type EquipmentSlot = 'MainHand' | 'OffHand' | 'Head' | 'Body' | 'Feet' | 'Ring1' | 'Ring2' | 'Cloak' | 'ClassMod'

export type StatType = 'Attack' | 'Defense' | 'Evasion' | 'Critical' | 'Block' | 'Mana'

export type EffectKind =
  | 'FireBoost'
  | 'IceBoost'
  | 'LightningBoost'
  | 'WindBoost'
  | 'LifeSteal'
  | 'Thorns'
  | 'DoubleShot'
  | 'Luck'
  | 'Wisdom'
  | 'ManaFlow'
  | 'Haste'
  | 'Regeneration'
  | 'BulwarkExtension'
  | 'BlockReflect'
  | 'FuryLifeSteal'
  | 'FuryRegeneration'
  | 'FuryExtension'
  | 'FarShotCritical'
  | 'PowerShotMastery'
  | 'Stealth'
  | 'Ambush'
  | 'Phantom'
  | 'ManaSiphon'
  | 'SpellMastery'

export type ClassAbility = 'Bulwark' | 'Fury' | 'PowerShot' | 'Invisibility' | 'Overload'

export type SpellShape = 'Bolt' | 'Burst' | 'Line' | 'Chain' | 'Nova' | 'Storm'

export type SpellRejectReason =
  | 'ItemNotFound'
  | 'NotAScroll'
  | 'NoStaff'
  | 'AlreadyKnown'
  | 'SpellbookFull'
  | 'UnknownSpell'
  | 'NotEnoughMana'
  | 'InvalidKey'
  | 'BackpackFull'

export interface SpellInfo {
  id: string
  name: string
  element: Element
  shape: SpellShape
  power: number
  manaCost: number
  size: number
  manaPercent: number
}

export type AbilityRejectReason = 'NoAbility' | 'OnCooldown'

export type ItemMark = 'None' | 'Favorite' | 'Junk'

export type Speed = 'Slow' | 'Normal' | 'Fast'

export type Element = 'Fire' | 'Ice' | 'Lightning' | 'Wind'

export type Effectiveness = 'Neutral' | 'Effective' | 'Resisted'

export type WeaponType = 'Sword' | 'Axe' | 'Bow' | 'Dagger' | 'Spear' | 'Staff'

export type EquipRejectReason = 'ItemNotFound' | 'NotEquippable' | 'AlreadyEquipped' | 'BackpackFull' | 'WrongClass'

export type UnequipRejectReason = 'SlotEmpty' | 'BackpackFull'

export type SpendRejectReason = 'NoPointsLeft' | 'StatAtCap'

export type StatPoint = 'Health' | 'Attack' | 'Defense' | 'Evasion' | 'Critical' | 'Mana'

export type UseRejectReason =
  | 'ItemNotFound'
  | 'NotUsable'
  | 'AlreadyAtFullHealth'
  | 'AlreadyAtFullMana'
  | 'QuickSlotEmpty'
  | 'ItemIsFavorite'
  | 'NoJunk'
  | 'NotEnoughMana'
  | 'ScrollNeedsSlot'
  | 'NoWeaponToImbue'
  | 'WeaponCannotBeImbued'
  | 'WeaponAlreadyImbued'

export interface StatModifierInfo {
  stat: StatType
  amount: number
}

export interface ItemEffectInfo {
  kind: EffectKind
  amount: number
}

export interface ItemInfo {
  id: string
  name: string
  rarity: Rarity
  category: ItemCategory
  description?: string
  modifiers: StatModifierInfo[]
  healing?: number
  boost?: StatModifierInfo
  boostTurns?: number
  element?: Element
  effectChance?: number
  effects: ItemEffectInfo[]
  twoHanded: boolean
  isTwin?: boolean
  isBound?: boolean
  scalesWithLevel?: boolean
  classId?: string
  manaRestore?: number
  spell?: SpellInfo
}

export interface QuickSlotInfo {
  item?: ItemInfo
  quantity: number
}

export interface ClassInfo {
  id: string
  name: string
  description: string
  maxHealth: number
  attack: number
  defense: number
  evasion: number
  critical: number
  speed: Speed
  masteryWeapon: WeaponType
  masteryBonus: StatModifierInfo
  maxMana: number
  ability?: ClassAbility
  abilityCooldown: number
}

export interface AbilityInfo {
  kind: ClassAbility
  cooldown: number
  maxCooldown: number
  activeTurns: number
}

export interface EquippedItem {
  slot: EquipmentSlot
  entryId: number
  item: ItemInfo
  mark: ItemMark
  sellPrice: number
}

export interface ActiveBoostInfo {
  item: ItemInfo
  modifier: StatModifierInfo
  remainingTurns: number
}

export interface PlayerState {
  classId?: string
  name: string
  level: number
  experience: number
  experienceToNextLevel: number
  unspentPoints: number
  mana: number
  maxMana: number
  coins: number
  x: number
  y: number
  facing: Direction
  health: number
  maxHealth: number
  attack: number
  defense: number
  evasion: number
  critical: number
  block: number
  freeSteps: number
  ability?: AbilityInfo
  equipment: EquippedItem[]
  quickSlots: QuickSlotInfo[]
  spells: (SpellInfo | null)[]
  activeSpell: number
  isOverloaded: boolean
  boosts: ActiveBoostInfo[]
  resistance?: Element
  poisonTurns: number
  offHandAttack: number
}

export interface EnemyState {
  id: number
  kind: string
  name: string
  rarity: Rarity
  level: number
  x: number
  y: number
  health: number
  maxHealth: number
  isAwake: boolean
  weakness?: Element
  resistance?: Element
  attackElement?: Element
  isBurning: boolean
  isFrozen: boolean
  isBoss: boolean
  markedTurns: number
  venomStacks: number
  isStaggered: boolean
}

export interface StairsState {
  x: number
  y: number
  isOpen: boolean
}

export interface FloorItemState {
  x: number
  y: number
  item: ItemInfo
}

export interface InventoryEntryState {
  id: number
  item: ItemInfo
  quantity: number
  mark: ItemMark
  sellPrice: number
}

export type Difficulty = 'Normal' | 'Hard' | 'Hardcore'

export interface GraveInfo {
  x: number
  y: number
  count: number
}

export interface GameState {
  id: string
  saveCode: string
  difficulty: Difficulty
  isLost: boolean
  grave?: GraveInfo
  map: string[]
  player: PlayerState
  enemies: EnemyState[]
  floorItems: FloorItemState[]
  inventory: InventoryEntryState[]
  inventoryCapacity: number
  deaths: number
  floor: number
  floorCount: number
  stairs?: StairsState
  isConquered: boolean
  biome?: string
  chests: TilePosition[]
  merchant?: MerchantState
  waystone?: WaystoneState
  totem?: TotemState
  deepestFloor: number
  graveFloor?: number
}

export interface DestinationInfo {
  floor: number
  biome?: string
  isCleared: boolean
  hasGrave?: boolean
}

export interface WaystoneState {
  x: number
  y: number
  isNearby: boolean
  destinations: DestinationInfo[]
}

export interface TotemState {
  x: number
  y: number
  isNearby: boolean
  price: number
  bossName: string
  bossKind: string
}

export type OfferKind = 'Regular' | 'Featured' | 'Unlimited'

export interface OfferInfo {
  id: number
  item: ItemInfo
  price: number
  kind: OfferKind
}

export interface MerchantState {
  x: number
  y: number
  isNearby: boolean
  offers: OfferInfo[]
  upgradePrice?: number
  upgradeSize: number
}

export type TradeRejectReason =
  | 'NoMerchantNearby'
  | 'OfferNotFound'
  | 'NotEnoughCoins'
  | 'BackpackFull'
  | 'BackpackAtMax'
  | 'ItemNotFound'
  | 'ItemIsFavorite'
  | 'NoJunk'

export type GameEventType =
  | 'PlayerMoved'
  | 'PlayerTurned'
  | 'PlayerAttackedEnemy'
  | 'PlayerAttackedNothing'
  | 'EnemyDefeated'
  | 'ExperienceGained'
  | 'LeveledUp'
  | 'StatPointSpent'
  | 'SpendRejected'
  | 'EnemyAttackedPlayer'
  | 'EnemyWokeUp'
  | 'EnemyRespawned'
  | 'EnemyMoved'
  | 'PlayerDefeated'
  | 'PlayerRespawned'
  | 'ItemPickedUp'
  | 'InventoryFull'
  | 'ItemDropped'
  | 'ItemDiscarded'
  | 'ItemMarked'
  | 'SpellCast'
  | 'SpellSelected'
  | 'SpellLearned'
  | 'SpellRejected'
  | 'ManaSiphoned'
  | 'EnemyPulled'
  | 'CoinsGained'
  | 'CoinsLost'
  | 'MerchantGreeted'
  | 'ItemBought'
  | 'ItemSold'
  | 'JunkSold'
  | 'BackpackExpanded'
  | 'TradeRejected'
  | 'JunkDiscarded'
  | 'ChestOpened'
  | 'MimicRevealed'
  | 'LifeDrained'
  | 'EnemyPricked'
  | 'EnemySlammed'
  | 'AbilityUsed'
  | 'AbilityRejected'
  | 'AbilityEnded'
  | 'ItemEquipped'
  | 'EquipRejected'
  | 'ItemUnequipped'
  | 'UnequipRejected'
  | 'ItemUsed'
  | 'UseRejected'
  | 'QuickSlotAssigned'
  | 'BoostExpired'
  | 'EnemyIgnited'
  | 'EnemyBurned'
  | 'EnemyFrozen'
  | 'EnemyThawed'
  | 'EnemyShocked'
  | 'EnemyKnockedBack'
  | 'ArrowFlew'
  | 'PlayerSwept'
  | 'PlayerThrust'
  | 'StairsSealed'
  | 'StairsOpened'
  | 'FloorDescended'
  | 'VaultConquered'
  | 'PlayerScorched'
  | 'EnemyScorched'
  | 'LavaSpread'
  | 'LavaCooled'
  | 'PlayerPulled'
  | 'CrystalShattered'
  | 'ObstacleSmashed'
  | 'EnemyShardHit'
  | 'PlayerShardHit'
  | 'CrystalsGrew'
  | 'CrystalsBurst'
  | 'PlayerPoisoned'
  | 'PlayerPoisonDamaged'
  | 'PlayerDrifted'
  | 'EnemyDrifted'
  | 'PlayerBlownAway'
  | 'EnemySummoned'
  | 'EnemyWithered'
  | 'BossPhaseChanged'
  | 'GraveDug'
  | 'GraveRecovered'
  | 'GraveLost'
  | 'GraveLeftBehind'
  | 'GraveAwaits'
  | 'TotemPrimed'
  | 'TotemTooExpensive'
  | 'WaystoneTouched'
  | 'HeroLost'
  | 'SpareWeaponTaken'
  | 'WeaponImbued'
  | 'ExoticTriggered'
  | 'EnemyEnvenomed'
  | 'Traveled'
  | 'TravelRejected'
  | 'TotemAwakened'
  | 'TotemRejected'

export interface TilePosition {
  x: number
  y: number
}

export interface GameEvent {
  type: GameEventType
  enemyId?: number
  damage?: number
  isCritical?: boolean
  isDodged?: boolean
  isBlocked?: boolean
  x?: number
  y?: number
  item?: ItemInfo
  reason?: string
  element?: Element
  effectiveness?: Effectiveness
  tiles?: TilePosition[]
  floor?: number
  amount?: number
  level?: number
  stat?: StatPoint
  powered?: boolean
  ability?: ClassAbility
  path?: TilePosition[]
  spell?: SpellInfo
  slot?: number
  price?: number
  replaced?: SpellInfo
  power?: ExoticPower
}

export interface TrophyInfo {
  id: string
  name: string
  description: string
  unlockedAt?: string
  progress?: number
  goal?: number
}

export interface TurnResponse {
  turnPassed: boolean
  events: GameEvent[]
  state: GameState
  trophies?: TrophyInfo[]
}

export interface StatsPreview {
  result: string
  maxHealth: number
  maxMana: number
  attack: number
  defense: number
  evasion: number
  critical: number
  block: number
  resistance?: Element
}
