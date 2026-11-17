export type Direction = 'Up' | 'Down' | 'Left' | 'Right'

export type Rarity = 'Common' | 'Rare' | 'Epic' | 'Legendary'

export type ItemCategory = 'Sword' | 'Axe' | 'Bow' | 'Dagger' | 'Head' | 'Body' | 'Feet' | 'Ring' | 'Cloak' | 'Potion'

export type EquipmentSlot = 'Weapon' | 'Head' | 'Body' | 'Feet' | 'Ring1' | 'Ring2' | 'Cloak'

export type StatType = 'Attack' | 'Defense' | 'Evasion' | 'Critical'

export type Speed = 'Slow' | 'Normal' | 'Fast'

export type Element = 'Fire' | 'Ice' | 'Lightning' | 'Wind'

export type Effectiveness = 'Neutral' | 'Effective' | 'Resisted'

export type WeaponType = 'Sword' | 'Axe' | 'Bow' | 'Dagger'

export type EquipRejectReason = 'ItemNotFound' | 'NotEquippable' | 'AlreadyEquipped'

export type UseRejectReason = 'ItemNotFound' | 'NotUsable' | 'AlreadyAtFullHealth' | 'QuickSlotEmpty'

export interface StatModifierInfo {
  stat: StatType
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
}

export interface EquippedItem {
  slot: EquipmentSlot
  entryId: number
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
  x: number
  y: number
  facing: Direction
  health: number
  maxHealth: number
  attack: number
  defense: number
  evasion: number
  critical: number
  freeSteps: number
  equipment: EquippedItem[]
  quickSlot?: ItemInfo
  quickSlotQuantity: number
  boosts: ActiveBoostInfo[]
  resistance?: Element
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
}

export type GameEventType =
  | 'PlayerMoved'
  | 'PlayerTurned'
  | 'PlayerAttackedEnemy'
  | 'PlayerAttackedNothing'
  | 'EnemyDefeated'
  | 'EnemyAttackedPlayer'
  | 'EnemyWokeUp'
  | 'EnemyMoved'
  | 'PlayerDefeated'
  | 'PlayerRespawned'
  | 'ItemPickedUp'
  | 'InventoryFull'
  | 'ItemEquipped'
  | 'EquipRejected'
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

export interface GameEvent {
  type: GameEventType
  enemyId?: number
  damage?: number
  isCritical?: boolean
  isDodged?: boolean
  x?: number
  y?: number
  item?: ItemInfo
  reason?: string
  element?: Element
  effectiveness?: Effectiveness
}

export interface TurnResponse {
  turnPassed: boolean
  events: GameEvent[]
  state: GameState
}