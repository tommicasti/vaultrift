import type { Object3D } from 'three'
import type { ItemCategory, ItemInfo } from '../api/types'
import { buildCrab, buildRat, buildRatKing, buildScorpion, buildSpider, buildSpiderQueen } from './creatures'
import animationsUrl from '../assets/models/animations.glb?url'
import dungeonUrl from '../assets/models/dungeon.glb?url'
import berserkerUrl from '../assets/models/hero-berserker.glb?url'
import knightUrl from '../assets/models/hero-knight.glb?url'
import mageUrl from '../assets/models/hero-mage.glb?url'
import rangerUrl from '../assets/models/hero-ranger.glb?url'
import rogueUrl from '../assets/models/hero-rogue.glb?url'
import skeletonMageUrl from '../assets/models/monster-skeleton-mage.glb?url'
import skeletonMinionUrl from '../assets/models/monster-skeleton-minion.glb?url'
import skeletonRogueUrl from '../assets/models/monster-skeleton-rogue.glb?url'
import skeletonWarriorUrl from '../assets/models/monster-skeleton-warrior.glb?url'
import weaponsUrl from '../assets/models/weapons.glb?url'

export const cell = 4

export const modelUrls = {
  animations: animationsUrl,
  dungeon: dungeonUrl,
  weapons: weaponsUrl,
}

const heroUrls: Record<string, string> = {
  knight: knightUrl,
  berserker: berserkerUrl,
  ranger: rangerUrl,
  rogue: rogueUrl,
  mage: mageUrl,
}

export const merchantUrl = rangerUrl

export function heroUrl(classId: string | undefined): string {
  return heroUrls[classId ?? ''] ?? knightUrl
}

type SkeletonBody = 'minion' | 'warrior' | 'rogue' | 'mage'

const skeletonUrls: Record<SkeletonBody, string> = {
  minion: skeletonMinionUrl,
  warrior: skeletonWarriorUrl,
  rogue: skeletonRogueUrl,
  mage: skeletonMageUrl,
}

export type MonsterClip = 'Idle' | 'Sleep' | 'Walk' | 'Attack' | 'Hit' | 'Death' | 'Wake' | 'Spawn'

export interface MonsterLook {
  url: string
  scale: number
  height: number
  clips: Record<MonsterClip, string>
  tint?: string
  weapon?: string
  offHand?: string
  procedural?: boolean
  build?: () => Object3D
}

const skeletonClips: Record<MonsterClip, string> = {
  Idle: 'Idle_Combat',
  Sleep: 'Skeleton_Inactive_Standing_Pose',
  Walk: 'Walking_D_Skeletons',
  Attack: '1H_Melee_Attack_Chop',
  Hit: 'Hit_A',
  Death: 'Death_A',
  Wake: 'Skeletons_Awaken_Standing',
  Spawn: 'Spawn_Ground',
}

const creatureClips: Record<MonsterClip, string> = {
  Idle: 'Idle',
  Sleep: 'Idle',
  Walk: 'Walk',
  Attack: 'Attack',
  Hit: 'Hit',
  Death: 'Death',
  Wake: 'Wake',
  Spawn: 'Wake',
}

const proceduralClips: Record<MonsterClip, string> = {
  Idle: 'Idle',
  Sleep: 'Sleep',
  Walk: 'Walk',
  Attack: 'Attack',
  Hit: 'Hit',
  Death: 'Death',
  Wake: 'Wake',
  Spawn: 'Spawn',
}

const skeletonHeight = 2.7

const skeletonLooks: Record<string, [SkeletonBody, string, number, string?, string?]> = {}

const builtLooks: Record<string, [() => Object3D, number]> = {
  spider: [buildSpider, 2.1],
  rat: [buildRat, 1.9],
  'rat-king': [buildRatKing, 5.2],
  crab: [buildCrab, 2.2],
  'spider-queen': [buildSpiderQueen, 4.4],
  'dune-scorpion': [buildScorpion, 4.4],
}

const creatureUrls = import.meta.glob<string>('../assets/models/monster-*.glb', { query: '?url', import: 'default', eager: true })

