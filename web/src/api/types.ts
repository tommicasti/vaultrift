export type Direction = 'Up' | 'Down' | 'Left' | 'Right'

export type Rarity = 'Common' | 'Rare' | 'Epic' | 'Legendary'

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

export interface SpellInfo {
  id: string
  name: string
  element: Element
  shape: SpellShape
  power: number
  manaCost: number
  size: number
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
  spells: SpellInfo[]
  activeSpell: number
  isOverloaded: boolean
  boosts: ActiveBoostInfo[]
  resistance?: Element
  poisonTurns: number
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
}

export interface GameState {
  id: string
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
}

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
}

export interface TurnResponse {
  turnPassed: boolean
  events: GameEvent[]
  state: GameState
}