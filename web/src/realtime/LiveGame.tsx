import { burstFor } from './liveEffects'
import { lazy, Suspense, useCallback, useEffect, useRef, useState } from 'react'
import type { EquipmentSlot, ItemMark, StatPoint } from '../api/types'
import { InventoryScreen } from '../components/InventoryScreen'
import { ShopScreen } from '../components/ShopScreen'
import { TravelScreen } from '../components/TravelScreen'
import { TrophyToast } from '../components/TrophyToast'
import { VictoryScreen } from '../components/VictoryScreen'
import type { TrophyInfo } from '../api/types'
import { supportsWebGL } from '../scene3d/viewMode'
import { HeaderToggles } from '../components/HeaderToggles'
import { SpellBar } from '../components/SpellBar'
import { StatusBar } from '../components/StatusBar'
import { describeEvent } from '../game/describeEvent'
import type { Cue } from '../scene3d/Actor'
import { heroAttackClip } from '../scene3d/models'
import { canMouseLook, canTouchLook, loadCameraMode, saveCameraMode, type CameraMode } from '../scene3d/viewMode'
import { headingVector, isMoveKey, moveVector, relativeMove, sameInput, sprintKeys, type MoveInput } from './input'
import { FrameBuffer, renderDelaySeconds, type Placed } from './interpolation'
import { levelUpSeconds } from '../scene3d/Effects3D'
import { LiveScene, type Blast, type Corpse, type LiveCue, type Look, type Lunge, type MovingProjectile, type Popup, type Spark } from './LiveScene'
import { LiveTouchControls } from './LiveTouchControls'
import { patchMap } from './mapPatch'
import { Predictor, type Solid } from './prediction'
import { TopDown } from './TopDown'
import { explore, meetBosses, sightFrom, type Vision } from './topDownLayout'
import { RoomConnection } from './roomConnection'
import type { EquippedItem } from '../api/types'
import type { LiveActionResponse, LiveChest, LiveEvent, LiveHero, LiveHeroState, LiveItem, LiveMonster, LivePartyMember, LiveSnapshot, LiveStairs, LiveWelcome } from './types'
import './live.css'

const HeroPortrait3D = lazy(() => import('../scene3d/HeroPortrait3D'))

const exoticLabels: Record<string, string> = {
  Riftrender: 'Riftrender!',
  Oathbound: 'Oath kept!',
  HeadsmansMoon: 'Another swing!',
  Worldsplitter: 'Staggered!',
  HuntressPromise: 'Prey falls: healed',
  Shadowstep: 'Shadowstep!',
  LastWall: 'The wall holds!',
  Echo: 'Echo!',
}

const flatKey = 'vaultrift.flatView'
const slowKey = 'vaultrift.slowHintDismissed'
const slowWindowMs = 5000
const slowFps = 24

const webgl = supportsWebGL()

function loadFlat(): boolean {
  try {
    const saved = localStorage.getItem(flatKey)
    return !webgl || saved === 'true'
  } catch {
    return !webgl
  }
}

function saveFlat(flat: boolean) {
  try {
    localStorage.setItem(flatKey, String(flat))
  } catch {
    return
  }
}

function loadSlowDismissed(): boolean {
  try {
    return localStorage.getItem(slowKey) === 'true'
  } catch {
    return false
  }
}

function dismissSlow() {
  try {
    localStorage.setItem(slowKey, 'true')
  } catch {
    return
  }
}

export type LiveMode = { kind: 'create'; worldId: string; shared: boolean } | { kind: 'join'; code: string }

interface LiveGameProps {
  mode: LiveMode
  heroId: string
  onExit: () => void
}

type Phase = { kind: 'connecting' } | { kind: 'playing'; welcome: LiveWelcome } | { kind: 'closed'; reason: string; welcome?: LiveWelcome }

const corpseMs = 760
const popupMs = 900
const swingMs = 420
const chestHintReach = 1.6
const doubleTapMs = 350
const attackKeys = new Set(['Space'])
const quickSlotCodes: Record<string, number> = { KeyQ: 0, KeyR: 1, KeyF: 2 }
const messageMs = 30000
const logSize = 7
const noNames: ReadonlyMap<number, string> = new Map()
const mouseLook = canMouseLook()
const touchLook = canTouchLook()
const yawPerPixel = 0.0028
const pitchPerPixel = 0.0022
const maxPitch = 1.1
const touchYawPerPixel = 0.006
const touchPitchPerPixel = 0.004

function strikeLine(attacker: string, target: string | undefined, event: LiveEvent, verb: string): string {
  const victim = target === undefined ? 'it' : target === 'you' ? 'you' : `the ${target}`
  if (event.missed) return `${attacker} ${attacker === 'You' ? 'miss' : 'misses'} ${victim}.`
  const element = event.element ? event.element.toLowerCase() : ''
  const effect =
    event.itemName === 'Effective' ? ` · weak to ${element}` : event.itemName === 'Resisted' ? (target === 'you' ? ` · you resist ${element}` : ` · resists ${element}`) : ''
  return `${attacker} ${verb} ${victim} for ${event.amount ?? 0}${event.critical ? ' · critical!' : ''}${effect}`
}

interface Message {
  key: number
  text: string
  until: number
}

const resumeGraceSeconds = 30
const sparkMs = 900

function sameRows(previous: readonly string[] | undefined, next: readonly string[]): boolean {
  return previous !== undefined && previous.length === next.length && previous.every((row, index) => row === next[index])
}