function creatureUrl(model: string): string {
  const url = creatureUrls[`../assets/models/monster-${model}.glb`]
  if (!url) throw new Error(`Missing monster model ${model}`)
  return url
}

const creatureLooks: Record<string, [string, number, string?, boolean?]> = {
  mimic: ['mimic', 1.5, undefined, true],
  bat: ['ghost', 1.7],
  slime: ['greenblob', 1.5],
  ghost: ['ghost-skull', 2.3],
  cyclops: ['orc', 3.4],
  sporeling: ['mushnub', 1.6],
  drakeling: ['dragon', 1.9],
  salamander: ['dino', 2.2, '#ff8a5a'],
  'fire-imp': ['demon', 1.9],
  eel: ['fish', 1.8],
  'gem-beetle': ['armabee', 1.7, '#7ae0d0'],
  shardling: ['greenspikyblob', 1.9, '#8fe6ff'],
  myconid: ['mushnub-evolved', 2.4],
  puffball: ['pinkblob', 1.5],
  harpy: ['squidle', 2.1],
  gargoyle: ['goleling', 2.3, '#9aa0aa'],
  troll: ['orc-skull', 4.6],
  'frost-yeti': ['yeti', 4.4],
  'black-knight': ['ninja', 4],
  'magma-golem': ['tribal', 4.6],
  kraken: ['glub-evolved', 4.4, '#6a8aff'],
  'crystal-golem': ['goleling-evolved', 4.4, '#8fe6ff'],
  'mushroom-king': ['mushroomking', 4.4],
  'storm-griffin': ['alpaking-evolved', 4.2],
  custode: ['dragon-evolved', 5.6],
}

const monsterScale = 1.25

export const selfCrowned: ReadonlySet<string> = new Set(['rat-king', 'spider-queen'])

export function monsterLook(kind: string): MonsterLook {
  const look = baseLook(kind)
  return { ...look, scale: look.scale * monsterScale, height: look.height * monsterScale }
}

function baseLook(kind: string): MonsterLook {
  const built = builtLooks[kind]
  if (built) {
    const [build, scale] = built
    return { url: '', scale, height: scale * 0.85, clips: proceduralClips, procedural: true, build }
  }

  const creature = creatureLooks[kind]
  if (creature) {
    const [model, height, tint, procedural] = creature
    return { url: creatureUrl(model), scale: height, height, clips: procedural ? proceduralClips : creatureClips, tint, procedural }
  }

  const [body, tint, scale, weapon, offHand] = skeletonLooks[kind] ?? ['minion', '#ffffff', 1]
  return { url: skeletonUrls[body], scale, height: skeletonHeight * scale, clips: skeletonClips, tint, weapon, offHand }
}

const weaponModels: Partial<Record<ItemCategory, [string, string?]>> = {
  Sword: ['sword', 'sword-2h'],
  Axe: ['axe', 'axe-2h'],
  Dagger: ['dagger'],
  Spear: ['spear'],
  Staff: ['staff', 'staff'],
  Bow: ['bow'],
  Shield: ['shield'],
}

export function weaponModel(item: ItemInfo | undefined): string | undefined {
  if (!item) return undefined

  const models = weaponModels[item.category]
  if (!models) return undefined

  return item.twoHanded && !item.isTwin && models[1] ? models[1] : models[0]
}

export function offHandModel(item: ItemInfo | undefined): string | undefined {
  if (!item) return undefined
  if (item.category === 'Shield') return 'shield'
  if (item.category === 'Dagger') return 'dagger'
  if (item.category === 'Sword') return 'sword'
  if (item.category === 'Axe') return 'axe'
  return undefined
}

export type Clip =
  | 'Idle'
  | '2H_Melee_Idle'
  | 'Unarmed_Idle'
  | 'Idle_Combat'
  | 'Walking_A'
  | 'Walking_D_Skeletons'
  | '1H_Melee_Attack_Chop'
  | '1H_Melee_Attack_Stab'
  | '2H_Melee_Attack_Slice'
  | '2H_Melee_Attack_Chop'
  | '2H_Melee_Attack_Stab'
  | 'Dualwield_Melee_Attack_Stab'
  | 'Unarmed_Melee_Attack_Punch_A'
  | '2H_Ranged_Shoot'
  | 'Spellcast_Shoot'
  | 'Spellcast_Raise'
  | 'Hit_A'
  | 'Block_Hit'
  | 'Death_A'
  | 'Cheer'
  | 'Use_Item'
  | 'PickUp'
  | 'Interact'
  | 'Skeletons_Awaken_Standing'
  | 'Skeleton_Inactive_Standing_Pose'
  | 'Spawn_Ground'
  | 'Taunt'

