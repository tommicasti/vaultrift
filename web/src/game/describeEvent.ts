import type { EquipRejectReason, GameEvent, UseRejectReason } from '../api/types'
import { elementNames } from './items'

const equipRejectMessages: Record<EquipRejectReason, string> = {
  ItemNotFound: 'That item is no longer in your backpack.',
  NotEquippable: "You can't equip that.",
  AlreadyEquipped: 'That item is already equipped.',
}

const useRejectMessages: Record<UseRejectReason, string> = {
  ItemNotFound: "You don't have any left.",
  NotUsable: "You can't use that.",
  AlreadyAtFullHealth: 'You are already at full health.',
  QuickSlotEmpty: 'Your quick slot is empty.',
}

function effectivenessNote(event: GameEvent): string {
  switch (event.effectiveness) {
    case 'Effective':
      return " It's super effective!"
    case 'Resisted':
      return " It's resisted."
    default:
      return ''
  }
}

export function describeEvent(event: GameEvent, enemyNames: ReadonlyMap<number, string>): string | null {
  const critical = event.isCritical ? ' Critical hit!' : ''
  const note = effectivenessNote(event)
  const withElement = event.element ? ` with ${elementNames[event.element]}` : ''
  const enemy = event.enemyId === undefined ? 'The enemy' : `The ${enemyNames.get(event.enemyId) ?? 'enemy'}`
  const enemyLower = enemy.charAt(0).toLowerCase() + enemy.slice(1)

  switch (event.type) {
    case 'PlayerMoved':
    case 'PlayerTurned':
    case 'EnemyMoved':
    case 'ArrowFlew':
    case 'PlayerSwept':
      return null
    case 'PlayerAttackedEnemy':
      return event.isDodged
        ? `${enemy} dodges your attack!`
        : `You hit ${enemyLower}${withElement} for ${event.damage} damage.${critical}${note}`
    case 'PlayerAttackedNothing':
      return 'Your attack hits nothing.'
    case 'EnemyDefeated':
      return `${enemy} is defeated.`
    case 'EnemyAttackedPlayer':
      return event.isDodged
        ? `You dodge ${enemyLower}'s attack!`
        : `${enemy} hits you${withElement} for ${event.damage} damage.${critical}${note}`
    case 'EnemyWokeUp':
      return `${enemy} wakes up!`
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
      return equipRejectMessages[event.reason as EquipRejectReason]
    case 'ItemUsed':
      return `You use ${event.item?.name}.`
    case 'UseRejected':
      return useRejectMessages[event.reason as UseRejectReason]
    case 'QuickSlotAssigned':
      return `${event.item?.name} is now in your quick slot.`
    case 'BoostExpired':
      return `The effect of ${event.item?.name} wears off.`
    case 'EnemyIgnited':
      return `${enemy} catches fire!`
    case 'EnemyBurned':
      return `${enemy} burns for ${event.damage} damage.`
    case 'EnemyFrozen':
      return `${enemy} is frozen solid!`
    case 'EnemyThawed':
      return `${enemy} thaws out.`
    case 'EnemyShocked':
      return `Lightning jumps to ${enemyLower} for ${event.damage} damage.${note}`
    case 'EnemyKnockedBack':
      return `The wind pushes ${enemyLower} back.`
  }
}