export function LiveGame({ mode, heroId, onExit }: LiveGameProps) {
  const [phase, setPhase] = useState<Phase>({ kind: 'connecting' })
  const [heroes, setHeroes] = useState<LiveHero[]>([])
  const [monsters, setMonsters] = useState<LiveMonster[]>([])
  const [corpses, setCorpses] = useState<Corpse[]>([])
  const [popups, setPopups] = useState<Popup[]>([])
  const [projectiles, setProjectiles] = useState<MovingProjectile[]>([])
  const [blasts, setBlasts] = useState<Blast[]>([])
  const [sparks, setSparks] = useState<Spark[]>([])
  const [lunges] = useState(() => new Map<number, Lunge>())
  const [heroCues, setHeroCues] = useState(new Map<number, Cue>())
  const [monsterCues, setMonsterCues] = useState(new Map<number, LiveCue>())
  const [heroState, setHeroState] = useState<LiveHeroState | null>(null)
  const [items, setItems] = useState<LiveItem[]>([])
  const [chests, setChests] = useState<LiveChest[]>([])
  const chestsRef = useRef<LiveChest[]>([])
  const hintedChests = useRef(new Set<number>())
  const [stairs, setStairs] = useState<LiveStairs | null>(null)
  const [messages, setMessages] = useState<Message[]>([])
  const [inventoryOpen, setInventoryOpen] = useState(false)
  const [shopOpen, setShopOpen] = useState(false)
  const [travelOpen, setTravelOpen] = useState(false)
  const [victory, setVictory] = useState(false)
  const [map, setMap] = useState<string[]>([])
  const [predictor] = useState(() => new Predictor())
  const roundTrip = useRef(120)
  const [flat, setFlat] = useState(loadFlat)
  const [mapOpen, setMapOpen] = useState(false)
  const [slowHint, setSlowHint] = useState(false)
  const [reconnecting, setReconnecting] = useState(false)
  const reconnectingRef = useRef(false)
  const [trophies, setTrophies] = useState<TrophyInfo[]>([])
  const [explored] = useState(() => new Set<string>())
  const [metBosses] = useState(() => new Set<number>())
  const [vision, setVision] = useState<Vision>(() => ({ explored: new Set<string>(), sight: () => true }))
  const visionSpot = useRef('')
  const flatRef = useRef(flat)
  const mapOpenRef = useRef(mapOpen)

  useEffect(() => {
    mapOpenRef.current = mapOpen
  }, [mapOpen])
  const mapRef = useRef<string[]>([])
  const paused = inventoryOpen || shopOpen || travelOpen || victory
  const [party, setParty] = useState<LivePartyMember[]>([])
  const partyRef = useRef<LivePartyMember[]>([])
  const [view, setView] = useState<CameraMode>(() => (mouseLook ? loadCameraMode() : 'third'))
  const viewRef = useRef(view)
  const look = useRef<Look>({ yaw: 0, pitch: 0 })
  const gameElement = useRef<HTMLDivElement>(null)
  const latestHeroes = useRef<LiveHero[]>([])
  const touchMove = useRef({ x: 0, y: 0 })
  const heroStateRef = useRef<LiveHeroState | null>(null)
  const [heroBuffer] = useState(() => new FrameBuffer<LiveHero>())
  const [monsterBuffer] = useState(() => new FrameBuffer<LiveMonster>())
  const [projectileBuffer] = useState(() => new FrameBuffer<MovingProjectile>())
  const connection = useRef<RoomConnection | null>(null)
  const welcomeRef = useRef<LiveWelcome | null>(null)
  const lastMonsters = useRef(new Map<number, LiveMonster>())
  const cueId = useRef(0)
  const pressed = useRef(new Set<string>())
  const attacking = useRef({ key: false, mouse: false })
  const localSwingAt = useRef(-Infinity)
  const sent = useRef<MoveInput>({
    x: 0,
    y: 0,
    attack: false,
    aimX: 0,
    aimY: 0,
    sprint: false,
  })
  const sprintKey = useRef(false)
  const [sprintTouch, setSprintTouch] = useState(false)
  const sprintTouchRef = useRef(false)

  const addMessages = useCallback((texts: string[]) => {
    const until = performance.now() + messageMs
    setMessages(current => {
      const next = [...current]
      for (const text of texts) {
        if (next[next.length - 1]?.text === text) next[next.length - 1] = { ...next[next.length - 1], until }
        else next.push({ key: ++cueId.current, text, until })
      }
      return next.slice(-logSize)
    })
  }, [])

  const showHero = useCallback(
    (hero: LiveHeroState) => {
      const before = heroStateRef.current
      heroStateRef.current = hero
      setHeroState(hero)
      const hints: string[] = []
      if (hero.merchant?.isNearby && !before?.merchant?.isNearby) hints.push('Strike the merchant to trade.')
      if (hero.waystone?.isNearby && !before?.waystone?.isNearby) hints.push('Strike the waystone to travel.')
      if (hints.length > 0) addMessages(hints)
    },
    [addMessages],
  )

  const applyEvents = useCallback(
    (events: LiveEvent[], snapshot: LiveSnapshot) => {
      const welcome = welcomeRef.current
      if (!welcome || events.length === 0) return

      const nextHeroCues = new Map<number, Cue>()
      const nextMonsterCues = new Map<number, LiveCue>()
      const fallen: Corpse[] = []
      const fresh: Popup[] = []
      const nextBlasts: Blast[] = []
      const nextSparks: Spark[] = []
      const now = performance.now()
      const equipment = heroStateRef.current?.player.equipment ?? welcome.equipment
      const notes: string[] = []
      const opened: number[] = []
      const heroSpot = (heroId: number | undefined) => snapshot.heroes.find(candidate => candidate.id === heroId)
      const delayMs = renderDelaySeconds * 1000
      const cue = (clip: string, hold = false, durationMs?: number): LiveCue => ({
        id: ++cueId.current,
        clip,
        delayMs,
        hold,
        durationMs,
        at: now + delayMs,
      })
      const spotOf = (monsterId: number | undefined) =>
        snapshot.monsters.find(monster => monster.id === monsterId) ?? (monsterId !== undefined ? lastMonsters.current.get(monsterId) : undefined)

      for (const event of events) {
        const burst = burstFor(event, spotOf, heroSpot)
        if (burst) nextSparks.push({ key: ++cueId.current, ...burst, start: now })

        switch (event.type) {
          case 'HeroSwung': {
            if (event.heroId === undefined) break
            if (event.heroId === welcome.heroId && now - localSwingAt.current < swingMs) break
            const gear = event.heroId === welcome.heroId ? equipment : (partyRef.current.find(member => member.heroId === event.heroId)?.equipment ?? [])
            nextHeroCues.set(
              event.heroId,
              cue(heroAttackClip(gear.find(equipped => equipped.slot === 'MainHand')?.item, gear.find(equipped => equipped.slot === 'OffHand')?.item), false, swingMs),
            )
            break
          }
          case 'MonsterWindup':
            if (event.monsterId !== undefined) nextMonsterCues.set(event.monsterId, cue('Windup', false, welcome.monsterWindupSeconds * 1000))
            break
          case 'MonsterHit': {
            const spot = spotOf(event.monsterId)
            if (event.heroId === welcome.heroId) notes.push(strikeLine('You', spot?.name, event, 'hit'))
            if (event.monsterId !== undefined && !event.missed) nextMonsterCues.set(event.monsterId, cue('Hit'))
            if (spot)
              fresh.push({
                key: ++cueId.current,
                x: spot.x,
                y: spot.y,
                text: event.missed ? 'Miss' : `${event.amount ?? 0}`,
                kind: event.missed ? 'miss' : event.critical ? 'critical' : 'damage',
                until: now + popupMs,
              })
            break
          }
          case 'MonsterSlain': {
            const spot = event.monsterId !== undefined ? lastMonsters.current.get(event.monsterId) : undefined
            if (spot) notes.push(event.heroId === welcome.heroId ? `You slay the ${spot.name}.` : `${heroSpot(event.heroId)?.name ?? 'A hero'} slays the ${spot.name}.`)
            if (event.monsterId !== undefined) nextMonsterCues.set(event.monsterId, cue('Death', true))
            if (spot)
              fallen.push({
                monster: { ...spot, health: 0 },
                until: now + corpseMs,
              })
            break
          }
          case 'HeroHit': {
            const hero = snapshot.heroes.find(candidate => candidate.id === event.heroId)
            const striker = spotOf(event.monsterId)
            if (event.heroId === welcome.heroId) notes.push(strikeLine(striker ? `The ${striker.name}` : 'A monster', 'you', event, 'hits'))
            if (hero && striker && event.monsterId !== undefined) {
              const length = Math.max(0.001, Math.hypot(hero.x - striker.x, hero.y - striker.y))
              lunges.set(event.monsterId, { at: now, x: (hero.x - striker.x) / length, y: (hero.y - striker.y) / length })
            }
            if (hero && !event.missed) nextSparks.push({ key: ++cueId.current, x: hero.x, y: hero.y, kind: 'claw', start: now })
            if (event.heroId !== undefined && !event.missed && hero && !hero.dead) nextHeroCues.set(event.heroId, cue('Hit_A'))
            if (hero)
              fresh.push({
                key: ++cueId.current,
                x: hero.x,
                y: hero.y,
                text: event.missed ? 'Miss' : `-${event.amount ?? 0}`,
                kind: event.missed ? 'miss' : 'hurt',
                until: now + popupMs,
              })
            break
          }
          case 'HeroFell':
            if (event.heroId !== undefined) nextHeroCues.set(event.heroId, cue('Death_A', true))
            if (event.heroId === welcome.heroId)
              notes.push(event.amount ? `You fell and dropped ${event.amount} coins. You rise again at the entrance.` : 'You fell. You rise again at the entrance.')
            else notes.push(`${heroSpot(event.heroId)?.name ?? 'A hero'} fell.`)
            break
          case 'HeroRewarded': {
            const hero = heroSpot(event.heroId)
            if (hero)
              fresh.push({
                key: ++cueId.current,
                x: hero.x,
                y: hero.y,
                text: `+${event.experience ?? 0} XP`,
                kind: 'reward',
                until: now + popupMs,
              })
            if (event.levels) {
              notes.push(`Level up! You reached a new level and gained stat points.`)
              if (hero)
                fresh.push({
                  key: ++cueId.current,
                  x: hero.x,
                  y: hero.y,
                  text: 'Level up!',
                  kind: 'level',
                  until: now + Math.max(popupMs * 2, levelUpSeconds * 1000),
                  levelUpHero: event.heroId,
                })
            }
            break
          }
          case 'LootPickedUp':
            if (event.itemName) notes.push(`You pick up ${event.itemName}.`)
            break
          case 'BackpackFull':
            notes.push('Your backpack is full: press Tab to make room.')
            break
          case 'ChestLooted':
            if (event.chestId !== undefined) opened.push(event.chestId)
            if (event.heroId === welcome.heroId) notes.push(`You open a chest: ${event.amount ?? 0} coins.`)
            break
          case 'RewardGranted': {
            const winner = snapshot.heroes.find(hero => hero.id === event.heroId)
            if (event.heroId === welcome.heroId)
              notes.push(event.missed ? `The Custode leaves you ${event.itemName}: your backpack is full, so it waits in the deposit.` : `The Custode leaves you ${event.itemName}.`)
            else if (winner) notes.push(`${winner.name} receives ${event.itemName}.`)
            break
          }
          case 'MimicSprang':
            if (event.chestId !== undefined) opened.push(event.chestId)
            notes.push('That chest was a mimic!')
            break
          case 'StairsUnsealed':
            setStairs(current => (current ? { ...current, isOpen: true } : current))
            notes.push(welcome.isHost ? 'The stairs are open: step on them to lead the party down.' : 'The stairs are open: the host leads the party down.')
            break
          case 'HeroAtStairs':
            if (event.heroId === welcome.heroId && !welcome.isHost) notes.push('Only the host can lead the party down.')
            break
          case 'MerchantGreeted':
            if (event.heroId === welcome.heroId) setShopOpen(true)
            break
          case 'WaystoneTouched':
            if (event.heroId !== welcome.heroId) break
            if ((heroStateRef.current?.waystone?.destinations.length ?? 0) === 0) {
              notes.push('The waystone has nowhere to take you yet: go deeper first.')
              break
            }
            if (welcome.isHost) setTravelOpen(true)
            else notes.push('Only the host can choose where the party travels.')
            break
          case 'GraveDug':
            notes.push(event.heroId === welcome.heroId ? 'Your gear stays in your grave: walk over it to take it back.' : `${heroSpot(event.heroId)?.name ?? 'A hero'} left a grave.`)
            break
          case 'GraveRecovered':
            if (event.heroId === welcome.heroId)
              notes.push(event.levels ? `You take back ${event.amount ?? 0} items; ${event.levels} do not fit and stay in the grave.` : 'You take back your gear.')
            break
          case 'GraveLost':
            notes.push('An old grave crumbles to dust.')
            break
          case 'TotemPrimed':
            if (event.heroId === welcome.heroId) notes.push(`The totem asks for ${event.amount ?? 0} coins: strike it again to pay and wake the boss.`)
            break
          case 'TotemTooExpensive':
            if (event.heroId === welcome.heroId) notes.push(`The totem asks for ${event.amount ?? 0} coins. You do not have enough.`)
            break
          case 'TotemAwakened':
            notes.push(`${heroSpot(event.heroId)?.name ?? 'A hero'} paid ${event.amount ?? 0} coins: the boss awakens!`)
            break
          case 'BossPhaseChanged':
            notes.push(`The boss grows fiercer (phase ${event.phase ?? 2}).`)
            break
          case 'TilesChanged':
            if (event.tiles && event.tile) {
              const changed = event.tiles
              const symbol = event.tile
              setMap(current => patchMap(current, changed, symbol))
            }
            break
          case 'HeroPulled':
            if (event.heroId === welcome.heroId) notes.push('Tentacles drag you closer!')
            break
          case 'HeroBlownAway':
          case 'HeroShardHit':
          case 'HeroScorched': {
            const hero = heroSpot(event.heroId)
            if (hero && (event.amount ?? 0) > 0)
              fresh.push({
                key: ++cueId.current,
                x: hero.x,
                y: hero.y,
                text: `-${event.amount ?? 0}`,
                kind: event.type === 'HeroScorched' ? 'burn' : 'hurt',
                until: now + popupMs,
              })
            if (event.type === 'HeroBlownAway' && event.heroId === welcome.heroId) notes.push('A gust hurls you back!')
            break
          }
          case 'MonsterSummoned':
            break
          case 'BossSlain':
            notes.push('The boss falls!')
            break
          case 'VaultConquered':
            setVictory(true)
            break
          case 'BoostFaded':
            if (event.itemName) notes.push(`The effect of ${event.itemName} wears off.`)
            break
          case 'AbilityActivated': {
            const hero = heroSpot(event.heroId)
            if (event.heroId !== undefined) nextHeroCues.set(event.heroId, cue('Spellcast_Raise'))
            if (hero)
              fresh.push({
                key: ++cueId.current,
                x: hero.x,
                y: hero.y,
                text: `${event.ability ?? ''}!`,
                kind: 'level',
                until: now + popupMs * 1.5,
              })
            break
          }
          case 'AbilityFaded':
            if (event.heroId === welcome.heroId && event.ability) notes.push(`${event.ability} fades.`)
            break
          case 'MonsterShardHit':
          case 'MonsterBurnedByLava':
          case 'MonsterPricked':
          case 'MonsterEnvenomed': {
            const spot = spotOf(event.monsterId)
            if (spot)
              fresh.push({
                key: ++cueId.current,
                x: spot.x,
                y: spot.y,
                text: `${event.amount ?? 0}`,
                kind: event.type === 'MonsterBurnedByLava' || event.type === 'MonsterEnvenomed' ? 'burn' : 'damage',
                until: now + popupMs,
              })
            break
          }
          case 'HeroPoisoned':
            if (event.heroId === welcome.heroId) notes.push('You breathe toxic spores: poisoned!')
            break
          case 'HeroPoisonDamaged':
          case 'HeroLifeDrained':
          case 'HeroManaSiphoned': {
            const hero = heroSpot(event.heroId)
            if (hero && (event.amount ?? 0) > 0)
              fresh.push({
                key: ++cueId.current,
                x: hero.x,
                y: hero.y,
                text: event.type === 'HeroPoisonDamaged' ? `-${event.amount ?? 0}` : `+${event.amount ?? 0}${event.type === 'HeroManaSiphoned' ? ' mana' : ''}`,
                kind: event.type === 'HeroPoisonDamaged' ? 'burn' : 'reward',
                until: now + popupMs,
              })
            break
          }
          case 'ExoticTriggered': {
            const spot = spotOf(event.monsterId) ?? heroSpot(event.heroId)
            if (spot && event.heroId === welcome.heroId)
              fresh.push({
                key: ++cueId.current,
                x: spot.x,
                y: spot.y,
                text: exoticLabels[event.ability ?? ''] ?? 'Exotic!',
                kind: 'critical',
                until: now + popupMs,
              })
            break
          }
          case 'CrystalBroken':
          case 'ObstacleBroken': {
            const tile = event.tiles?.[0]
            if (tile) nextSparks.push({ key: ++cueId.current, x: tile[0], y: tile[1], kind: event.type === 'CrystalBroken' ? 'crystal' : 'rubble', start: now })
            break
          }
          case 'MonsterScorched': {
            const spot = spotOf(event.monsterId)
            if (spot)
              fresh.push({
                key: ++cueId.current,
                x: spot.x,
                y: spot.y,
                text: `${event.amount ?? 0}`,
                kind: 'burn',
                until: now + popupMs,
              })
            break
          }
          case 'MonsterFrozen':
          case 'MonsterIgnited': {
            const spot = spotOf(event.monsterId)
            if (spot)
              fresh.push({
                key: ++cueId.current,
                x: spot.x,
                y: spot.y,
                text: event.type === 'MonsterFrozen' ? 'Frozen' : 'Burning',
                kind: event.type === 'MonsterFrozen' ? 'frozen' : 'burn',
                until: now + popupMs,
              })
            break
          }
          case 'SpellUnleashed':
            if (event.x !== undefined && event.y !== undefined)
              nextBlasts.push({
                key: ++cueId.current,
                x: event.x,
                y: event.y,
                radius: event.radius ?? 0.6,
                element: event.element,
                start: now + delayMs,
              })
            break
          default:
            break
        }
      }

      if (nextHeroCues.size > 0) setHeroCues(current => new Map([...current, ...nextHeroCues]))
      if (nextMonsterCues.size > 0) setMonsterCues(current => new Map([...current, ...nextMonsterCues]))
      if (fallen.length > 0) setCorpses(current => [...current.filter(corpse => corpse.until > now), ...fallen])
      if (fresh.length > 0) setPopups(current => [...current, ...fresh].slice(-24))
      if (notes.length > 0) addMessages(notes)
      if (opened.length > 0) setChests(current => current.filter(chest => !opened.includes(chest.id)))
      if (nextBlasts.length > 0) setBlasts(current => [...current.filter(blast => blast.start > now - 1000), ...nextBlasts])
      if (nextSparks.length > 0) setSparks(current => [...current.filter(spark => spark.start > now - sparkMs), ...nextSparks])
    },
    [addMessages, lunges],
  )

  useEffect(() => {
    let active = true
    let floor = 0
    const enterFloor = (welcome: LiveWelcome) => {
      const sameFloor = sameRows(welcomeRef.current?.map, welcome.map)
      welcomeRef.current = welcome
      predictor.forget()
      if (!sameFloor) {
        explored.clear()
        metBosses.clear()
        visionSpot.current = ''
      }
      mapRef.current = welcome.map
      setMap(welcome.map)
      setShopOpen(false)
      setTravelOpen(false)
      setParty(welcome.party)
      showHero(welcome.hero)
      setItems(welcome.items)
      setChests(welcome.chests)
      setStairs(welcome.stairs ?? null)
      setPhase({ kind: 'playing', welcome })
      if (floor !== 0 && welcome.hero.floor !== floor)
        addMessages([welcome.hero.floor === floor + 1 ? `Floor ${welcome.hero.floor}: the party goes deeper.` : `The waystone carries the party to floor ${welcome.hero.floor}.`])
      floor = welcome.hero.floor
    }
    const room = new RoomConnection({
      onSnapshot: (snapshot, receivedAt) => {
        if (snapshot.newFloor) enterFloor(snapshot.newFloor)
        heroBuffer.push(snapshot.time, snapshot.heroes, receivedAt)
        const own = snapshot.heroes.find(hero => hero.id === welcomeRef.current?.heroId)
        if (own && !own.dead && !own.away) predictor.reconcile(own, receivedAt, roundTrip.current)
        monsterBuffer.push(snapshot.time, snapshot.monsters, receivedAt)
        const flying = snapshot.projectiles.map(projectile => ({
          ...projectile,
          moving: true,
        }))
        projectileBuffer.push(snapshot.time, flying, receivedAt)
        setProjectiles(current => (current.length === 0 && flying.length === 0 ? current : flying))
        if (snapshot.hero) showHero(snapshot.hero)
        if (snapshot.trophies?.length) {
          const earned = snapshot.trophies
          setTrophies(current => [...current, ...earned])
        }
        const mine = snapshot.heroes.find(hero => hero.id === welcomeRef.current?.heroId)
        if (mine) {
          const sight = sightFrom(mapRef.current, mine.x, mine.y)
          const added = explore(explored, mapRef.current, sight)
          meetBosses(metBosses, mapRef.current, mine.x, mine.y, snapshot.monsters)
          const spot = `${Math.round(mine.x)},${Math.round(mine.y)}`
          if (added || spot !== visionSpot.current) {
            visionSpot.current = spot
            setVision({ explored: new Set(explored), sight })
          }
        }
        if (snapshot.items) setItems(snapshot.items)
        if (snapshot.party) setParty(snapshot.party)
        applyEvents(snapshot.events, snapshot)
        lastMonsters.current = new Map(snapshot.monsters.map(monster => [monster.id, monster]))
        latestHeroes.current = snapshot.heroes
        setHeroes(snapshot.heroes)
        setMonsters(snapshot.monsters)
      },
      onClosed: reason => {
        if (!active) return
        setReconnecting(false)
        setPhase(current => ({ kind: 'closed', reason, welcome: current.kind === 'connecting' ? undefined : current.welcome }))
      },
      onReconnecting: () => {
        if (!active) return
        reconnectingRef.current = true
        setReconnecting(true)
      },
      onResumed: welcome => {
        if (!active) return
        reconnectingRef.current = false
        setReconnecting(false)
        enterFloor(welcome)
        room.send(sent.current)
        addMessages(['Back in the world.'])
      },
    })
    connection.current = room

    const opening = window.setTimeout(() => {
      const entering = mode.kind === 'join' ? room.join(mode.code, heroId) : room.create(heroId, mode.worldId, mode.shared)
      entering
        .then(welcome => {
          if (!active) return
          enterFloor(welcome)
        })
        .catch((error: unknown) => {
          if (active)
            setPhase({
              kind: 'closed',
              reason: error instanceof Error ? error.message : 'Could not open the room.',
            })
        })
    }, 0)

    return () => {
      active = false
      window.clearTimeout(opening)
      void room.leave()
      connection.current = null
    }
  }, [mode, heroId, applyEvents, showHero, addMessages, heroBuffer, monsterBuffer, projectileBuffer, predictor, explored, metBosses])

  useEffect(() => {
    mapRef.current = map
  }, [map])

  useEffect(() => {
    chestsRef.current = chests
  }, [chests])

  useEffect(() => {
    let lastTap = -Infinity
    const guard = (event: TouchEvent) => {
      if (event.target instanceof Element && event.target.closest('button')) return
      if (event.timeStamp - lastTap < doubleTapMs) event.preventDefault()
      lastTap = event.timeStamp
    }
    const pinch = (event: Event) => event.preventDefault()
    document.addEventListener('touchend', guard, { passive: false })
    document.addEventListener('gesturestart', pinch)
    return () => {
      document.removeEventListener('touchend', guard)
      document.removeEventListener('gesturestart', pinch)
    }
  }, [])

  useEffect(() => {
    partyRef.current = party
  }, [party])

  useEffect(() => {
    let frame = 0
    let last = performance.now()
    const step = (now: number) => {
      frame = requestAnimationFrame(step)
      const seconds = Math.min(0.1, (now - last) / 1000)
      last = now
      const welcome = welcomeRef.current
      const server = welcome ? heroBuffer.latest()?.items.get(welcome.heroId) : undefined
      if (!welcome || !server) return
      if (server.dead) {
        predictor.forget()
        return
      }
      if (reconnectingRef.current) return
      const input = sent.current
      const fixtures = heroStateRef.current
      const solids: Solid[] = [fixtures?.merchant, fixtures?.waystone, fixtures?.totem]
        .filter(fixture => fixture != null)
        .map(fixture => ({ x: fixture.x, y: fixture.y, radius: welcome.fixtureRadius }))
      for (const monster of monsterBuffer.latest()?.items.values() ?? []) if (monster.health > 0) solids.push({ x: monster.x, y: monster.y, radius: monster.radius })
      for (const chest of chestsRef.current) {
        solids.push({ x: chest.x, y: chest.y, radius: welcome.fixtureRadius })
        if (!hintedChests.current.has(chest.id) && Math.hypot(chest.x - server.x, chest.y - server.y) <= chestHintReach) {
          hintedChests.current.add(chest.id)
          addMessages(['Strike the chest to open it.'])
        }
      }
      const motion = {
        move: input,
        speed: welcome.heroSpeed * (input.sprint ? welcome.sprintFactor : 1),
        radius: welcome.heroRadius,
        currentSpeed: welcome.currentSpeed,
        solids,
      }
      predictor.advance(mapRef.current, motion, seconds, now)
    }
    frame = requestAnimationFrame(step)
    return () => cancelAnimationFrame(frame)
  }, [heroBuffer, monsterBuffer, predictor, addMessages])

  const changeFlat = useCallback((next: boolean) => {
    flatRef.current = next
    saveFlat(next)
    setFlat(next)
    setSlowHint(false)
  }, [])

  useEffect(() => {
    if (flat || phase.kind !== 'playing' || loadSlowDismissed()) return
    let frame = 0
    let frames = 0
    let started = performance.now()
    let slowWindows = 0
    const count = (now: number) => {
      frame = requestAnimationFrame(count)
      frames++
      if (now - started < slowWindowMs) return
      const fps = (frames * 1000) / (now - started)
      slowWindows = fps < slowFps ? slowWindows + 1 : 0
      if (slowWindows >= 2) setSlowHint(true)
      frames = 0
      started = now
    }
    frame = requestAnimationFrame(count)
    return () => cancelAnimationFrame(frame)
  }, [flat, phase.kind])

  const playing = phase.kind === 'playing'

  const nextTrophy = useCallback(() => setTrophies(current => current.slice(1)), [])

  const trackHero = useCallback(
    (hero: LiveHero): Placed => {
      if (hero.id === welcomeRef.current?.heroId && predictor.isReady && !hero.dead) {
        const input = sent.current
        return { x: predictor.x, y: predictor.y, moving: input.x !== 0 || input.y !== 0 }
      }
      return heroBuffer.sample(hero.id, performance.now()) ?? { x: hero.x, y: hero.y, moving: false }
    },
    [heroBuffer, predictor],
  )

  const trackMonster = useCallback(
    (monster: LiveMonster): Placed => monsterBuffer.sample(monster.id, performance.now()) ?? { x: monster.x, y: monster.y, moving: false },
    [monsterBuffer],
  )

  const trackProjectile = useCallback(
    (projectile: MovingProjectile): Placed => projectileBuffer.sample(projectile.id, performance.now()) ?? { x: projectile.x, y: projectile.y, moving: true },
    [projectileBuffer],
  )

  useEffect(() => {
    if (!playing) return
    const measure = () => {
      connection.current
        ?.ping()
        .then(rtt => {
          roundTrip.current = roundTrip.current * 0.7 + rtt * 0.3
        })
        .catch(() => undefined)
    }
    measure()
    const timer = window.setInterval(measure, 2000)
    return () => window.clearInterval(timer)
  }, [playing])

  useEffect(() => {
    if (corpses.length === 0 && popups.length === 0 && messages.length === 0) return
    const prune = window.setTimeout(() => {
      const now = performance.now()
      setCorpses(current => current.filter(corpse => corpse.until > now))
      setPopups(current => current.filter(popup => popup.until > now))
      setMessages(current => current.filter(message => message.until > now))
    }, 120)
    return () => window.clearTimeout(prune)
  }, [corpses, popups, messages])

  const act = useCallback(
    (action: (room: RoomConnection) => Promise<LiveActionResponse>) => {
      const room = connection.current
      if (!room) return
      action(room)
        .then(response => {
          showHero(response.hero)
          const texts = response.events.map(event => describeEvent(event, noNames)).filter((text): text is string => Boolean(text))
          if (texts.length > 0) addMessages(texts)
        })
        .catch((error: unknown) => addMessages([error instanceof Error ? error.message : 'That did not work.']))
    },
    [showHero, addMessages],
  )

  const releaseAll = useCallback(() => {
    pressed.current.clear()
    attacking.current = { key: false, mouse: false }
    sprintKey.current = false
  }, [])

  const swingNow = useCallback(() => {
    const welcome = welcomeRef.current
    const mine = latestHeroes.current.find(hero => hero.id === welcome?.heroId)
    if (!welcome || !mine || mine.dead) return
    const now = performance.now()
    if (now - localSwingAt.current < swingMs) return
    localSwingAt.current = now
    const equipment = heroStateRef.current?.player.equipment ?? welcome.equipment
    const clip = heroAttackClip(equipment.find(equipped => equipped.slot === 'MainHand')?.item, equipment.find(equipped => equipped.slot === 'OffHand')?.item)
    setHeroCues(current => new Map(current).set(welcome.heroId, { id: ++cueId.current, clip, delayMs: 0, durationMs: swingMs }))
  }, [])

  const pushInput = useCallback(() => {
    const keys = moveVector(pressed.current)
    const attack = attacking.current.key || attacking.current.mouse
    const sprint = sprintKey.current || sprintTouchRef.current
    let input: MoveInput
    if (viewRef.current === 'first' && !flatRef.current) {
      const heading = headingVector(look.current.yaw)
      const steer = keys.x !== 0 || keys.y !== 0 ? keys : touchMove.current
      input = {
        ...relativeMove(steer, look.current.yaw),
        aimX: heading.x,
        aimY: heading.y,
        attack,
        sprint,
      }
    } else {
      const move = keys.x !== 0 || keys.y !== 0 ? keys : touchMove.current
      input = { ...move, aimX: 0, aimY: 0, attack, sprint }
    }
    if (sameInput(input, sent.current)) return
    if (input.attack && !sent.current.attack && !input.sprint) swingNow()
    sent.current = input
    connection.current?.send(input)
  }, [swingNow])

  const changeView = useCallback(
    (next: CameraMode) => {
      if (next === 'first' && !mouseLook && !touchLook) return
      const mine = latestHeroes.current.find(hero => hero.id === welcomeRef.current?.heroId)
      if (next === 'first') {
        look.current = { yaw: mine?.facing ?? Math.PI, pitch: 0 }
        if (mouseLook) gameElement.current?.requestPointerLock()?.catch?.(() => undefined)
      } else if (document.pointerLockElement) document.exitPointerLock()
      viewRef.current = next
      saveCameraMode(next)
      setView(next)
      pushInput()
    },
    [pushInput],
  )

  useEffect(() => {
    const element = gameElement.current
    if (!element) return
    const handleWheel = (event: WheelEvent) => {
      if (viewRef.current !== 'first' || document.pointerLockElement !== element) return
      event.preventDefault()
      if (event.deltaY > 0) changeView('third')
    }
    element.addEventListener('wheel', handleWheel, { passive: false })
    return () => element.removeEventListener('wheel', handleWheel)
  }, [changeView, phase.kind])

  useEffect(() => {
    const element = gameElement.current
    if (!element || !touchLook) return
    let finger: { id: number; x: number; y: number } | null = null
    const handleDown = (event: PointerEvent) => {
      if (event.pointerType !== 'touch' || viewRef.current !== 'first' || finger) return
      if (!(event.target instanceof HTMLCanvasElement)) return
      finger = { id: event.pointerId, x: event.clientX, y: event.clientY }
    }
    const handleMove = (event: PointerEvent) => {
      if (!finger || event.pointerId !== finger.id) return
      look.current = {
        yaw: look.current.yaw - (event.clientX - finger.x) * touchYawPerPixel,
        pitch: Math.max(-maxPitch, Math.min(maxPitch, look.current.pitch - (event.clientY - finger.y) * touchPitchPerPixel)),
      }
      finger = { id: finger.id, x: event.clientX, y: event.clientY }
      pushInput()
    }
    const handleUp = (event: PointerEvent) => {
      if (finger && event.pointerId === finger.id) finger = null
    }
    element.addEventListener('pointerdown', handleDown)
    window.addEventListener('pointermove', handleMove)
    window.addEventListener('pointerup', handleUp)
    window.addEventListener('pointercancel', handleUp)
    return () => {
      element.removeEventListener('pointerdown', handleDown)
      window.removeEventListener('pointermove', handleMove)
      window.removeEventListener('pointerup', handleUp)
      window.removeEventListener('pointercancel', handleUp)
    }
  }, [pushInput, phase.kind])

  useEffect(() => {
    const handleLook = (event: MouseEvent) => {
      if (viewRef.current !== 'first' || document.pointerLockElement !== gameElement.current) return
      look.current = {
        yaw: look.current.yaw - event.movementX * yawPerPixel,
        pitch: Math.max(-maxPitch, Math.min(maxPitch, look.current.pitch - event.movementY * pitchPerPixel)),
      }
      pushInput()
    }
    document.addEventListener('mousemove', handleLook)
    return () => document.removeEventListener('mousemove', handleLook)
  }, [pushInput])

  useEffect(() => {
    if (phase.kind !== 'playing') return
    const aiming = window.setInterval(pushInput, 50)
    return () => window.clearInterval(aiming)
  }, [phase.kind, pushInput])

  useEffect(() => {
    if (phase.kind !== 'playing') return

    const handleDown = (event: KeyboardEvent) => {
      if (event.code === 'Tab') {
        event.preventDefault()
        if (!event.repeat) {
          releaseAll()
          pushInput()
          if (document.pointerLockElement) document.exitPointerLock()
          setInventoryOpen(open => !open)
        }
        return
      }
      if (paused) {
        if (event.code === 'Escape') {
          setInventoryOpen(false)
          setShopOpen(false)
          setTravelOpen(false)
        }
        return
      }
      if (event.code === 'Escape') {
        if (mapOpenRef.current) setMapOpen(false)
        else if (viewRef.current === 'first') changeView('third')
        else onExit()
        return
      }
      if (event.code === 'KeyV') {
        if (!event.repeat && !flatRef.current) changeView(viewRef.current === 'first' ? 'third' : 'first')
        return
      }
      if (event.code === 'KeyM') {
        if (!event.repeat) setMapOpen(open => !open)
        return
      }
      if (event.code === 'KeyE') {
        if (!event.repeat) act(room => room.ability())
        return
      }
      if (/^Digit[1-4]$/.test(event.code)) {
        act(room => room.selectSpell(Number(event.code.slice(5)) - 1))
        return
      }
      if (event.code in quickSlotCodes) {
        if (!event.repeat) act(room => room.useQuickSlot(quickSlotCodes[event.code]))
        return
      }
      if (isMoveKey(event.code)) pressed.current.add(event.code)
      else if (attackKeys.has(event.code)) attacking.current.key = true
      else if (sprintKeys.has(event.code)) sprintKey.current = true
      else return
      event.preventDefault()
      pushInput()
    }
    const handleUp = (event: KeyboardEvent) => {
      if (paused) return
      if (isMoveKey(event.code)) pressed.current.delete(event.code)
      else if (attackKeys.has(event.code)) attacking.current.key = false
      else if (sprintKeys.has(event.code)) sprintKey.current = false
      else return
      pushInput()
    }
    const handleBlur = () => {
      releaseAll()
      pushInput()
    }

    window.addEventListener('keydown', handleDown)
    window.addEventListener('keyup', handleUp)
    window.addEventListener('blur', handleBlur)
    return () => {
      window.removeEventListener('keydown', handleDown)
      window.removeEventListener('keyup', handleUp)
      window.removeEventListener('blur', handleBlur)
    }
  }, [phase.kind, onExit, pushInput, paused, act, releaseAll, changeView])

  useEffect(() => {
    if (!shopOpen && !travelOpen && !victory) return
    releaseAll()
    touchMove.current = { x: 0, y: 0 }
    pushInput()
    if (document.pointerLockElement) document.exitPointerLock()
  }, [shopOpen, travelOpen, victory, releaseAll, pushInput])

  const previewEntry = useCallback(
    (entryId: number, hand?: EquipmentSlot) => connection.current?.preview(entryId, hand) ?? Promise.reject(new Error('Not connected.')),
    [],
  )
  const previewOffer = useCallback((offerId: number) => connection.current?.previewOffer(offerId) ?? Promise.reject(new Error('Not connected.')), [])

  const setMouseAttack = (down: boolean) => {
    if (paused) return
    attacking.current.mouse = down
    pushInput()
  }

  if (phase.kind === 'connecting') return <div className="live-message">Opening the vault…</div>

  const welcome = phase.welcome
  if (!welcome)
    return (
      <div className="live-message">
        <p>{phase.kind === 'closed' ? phase.reason : ''}</p>
        <button className="primary" onClick={onExit}>
          Back
        </button>
      </div>
    )

  const me = heroes.find(hero => hero.id === welcome.heroId)
  const player =
    heroState && me
      ? {
          ...heroState.player,
          health: me.health,
          maxHealth: me.maxHealth,
          mana: me.mana,
          maxMana: me.maxMana,
        }
      : heroState?.player
  const equipment = heroState?.player.equipment ?? welcome.equipment
  const partyGear = new Map<number, EquippedItem[]>(party.map(member => [member.heroId, member.equipment]))

  return (
    <div
      ref={gameElement}
      className="live-game"
      onPointerDown={event => {
        if (event.pointerType !== 'mouse' || event.button !== 0) return
        if (view === 'first' && document.pointerLockElement !== gameElement.current) {
          gameElement.current?.requestPointerLock()?.catch?.(() => undefined)
          return
        }
        setMouseAttack(true)
      }}
      onPointerUp={event => {
        if (event.pointerType === 'mouse') setMouseAttack(false)
      }}
      onPointerLeave={event => {
        if (event.pointerType === 'mouse') setMouseAttack(false)
      }}
    >
      {webgl && (
        <LiveScene
          hidden={flat}
          welcome={welcome}
          map={map}
          merchant={heroState?.merchant ?? null}
          waystone={heroState?.waystone ?? null}
          totem={heroState?.totem ?? null}
          graves={heroState?.graves ?? []}
          equipment={equipment}
          party={partyGear}
          view={view}
          look={look}
          onViewChange={changeView}
          items={items}
          chests={chests}
          stairs={stairs}
          heroes={heroes}
          monsters={monsters}
          metBosses={metBosses}
          vision={vision}
          corpses={corpses}
          heroBuffer={heroBuffer}
          monsterBuffer={monsterBuffer}
          prediction={{ predictor, move: sent, roundTrip }}
          projectiles={projectiles}
          projectileBuffer={projectileBuffer}
          blasts={blasts}
          sparks={sparks}
          heroCues={heroCues}
          monsterCues={monsterCues}
          popups={popups}
        />
      )}
      {flat && (
        <TopDown
          mode="play"
          biome={welcome.biome}
          map={map}
          heroId={welcome.heroId}
          heroes={heroes}
          monsters={monsters}
          bosses={monsters.filter(monster => monster.isBoss && metBosses.has(monster.id))}
          items={items}
          chests={chests}
          stairs={stairs}
          merchant={heroState?.merchant ?? null}
          waystone={heroState?.waystone ?? null}
          totem={heroState?.totem ?? null}
          graves={heroState?.graves ?? []}
          projectiles={projectiles}
          popups={popups}
          explored={explored}
          sparks={sparks}
          monsterCues={monsterCues}
          lunges={lunges}
          strikeReach={welcome.monsterStrikeReach}
          trackHero={trackHero}
          trackMonster={trackMonster}
          trackProjectile={trackProjectile}
        />
      )}
      {mapOpen && (
        <div className="live-map" onPointerDown={event => event.stopPropagation()} onClick={() => setMapOpen(false)}>
          <TopDown
            mode="map"
            biome={welcome.biome}
            map={map}
            explored={explored}
            heroId={welcome.heroId}
            heroes={heroes}
            monsters={monsters.filter(monster => monster.isBoss && metBosses.has(monster.id))}
            items={items}
            chests={chests}
            stairs={stairs}
            merchant={heroState?.merchant ?? null}
            waystone={heroState?.waystone ?? null}
            totem={heroState?.totem ?? null}
            graves={heroState?.graves ?? []}
            projectiles={[]}
            popups={[]}
            trackHero={trackHero}
            trackMonster={trackMonster}
            trackProjectile={trackProjectile}
          />
          <p className="live-map-hint">Floor {heroState?.floor ?? 1} · M or tap to close</p>
        </div>
      )}
      {phase.kind === 'closed' && (
        <div className="live-closed-overlay" role="alertdialog" onPointerDown={event => event.stopPropagation()}>
          <p>{phase.reason}</p>
          <button className="primary" onClick={onExit}>
            Back
          </button>
        </div>
      )}
      {reconnecting && (
        <div className="live-reconnecting" role="status">
          <strong>Connection lost</strong>
          <span>Reconnecting… your hero waits safely for {resumeGraceSeconds} seconds.</span>
        </div>
      )}
      {slowHint && !flat && phase.kind === 'playing' && (
        <div className="live-slow" onPointerDown={event => event.stopPropagation()}>
          <span>The game is running slowly on this device. The 2D view is much lighter.</span>
          <button
            type="button"
            className="primary"
            onClick={() => {
              if (view === 'first') changeView('third')
              changeFlat(true)
            }}
          >
            Switch to 2D
          </button>
          <button
            type="button"
            onClick={() => {
              dismissSlow()
              setSlowHint(false)
            }}
          >
            Keep 3D
          </button>
        </div>
      )}
      {trophies.length > 0 && <TrophyToast trophy={trophies[0]} onDone={nextTrophy} />}
      <div className="live-top">
        {player && heroState && (
          <div className="live-status" onPointerDown={event => event.stopPropagation()}>
            <StatusBar
              player={player}
              deaths={heroState.deaths}
              floor={heroState.floor}
              floorCount={heroState.floorCount}
              biome={welcome.biome}
              difficulty={heroState.difficulty}
              onOpenInventory={() => {
                if (document.pointerLockElement) document.exitPointerLock()
                releaseAll()
                pushInput()
                setInventoryOpen(true)
              }}
            />
          </div>
        )}
        {player && player.spells.length > 0 && (
          <div className="live-spells" onPointerDown={event => event.stopPropagation()}>
            <SpellBar
              spells={player.spells}
              active={player.activeSpell}
              mana={player.mana}
              overloaded={player.isOverloaded}
              disabled={false}
              onSelect={index => act(room => room.selectSpell(index))}
            />
          </div>
        )}
        <div className="live-party" onPointerDown={event => event.stopPropagation()}>
          {welcome.isShared ? (
            <span>
              World code <strong>{welcome.roomCode}</strong> · {party.length}/{welcome.maxHeroes}
            </span>
          ) : (
            <span>Solo world</span>
          )}
          <ul>
            {party.map(member => (
              <li key={member.heroId} className={member.heroId === welcome.heroId ? 'me' : undefined}>
                {member.name}
                {member.isHost ? ' ★' : ''}
              </li>
            ))}
          </ul>
          <div className="live-tools">
            <button type="button" title="Map (M)" onClick={() => setMapOpen(open => !open)}>
              Map
            </button>
            <button
              type="button"
              disabled={!webgl}
              title="Switch between the 3D and the 2D view"
              onClick={() => {
                if (!flat && view === 'first') changeView('third')
                changeFlat(!flat)
              }}
            >
              {flat ? '3D' : '2D'}
            </button>
            <HeaderToggles />
          </div>
        </div>
      </div>
      <LiveTouchControls
        quickSlots={player?.quickSlots ?? []}
        onMove={(x, y) => {
          touchMove.current = { x, y }
          pushInput()
        }}
        onAttack={down => {
          attacking.current.mouse = down
          pushInput()
        }}
        onQuickSlot={slot => act(room => room.useQuickSlot(slot))}
        ability={player?.ability}
        onAbility={() => act(room => room.ability())}
        sprinting={sprintTouch}
        onSprint={on => {
          sprintTouchRef.current = on
          setSprintTouch(on)
          pushInput()
        }}
        onBackpack={() => {
          releaseAll()
          touchMove.current = { x: 0, y: 0 }
          pushInput()
          setInventoryOpen(true)
        }}
        onLeave={onExit}
      />
      {messages.length > 0 && (
        <ul className="live-log">
          {[...messages].reverse().map(message => (
            <li key={message.key}>{message.text}</li>
          ))}
        </ul>
      )}
      {me?.dead && <div className="live-fallen">You fell. Rising at the entrance…</div>}
      <p className="live-hint">
        {view === 'first' && !flat
          ? 'First person · click to look with the mouse · WASD to move · Shift to run · click or Space to attack · scroll out, V or Esc for third person'
          : `WASD or arrows to move · Shift to run · Space or click to attack · ${flat ? '' : 'V first person · '}E ability · 1–4 spells · Q R F quick slots · Tab backpack · M map · Esc to leave`}
      </p>
      {shopOpen && player && heroState?.merchant && (
        <div className="live-overlay" onPointerDown={event => event.stopPropagation()}>
          <ShopScreen
            merchant={heroState.merchant}
            coins={player.coins}
            entries={heroState.inventory}
            capacity={heroState.inventoryCapacity}
            disabled={!heroState.merchant.isNearby}
            player={player}
            onPreviewOffer={previewOffer}
            onBuy={offerId => act(room => room.buy(offerId))}
            onBuyUpgrade={() => act(room => room.buyUpgrade())}
            onSell={entryId => act(room => room.sell(entryId))}
            onSellJunk={() => act(room => room.sellJunk())}
            onClose={() => setShopOpen(false)}
          />
        </div>
      )}
      {travelOpen && heroState?.waystone && (
        <div className="live-overlay" onPointerDown={event => event.stopPropagation()}>
          <TravelScreen
            waystone={heroState.waystone}
            currentFloor={heroState.floor}
            disabled={!welcome.isHost}
            onTravel={destination => {
              setTravelOpen(false)
              connection.current?.travel(destination).catch((error: unknown) => addMessages([error instanceof Error ? error.message : 'The waystone did not answer.']))
            }}
            onClose={() => setTravelOpen(false)}
          />
        </div>
      )}
      {victory && heroState && (
        <div className="live-overlay" onPointerDown={event => event.stopPropagation()}>
          <VictoryScreen heroName={welcome.heroName} deaths={heroState.deaths} onKeepExploring={() => setVictory(false)} onNewGame={onExit} />
        </div>
      )}
      {inventoryOpen && player && heroState && (
        <div className="live-overlay" onPointerDown={event => event.stopPropagation()}>
          <InventoryScreen
            player={player}
            entries={heroState.inventory}
            capacity={heroState.inventoryCapacity}
            disabled={false}
            onEquip={(entryId: number, hand?: EquipmentSlot) => act(room => room.equip(entryId, hand))}
            onUnequip={(slot: EquipmentSlot) => act(room => room.unequip(slot))}
            onUse={(entryId: number) => act(room => room.use(entryId))}
            onAssignQuickSlot={(entryId: number, slot: number) => act(room => room.assignQuickSlot(entryId, slot))}
            onLearn={(entryId: number, key: number) => act(room => room.learn(entryId, key))}
            onSpendPoint={(stat: StatPoint) => act(room => room.spendPoint(stat))}
            onDiscard={(entryId: number) => act(room => room.discard(entryId))}
            onDiscardJunk={() => act(room => room.discardJunk())}
            onMark={(entryId: number, mark: ItemMark) => act(room => room.mark(entryId, mark))}
            onPreview={previewEntry}
            onClose={() => setInventoryOpen(false)}
            portrait={
              <Suspense fallback={null}>
                <HeroPortrait3D player={player} />
              </Suspense>
            }
          />
        </div>
      )}
    </div>
  )
}
