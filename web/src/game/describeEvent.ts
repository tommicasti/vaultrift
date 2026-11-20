import type {
  SpellRejectReason,
  TradeRejectReason,
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

const finalFloor = 21

const bossPhaseMessages: Record<string, string> = {
  SummonMinions: 'The Custode roars, and drakelings crawl out of the rift!',
  Gust: 'The Custode takes wing: its gusts sweep the arena, and it hunts faster!',
  LavaRing: 'Fire pours from the Custode!',
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
  ItemIsFavorite: 'That item is a favorite. Remove the star to throw it away.',
  NoJunk: 'Nothing is marked as junk.',
  NotEnoughMana: 'Not enough mana to read that scroll.',
  ScrollNeedsSlot: 'Put the scroll on F to cast it, or learn it with a staff.',
  NoWeaponToImbue: 'Hold a weapon in your main hand to use a rune on it.',
  WeaponCannotBeImbued: 'Runes do not work on staves or on weapons with a fixed element.',
  WeaponAlreadyImbued: 'Your weapon already has that element.',
}

const abilityRejectMessages: Record<AbilityRejectReason, string> = {
  NoAbility: 'You have no special ability.',
  OnCooldown: 'Your ability is still recharging.',
}

const abilityUsedMessages: Record<ClassAbility, string> = {
  Bulwark: 'You raise your guard. Bulwark!',
  Fury: 'Fury takes over. Your blows grow heavier!',
  PowerShot: 'You loose a Power Shot!',
  Invisibility: 'You melt into the shadows.',
  Overload: 'Arcane power surges. Your next spell is free and wider!',
}

const abilityEndedMessages: Record<ClassAbility, string> = {
  Bulwark: 'Your guard drops.',
  Fury: 'Your fury fades.',
  PowerShot: '',
  Invisibility: 'You are visible again.',
  Overload: '',
}

const tradeRejectMessages: Record<TradeRejectReason, string> = {
  NoMerchantNearby: 'Step next to the merchant to trade.',
  OfferNotFound: 'The merchant no longer has that.',
  NotEnoughCoins: 'You do not have enough coins.',
  BackpackFull: 'Your backpack is full.',
  BackpackAtMax: 'Your backpack cannot grow any bigger.',
  ItemNotFound: 'That item is no longer in your backpack.',
  ItemIsFavorite: 'That item is a favorite. Remove the star to sell it.',
  NoJunk: 'Nothing is marked as junk.',
}

const spellRejectMessages: Record<SpellRejectReason, string> = {
  ItemNotFound: 'That scroll is no longer in your backpack.',
  NotAScroll: 'Only scrolls can be learned.',
  NoStaff: 'Hold a staff to learn a spell.',
  AlreadyKnown: 'You already know that spell.',
  SpellbookFull: 'Your spellbook is full: three learned spells at most.',
  UnknownSpell: 'There is no spell in that place of your spellbook.',
  NotEnoughMana: 'Not enough mana. You swing the staff instead.',
  InvalidKey: 'Learned spells go on keys 2, 3 and 4.',
  BackpackFull: 'No room in the backpack for the scroll you would get back.',
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

function describeExotic(event: GameEvent, enemy: string): string | null {
  switch (event.power) {
    case 'Riftrender':
      return 'Riftrender tears through the rift: double damage, no armor can stop it!'
    case 'Oathbound':
      return `Oathbound keeps its promise: +${event.amount} damage for the blows you took.`
    case 'HeadsmansMoon':
      return "The Headsman's Moon claims its due: the monsters do not get to act."
    case 'Worldsplitter':
      return `${enemy} staggers and loses its next turn.`
    case 'HuntressPromise':
      return `The Huntress keeps her promise: you heal ${event.amount} health.`
    case 'Shadowstep':
      return 'You step through the shadows and strike!'
    case 'LastWall':
      return `${enemy} walks into the Last Wall.`
    case 'Echo':
      return 'The Echo Staff repeats your spell.'
    default:
      return null
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
    case 'PlayerDrifted':
    case 'EnemyDrifted':
    case 'LavaCooled':
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
    case 'ItemMarked':
    case 'SpellSelected':
    case 'EnemyPulled':
      return null
    case 'SpellCast':
      return event.spell ? `You cast ${event.spell.name}!` : null
    case 'SpellLearned':
      if (!event.spell) return null
      return event.replaced
        ? `You learn ${event.spell.name} on key ${event.slot}. ${event.replaced.name} goes back to its scroll.`
        : `You learn ${event.spell.name} on key ${event.slot}.`
    case 'SpellRejected':
      return spellRejectMessages[event.reason as SpellRejectReason]
    case 'CoinsGained':
      return `+${event.amount} coins.`
    case 'CoinsLost':
      return `You lose ${event.amount} coins.`
    case 'MerchantGreeted':
      return '"Welcome, traveller! Take a look."'
    case 'ItemBought':
      return `You buy ${event.item?.name} for ${event.price} coins.`
    case 'ItemSold':
      return `You sell ${event.item?.name} for ${event.price} coins.`
    case 'JunkSold':
      return `You sell ${event.amount} junk ${event.amount === 1 ? 'item' : 'items'} for ${event.price} coins.`
    case 'BackpackExpanded':
      return `Your backpack now holds ${event.amount} items.`
    case 'TradeRejected':
      return tradeRejectMessages[event.reason as TradeRejectReason]
    case 'ManaSiphoned':
      return `You siphon ${event.amount} mana.`
    case 'JunkDiscarded':
      return `You throw away ${event.amount} junk ${event.amount === 1 ? 'item' : 'items'}.`
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
      return event.floor === finalFloor
        ? 'You step into the Rift. Something vast stirs in the dark...'
        : `You descend to floor ${event.floor}.`
    case 'PlayerScorched':
      return `The lava burns you for ${event.damage} damage!`
    case 'EnemyScorched':
      return `${enemy} burns in the lava for ${event.damage} damage.`
    case 'LavaSpread':
      return `${enemy} cracks the ground. Lava pours out!`
    case 'PlayerPulled':
      return `A tentacle drags you toward ${enemyLower}!`
    case 'CrystalShattered':
      return 'The crystal shatters into a spray of shards!'
    case 'EnemyShardHit':
      return `Shards cut ${enemyLower} for ${event.damage} damage.`
    case 'PlayerShardHit':
      return `Crystal shards cut you for ${event.damage} damage!`
    case 'CrystalsGrew':
      return `${enemy} hums. Crystals sprout all around it!`
    case 'CrystalsBurst':
      return `${enemy} roars and its crystals burst!`
    case 'PlayerPoisoned':
      return 'You breathe in the spores. You are poisoned!'
    case 'PlayerPoisonDamaged':
      return `The poison deals ${event.damage} damage.`
    case 'PlayerBlownAway':
      return event.damage
        ? `A gust from ${enemyLower} slams you into the wall for ${event.damage} damage!`
        : `A gust from ${enemyLower} blows you away!`
    case 'EnemySummoned':
      return `A ${enemyNames.get(event.enemyId ?? -1) ?? 'minion'} springs from the ground!`
    case 'EnemyWithered':
      return `${enemy} withers away.`
    case 'BossPhaseChanged':
      return bossPhaseMessages[event.reason ?? ''] ?? `${enemy} grows stronger!`
    case 'GraveDug':
      return `Your gear stays behind in a grave where you fell (${event.amount} ${event.amount === 1 ? 'item' : 'items'}). Follow the marker on the edge of the map and walk over it to take it back.`
    case 'GraveRecovered':
      return event.level
        ? `You take back ${event.amount} ${event.amount === 1 ? 'item' : 'items'}. Your backpack is full: ${event.level} still ${event.level === 1 ? 'waits' : 'wait'} in the grave.`
        : `You take back your gear from the grave (${event.amount} ${event.amount === 1 ? 'item' : 'items'}).`
    case 'GraveLost':
      return `Your old grave crumbles to dust, and ${event.amount} ${event.amount === 1 ? 'item is' : 'items are'} lost forever.`
    case 'WeaponImbued':
      return `The rune sinks into the metal: you now wield a ${event.item?.name ?? 'new weapon'}.`
    case 'SpareWeaponTaken':
      return `You pick up a ${event.item?.name ?? 'spare weapon'} from the entrance. It is worth nothing, and it crumbles when you take your own weapon back.`
    case 'ExoticTriggered':
      return describeExotic(event, enemy)
    case 'EnemyEnvenomed':
      return `${enemy} loses ${event.damage} health to the venom (${event.amount} ${event.amount === 1 ? 'layer' : 'layers'}).`
    case 'Traveled':
      return `The waystone hums, and the rift carries you to floor ${event.floor}. It looks different from how you left it.`
    case 'TravelRejected':
      return event.reason === 'NoWaystoneNearby' ? 'Stand next to the waystone to travel.' : 'The waystone cannot take you there.'
    case 'TotemAwakened':
      return `You pay ${event.price} coins. The totem cracks open and ${enemyLower} rises again!`
    case 'TotemRejected':
      return event.reason === 'NotEnoughCoins' ? 'You do not have enough coins to wake the totem.' : 'Stand next to the totem to wake it.'
    case 'HeroLost':
      return 'Your journey ends here.'
    case 'VaultConquered':
      return 'The Custode falls. The rift closes, and the vault is yours!'
  }
}
