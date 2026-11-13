import type { EquipRejectReason, GameEvent } from '../api/types'

const equipRejectMessages: Record<EquipRejectReason, string> = {
  ItemNotFound: 'That item is no longer in your backpack.',
  NotEquippable: "You can't equip that.",
  AlreadyEquipped: 'That item is already equipped.',
}

export function describeEvent(event: GameEvent): string | null {
  const critical = event.isCritical ? ' Critical hit!' : ''

  switch (event.type) {
    case 'PlayerMoved':
    case 'PlayerTurned':
    case 'EnemyMoved':
      return null
    case 'PlayerAttackedEnemy':
      return event.isDodged
        ? `Enemy ${event.enemyId} dodges your attack!`
        : `You hit enemy ${event.enemyId} for ${event.damage} damage.${critical}`
    case 'PlayerAttackedNothing':
      return 'You swing at the empty air.'
    case 'EnemyDefeated':
      return `Enemy ${event.enemyId} is defeated.`
    case 'EnemyAttackedPlayer':
      return event.isDodged
        ? `You dodge enemy ${event.enemyId}'s attack!`
        : `Enemy ${event.enemyId} hits you for ${event.damage} damage.${critical}`
    case 'EnemyWokeUp':
      return `Enemy ${event.enemyId} wakes up!`
    case 'PlayerDefeated':
      return 'You have fallen...'
    case 'PlayerRespawned':
      return 'You wake up at the entrance of the floor.'
    case 'ItemPickedUp':
      return `You pick up ${event.item?.name}.`
    case 'InventoryFull':
      return `Your backpack is full. ${event.item?.name} stays on the floor.`
    case 'ItemEquipped':
      return `You equip ${event.item?.name}.`
    case 'EquipRejected':
      return equipRejectMessages[event.reason ?? 'NotEquippable']
  }
}