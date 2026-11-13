export type Direction = 'Up' | 'Down' | 'Left' | 'Right'

export type Rarity = 'Common' | 'Rare' | 'Epic' | 'Legendary'

export type ItemCategory = 'Sword' | 'Axe' | 'Bow' | 'Head' | 'Body' | 'Feet' | 'Ring' | 'Potion'

export type EquipmentSlot = 'Weapon' | 'Head' | 'Body' | 'Feet' | 'Ring1' | 'Ring2'

export type EquipRejectReason = 'ItemNotFound' | 'NotEquippable' | 'AlreadyEquipped'

export interface ItemInfo {
  id: string
  name: string
  rarity: Rarity
  category: ItemCategory
}

export interface EquippedItem {
  slot: EquipmentSlot
  entryId: number
}

export interface PlayerState {
  x: number
  y: number
  facing: Direction
  health: number
  maxHealth: number
  attack: number
  defense: number
  evasion: number
  equipment: EquippedItem[]
}

export interface EnemyState {
  id: number
  x: number
  y: number
  health: number
  maxHealth: number
  isAwake: boolean
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

export interface GameEvent {
  type: GameEventType
  enemyId?: number
  damage?: number
  isCritical?: boolean
  isDodged?: boolean
  x?: number
  y?: number
  item?: ItemInfo
  reason?: EquipRejectReason
}

export interface TurnResponse {
  turnPassed: boolean
  events: GameEvent[]
  state: GameState
}