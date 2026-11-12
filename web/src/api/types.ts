export type Direction = 'Up' | 'Down' | 'Left' | 'Right'

export interface PlayerState {
  x: number
  y: number
  facing: Direction
  health: number
  maxHealth: number
}

export interface EnemyState {
  id: number
  x: number
  y: number
  health: number
  maxHealth: number
  isAwake: boolean
}

export interface GameState {
  id: string
  map: string[]
  player: PlayerState
  enemies: EnemyState[]
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

export interface GameEvent {
  type: GameEventType
  enemyId?: number
  damage?: number
  isCritical?: boolean
  isDodged?: boolean
  x?: number
  y?: number
}

export interface TurnResponse {
  turnPassed: boolean
  events: GameEvent[]
  state: GameState
}