export function heroAttackClip(mainHand: ItemInfo | undefined, offHand: ItemInfo | undefined): Clip {
  switch (mainHand?.category) {
    case 'Bow':
      return '2H_Ranged_Shoot'
    case 'Staff':
      return 'Spellcast_Shoot'
    case 'Spear':
      return '2H_Melee_Attack_Stab'
    case 'Dagger':
      return offHand?.category === 'Dagger' || mainHand.isTwin ? 'Dualwield_Melee_Attack_Stab' : '1H_Melee_Attack_Stab'
    case 'Sword':
    case 'Axe':
      return mainHand.twoHanded && !mainHand.isTwin ? '2H_Melee_Attack_Slice' : '1H_Melee_Attack_Chop'
    default:
      return 'Unarmed_Melee_Attack_Punch_A'
  }
}

export function heroIdleClip(mainHand: ItemInfo | undefined): Clip {
  if (!mainHand) return 'Unarmed_Idle'
  return mainHand.twoHanded && !mainHand.isTwin && mainHand.category !== 'Bow' ? '2H_Melee_Idle' : 'Idle'
}

export const rarityColors: Record<string, string> = {
  Common: '#d8d8d8',
  Rare: '#4aa8ff',
  Epic: '#b45aff',
  Legendary: '#ff9a2a',
  Exotic: '#ff3a5a',
  Platinum: '#d8f0ff',
}

export interface BiomeLook {
  floor: string
  wall: string
  fog: string
  ambient: string
  light: string
}

const biomeLooks: Record<string, BiomeLook> = {
  crypt: { floor: '#b8b0a8', wall: '#a8a0a0', fog: '#0c0a10', ambient: '#5a5068', light: '#ffc890' },
  forest: { floor: '#9ab07a', wall: '#8a9a70', fog: '#0a140c', ambient: '#4a6a4a', light: '#ffe0a0' },
  desert: { floor: '#e8c890', wall: '#d8b47a', fog: '#2a1e10', ambient: '#8a6a4a', light: '#fff0c0' },
  snow: { floor: '#e8f0ff', wall: '#c8d8f0', fog: '#1a2230', ambient: '#6a7a9a', light: '#e0f0ff' },
  castle: { floor: '#b0b0b8', wall: '#a0a0b0', fog: '#0c0c14', ambient: '#5a5a70', light: '#ffd0a0' },
  swamp: { floor: '#8a9a6a', wall: '#6a7a5a', fog: '#0c140a', ambient: '#4a5a3a', light: '#d0f0a0' },
  volcano: { floor: '#7a5a50', wall: '#6a4a40', fog: '#1a0804', ambient: '#6a3020', light: '#ffa060' },
  ruins: { floor: '#90b0b0', wall: '#80a0a0', fog: '#081418', ambient: '#3a5a6a', light: '#c0f0ff' },
  mines: { floor: '#a09080', wall: '#8a7a70', fog: '#0a0a10', ambient: '#4a4a6a', light: '#a0e8ff' },
  fungal: { floor: '#a08aa8', wall: '#8a7090', fog: '#100818', ambient: '#5a3a6a', light: '#e0a0ff' },
  sky: { floor: '#f0f0ff', wall: '#e0e0f0', fog: '#3a4a6a', ambient: '#8a9ac0', light: '#ffffff' },
  rift: { floor: '#8a70a8', wall: '#6a5090', fog: '#0a0418', ambient: '#4a2a7a', light: '#d090ff' },
}

export function biomeLook(biome: string | undefined): BiomeLook {
  return biomeLooks[biome ?? ''] ?? biomeLooks.crypt
}
