import type { GameEvent } from '../api/types'

export function describeEvent(event: GameEvent): string | null {
  const critical = event.isCritical ? ' Critical hit!' : ''

  switch (event.type) {
    case 'PlayerMoved':
    case 'PlayerTurned':
    case 'EnemyMoved':
      return null
    case 'PlayerAttackedEnemy':
      return `You hit enemy ${event.enemyId} for ${event.damage} damage.${critical}`
    case 'PlayerAttackedNothing':
      return 'You swing at the empty air.'
    case 'EnemyDefeated':
      return `Enemy ${event.enemyId} is defeated.`
    case 'EnemyAttackedPlayer':
      return `Enemy ${event.enemyId} hits you for ${event.damage} damage.${critical}`
    case 'EnemyWokeUp':
      return `Enemy ${event.enemyId} wakes up!`
    case 'PlayerDefeated':
      return 'You have fallen...'
    case 'PlayerRespawned':
      return 'You wake up at the entrance of the floor.'
  }
}