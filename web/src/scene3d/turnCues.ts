import type { EnemyState, GameEvent, ItemInfo, PlayerState, TilePosition } from '../api/types'
import type { Cue } from './Actor'
import type { Arc3D, Burst3D, Projectile3D } from './Effects3D'
import { heroAttackClip, type Clip, type MonsterClip } from './models'

export interface Turn {
  id: number
  events: GameEvent[]
  enemiesBefore: EnemyState[]
}

export interface Popup {
  key: string
  x: number
  y: number
  text: string
  tone: 'enemy' | 'player' | 'heal' | 'miss' | 'gold' | 'xp' | 'weak' | 'resisted'
  delayMs: number
}

export interface TurnCues {
  hero?: Cue
  enemies: Map<number, Cue>
  dying: EnemyState[]
  popups: Popup[]
  projectiles: Projectile3D[]
  bursts: Burst3D[]
  arcs: Arc3D[]
  trail: Set<string>
}

const projectileKinds: Record<string, Projectile3D['kind']> = {
  Fire: 'fire',
  Ice: 'ice',
  Lightning: 'lightning',
  Wind: 'wind',
}

const enemyStrikeDelayMs = 160
const contactDelayMs = 240
const flightStepMs = 72

export function cuesForTurn(turn: Turn | null, enemiesAfter: EnemyState[], player: PlayerState, mainHand: ItemInfo | undefined, offHand: ItemInfo | undefined): TurnCues {
  const cues: TurnCues = { enemies: new Map(), dying: [], popups: [], projectiles: [], bursts: [], arcs: [], trail: new Set() }
  if (!turn) return cues

  const before = new Map(turn.enemiesBefore.map(enemy => [enemy.id, enemy]))
  const after = new Map(enemiesAfter.map(enemy => [enemy.id, enemy]))
  const spot = (id: number | undefined): TilePosition | undefined => (id === undefined ? undefined : (after.get(id) ?? before.get(id)))
  let order = 0
  const respawns = turn.events.some(event => event.type === 'PlayerRespawned')
  const flight = turn.events.reduce((longest, event) => {
    const tiles = event.type === 'SpellCast' ? event.path : event.type === 'ArrowFlew' ? event.tiles : undefined
    return Math.max(longest, tiles?.length ?? 0)
  }, 0)
  const hitDelayMs = flight > 0 ? 120 + flight * flightStepMs : contactDelayMs

  const heroCue = (clip: Clip, delayMs = 0, hold = false) => {
    cues.hero ??= { id: turn.id * 100 + order++, clip, delayMs, hold }
  }
  const enemyCue = (id: number | undefined, clip: MonsterClip, delayMs: number, hold = false) => {
    if (id === undefined) return
    const existing = cues.enemies.get(id)
    if (existing?.hold) return
    cues.enemies.set(id, { id: turn.id * 100 + order++, clip, delayMs, hold })
  }
  const popup = (position: TilePosition | undefined, text: string, tone: Popup['tone'], delayMs: number) => {
    if (!position) return
    cues.popups.push({ key: `${turn.id}-${cues.popups.length}`, x: position.x, y: position.y, text, tone, delayMs })
  }

  for (const event of turn.events) {
    if (event.type === 'ArrowFlew' && event.tiles && event.tiles.length > 0)
      cues.projectiles.push({ key: `${turn.id}-p${cues.projectiles.length}`, kind: event.powered ? 'power' : 'arrow', from: player, tiles: event.tiles, delayMs: 0 })

    if (event.type === 'SpellCast' && event.path && event.path.length > 0) {
      cues.bursts.push({ key: `${turn.id}-cast${cues.bursts.length}`, x: player.x, y: player.y, kind: 'cast', delayMs: 0 })
      if (event.element === 'Lightning' && event.spell?.shape !== 'Chain')
        cues.arcs.push({ key: `${turn.id}-arc${cues.arcs.length}`, points: [player, event.path[event.path.length - 1]], delayMs: 120 })
      else cues.projectiles.push({
        key: `${turn.id}-p${cues.projectiles.length}`,
        kind: event.element ? projectileKinds[event.element] : 'impact',
        from: player,
        tiles: event.path,
        delayMs: 0,
      })
      if (event.spell?.shape === 'Chain' && event.tiles && event.tiles.length > 0)
        cues.arcs.push({ key: `${turn.id}-arc${cues.arcs.length}`, points: [event.path[event.path.length - 1], ...event.tiles], delayMs: event.path.length * 72 })
      const impact = new Set((event.tiles ?? []).map(tile => `${tile.x},${tile.y}`))
      for (const tile of event.path) if (!impact.has(`${tile.x},${tile.y}`)) cues.trail.add(`${tile.x},${tile.y}`)
    }

    if (event.type === 'ItemUsed' && event.item?.category === 'Scroll')
      cues.bursts.push({ key: `${turn.id}-scroll${cues.bursts.length}`, x: player.x, y: player.y, kind: 'cast', delayMs: 0 })
    if (event.type === 'LeveledUp')
      cues.bursts.push({ key: `${turn.id}-level${cues.bursts.length}`, x: player.x, y: player.y, kind: 'levelup', delayMs: 450 })
    if (event.type === 'SpellLearned')
      cues.bursts.push({ key: `${turn.id}-learn${cues.bursts.length}`, x: player.x, y: player.y, kind: 'learn', delayMs: 0 })

    if (event.type === 'ObstacleSmashed' && event.x !== undefined && event.y !== undefined)
      cues.bursts.push({ key: `${turn.id}-smash${cues.bursts.length}`, x: event.x, y: event.y, kind: 'debris', delayMs: contactDelayMs })
    if (event.type === 'EnemyDefeated') {
      const fallen = spot(event.enemyId)
      if (fallen) cues.bursts.push({ key: `${turn.id}-death${event.enemyId}`, x: fallen.x, y: fallen.y, kind: 'soul', delayMs: hitDelayMs + 120 })
    }

    if (event.type === 'PlayerDrifted' || event.type === 'PlayerBlownAway')
      cues.bursts.push({ key: `${turn.id}-gust${cues.bursts.length}`, x: player.x, y: player.y, kind: 'wind', delayMs: event.type === 'PlayerBlownAway' ? enemyStrikeDelayMs : 0 })
    if (event.type === 'PlayerBlownAway') heroCue('Hit_A', enemyStrikeDelayMs)

    if (event.type === 'BossPhaseChanged') {
      const boss = spot(event.enemyId)
      if (boss) cues.bursts.push({ key: `${turn.id}-roar${event.enemyId}`, x: boss.x, y: boss.y, kind: 'roar', delayMs: 0 })
      enemyCue(event.enemyId, 'Wake', 0)
    }

    switch (event.type) {
      case 'PlayerAttackedEnemy':
      case 'PlayerAttackedNothing':
      case 'ArrowFlew':
      case 'PlayerSwept':
      case 'PlayerThrust':
        heroCue(heroAttackClip(mainHand, offHand))
        break
      case 'SpellCast':
        heroCue('Spellcast_Shoot')
        break
      case 'ItemUsed':
        heroCue('Use_Item')
        break
      case 'ObstacleSmashed':
        heroCue(heroAttackClip(mainHand, offHand))
        break
      case 'ItemPickedUp':
      case 'ChestOpened':
        heroCue('PickUp')
        break
      case 'LeveledUp':
      case 'VaultConquered':
        heroCue('Cheer', 450)
        break
      case 'AbilityUsed':
        heroCue('Spellcast_Raise')
        break
      case 'MerchantGreeted':
      case 'TotemPrimed':
      case 'TotemTooExpensive':
      case 'WaystoneTouched':
      case 'TotemAwakened':
        heroCue('Interact')
        break
      case 'PlayerDefeated':
        cues.hero = { id: turn.id * 100 + order++, clip: 'Death_A', delayMs: enemyStrikeDelayMs + contactDelayMs, hold: !respawns }
        break
      case 'MimicRevealed':
      case 'EnemyWokeUp':
        enemyCue(event.enemyId, 'Wake', 0)
        break
      case 'EnemySummoned':
      case 'EnemyRespawned':
        enemyCue(event.enemyId, 'Spawn', 0)
        break
      case 'EnemyDefeated': {
        const enemy = before.get(event.enemyId ?? -1)
        if (enemy && !after.has(enemy.id)) cues.dying.push(enemy)
        enemyCue(event.enemyId, 'Death', hitDelayMs, true)
        break
      }
      case 'EnemyAttackedPlayer':
        enemyCue(event.enemyId, 'Attack', enemyStrikeDelayMs)
        break
    }

    if (event.type === 'EnemyAttackedPlayer') {
      const delay = enemyStrikeDelayMs + contactDelayMs + order * 40
      if (event.isDodged) popup(player, 'Dodge', 'miss', delay)
      else if (event.isBlocked) {
        popup(player, event.damage ? `${event.damage}` : 'Block', event.damage ? 'player' : 'miss', delay)
        heroCue('Block_Hit', delay)
      } else if (event.damage !== undefined) {
        popup(player, `${event.damage}`, 'player', delay)
        if (event.effectiveness === 'Effective') popup(player, 'Weak!', 'weak', delay + 120)
        if (event.effectiveness === 'Resisted') popup(player, 'Resisted', 'resisted', delay + 120)
        heroCue('Hit_A', delay)
      }
      continue
    }

    if (event.type === 'PlayerBlownAway') {
      if (event.damage) popup(player, `${event.damage}`, 'player', enemyStrikeDelayMs + contactDelayMs)
      continue
    }

    if (event.enemyId !== undefined && event.damage !== undefined) {
      const position = spot(event.enemyId)
      if (event.isDodged) popup(position, 'Miss', 'miss', hitDelayMs)
      else {
        popup(position, event.isCritical ? `${event.damage}!` : `${event.damage}`, 'enemy', hitDelayMs + cues.popups.length * 60)
        if (event.effectiveness === 'Effective') popup(position, 'Weak!', 'weak', hitDelayMs + 120)
        if (event.effectiveness === 'Resisted') popup(position, 'Resisted', 'resisted', hitDelayMs + 120)
        enemyCue(event.enemyId, 'Hit', hitDelayMs)
      }
      continue
    }

    if (event.damage !== undefined && ['PlayerScorched', 'PlayerPoisonDamaged', 'PlayerShardHit'].includes(event.type)) {
      popup(player, `${event.damage}`, 'player', 300)
      heroCue('Hit_A', 300)
    }

    if (event.type === 'ItemUsed' && event.amount) popup(player, `+${event.amount}`, 'heal', 200)
    if (event.type === 'ExperienceGained' && event.amount) popup(player, `+${event.amount} XP`, 'xp', 500)
  }

  return cues
}
