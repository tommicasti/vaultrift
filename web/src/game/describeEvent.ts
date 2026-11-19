import type {
  AbilityRejectReason,
  ClassAbility,
  EquipRejectReason,
  GameEvent,
  SpendRejectReason,
  UnequipRejectReason,
  UseRejectReason,
} from '../api/types'
import { elementNames } from './items'

const equipRejectMessages: Record<EquipRejectReason, string> = {
  ItemNotFound: 'That item is no longer in your backpack.',
  NotEquippable: "You can't equip that.",
  AlreadyEquipped: 'That item is already equipped.',
  BackpackFull: 'Your backpack is full. Make room for what you would take off first.',
  WrongClass: 'That class mod belongs to another class.',
}

const unequipRejectMessages: Record<UnequipRejectReason, string> = {
  SlotEmpty: 'There is nothing to take off there.',
  BackpackFull: 'Your backpack is full. Make some room first.',
}

const spendRejectMessages: Record<SpendRejectReason, string> = {
  NoPointsLeft: 'You have no points left to spend.',
  StatAtCap: 'That stat is already at its limit.',
}

const useRejectMessages: Record<UseRejectReason, string> = {
  ItemNotFound: "You don't have any left.",
  NotUsable: "You can't use that.",
  AlreadyAtFullHealth: 'You are already at full health.',
  AlreadyAtFullMana: 'Your mana is already full.',
  QuickSlotEmpty: 'Your quick slot is empty.',
}

const abilityRejectMessages: Record<AbilityRejectReason, string> = {
  NoAbility: 'You have no special ability.',
  OnCooldown: 'Your ability is still recharging.',
  NeedsBow: 'Power Shot needs a bow in your main hand.',
}

const abilityUsedMessages: Record<ClassAbility, string> = {
  Bulwark: 'You raise your guard. Bulwark!',
  Fury: 'Fury takes over. Your blows grow heavier!',
  PowerShot: 'You loose a Power Shot!',
  Invisibility: 'You melt into the shadows.',
}

const abilityEndedMessages: Record<ClassAbility, string> = {
  Bulwark: 'Your guard drops.',
  Fury: 'Your fury fades.',
  PowerShot: '',
  Invisibility: 'You are visible again.',
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
    case 'StatPointSpent':
    case 'ArrowFlew':
    case 'PlayerSwept':
    case 'PlayerThrust':
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
      if (event.isDodged) return `You dodge ${enemyLower}'s attack!`
      if (event.isBlocked) return `You block ${enemyLower}'s attack!`
      return `${enemy} hits you${withElement} for ${event.damage} damage.${critical}${note}`
    case 'LifeDrained':
      return `You drain ${event.amount} health.`
    case 'EnemyPricked':
      return `${enemy} takes ${event.damage} damage striking you.`
    case 'EnemySlammed':
      return `${enemy} slams into an obstacle for ${event.damage} damage!`
    case 'AbilityUsed':
      return event.ability ? abilityUsedMessages[event.ability] : null
    case 'AbilityRejected':
      return abilityRejectMessages[event.reason as AbilityRejectReason]
    case 'AbilityEnded':
      return event.ability ? abilityEndedMessages[event.ability] || null : null
    case 'ExperienceGained':
      return `+${event.amount} XP.`
    case 'LeveledUp':
      return `Level up! You are now level ${event.level}. Press Tab to spend your points.`
    case 'SpendRejected':
      return spendRejectMessages[event.reason as SpendRejectReason]
    case 'EnemyWokeUp':
      return `${enemy} wakes up!`
    case 'EnemyRespawned':
      return 'Something stirs in the dark. A monster has returned.'
    case 'PlayerDefeated':
      return 'You have fallen...'
    case 'PlayerRespawned':
      return 'You wake up at the entrance of the floor.'
    case 'ItemPickedUp':
      return `You pick up ${event.item?.name}.`
    case 'ItemDropped':
      return `${enemy} drops ${event.item?.name} (${event.item?.rarity})!`
    case 'ItemDiscarded':
      return `You throw away ${event.item?.name}.`
    case 'ChestOpened':
      return event.item ? `You open the chest: ${event.item.name} (${event.item.rarity})!` : 'The chest is empty.'
    case 'MimicRevealed':
      return 'The chest bares its teeth. It was a Mimic!'
    case 'InventoryFull':
      return `Your backpack is full. ${event.item?.name} stays on the floor.`
    case 'ItemEquipped':
      return `You equip ${event.item?.name}.`
    case 'EquipRejected':
      return equipRejectMessages[event.reason as EquipRejectReason]
    case 'ItemUnequipped':
      return `You take off ${event.item?.name}.`
    case 'UnequipRejected':
      return unequipRejectMessages[event.reason as UnequipRejectReason]
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
    case 'StairsSealed':
      return 'The stairs are sealed. Defeat the guardian of this floor first.'
    case 'StairsOpened':
      return 'A deep rumble echoes through the floor. The stairs are open!'
    case 'FloorDescended':
      return `You descend to floor ${event.floor}.`
    case 'PlayerScorched':
      return `The lava burns you for ${event.damage} damage!`
    case 'EnemyScorched':
      return `${enemy} burns in the lava for ${event.damage} damage.`
    case 'LavaSpread':
      return `${enemy} cracks the ground. Lava pours out!`
    case 'PlayerPulled':
      return `A tentacle drags you toward ${enemyLower}!`
    case 'VaultConquered':
      return 'The last guardian falls. The vault is yours!'
  }
}
