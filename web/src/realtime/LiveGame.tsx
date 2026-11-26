import { lazy, Suspense, useCallback, useEffect, useRef, useState } from 'react'
import type { EquipmentSlot, ItemMark, StatPoint } from '../api/types'
import { InventoryScreen } from '../components/InventoryScreen'
import { StatusBar } from '../components/StatusBar'
import { describeEvent } from '../game/describeEvent'
import type { Cue } from '../scene3d/Actor'
import { heroAttackClip } from '../scene3d/models'
import { aimToward, isMoveKey, moveVector, sameInput, type MoveInput } from './input'
import { FrameBuffer, renderDelaySeconds } from './interpolation'
import { LiveScene, type Blast, type Corpse, type GroundPoint, type LiveCue, type MovingProjectile, type Popup } from './LiveScene'
import { LiveTouchControls } from './LiveTouchControls'
import { RoomConnection } from './roomConnection'
import type { EquippedItem } from '../api/types'
import type { LiveActionResponse, LiveEvent, LiveHero, LiveHeroState, LiveItem, LiveMonster, LivePartyMember, LiveSnapshot, LiveWelcome } from './types'
import './live.css'

const HeroPortrait3D = lazy(() => import('../scene3d/HeroPortrait3D'))

export type LiveMode = { kind: 'create' } | { kind: 'join'; code: string }

interface LiveGameProps {
  mode: LiveMode
  classId: string
  heroName: string
  onExit: () => void
}

type Phase = { kind: 'connecting' } | { kind: 'playing'; welcome: LiveWelcome } | { kind: 'closed'; reason: string }

const corpseMs = 760
const popupMs = 900
const attackKeys = new Set(['Space'])
const quickSlotCodes: Record<string, number> = { KeyQ: 0, KeyR: 1, KeyF: 2 }
const messageMs = 4000
const noNames: ReadonlyMap<number, string> = new Map()

interface Message {
  key: number
  text: string
  until: number
}

export function LiveGame({ mode, classId, heroName, onExit }: LiveGameProps) {
  const [phase, setPhase] = useState<Phase>({ kind: 'connecting' })
  const [heroes, setHeroes] = useState<LiveHero[]>([])
  const [monsters, setMonsters] = useState<LiveMonster[]>([])
  const [corpses, setCorpses] = useState<Corpse[]>([])
  const [popups, setPopups] = useState<Popup[]>([])
  const [projectiles, setProjectiles] = useState<MovingProjectile[]>([])
  const [blasts, setBlasts] = useState<Blast[]>([])
  const [heroCues, setHeroCues] = useState(new Map<number, Cue>())
  const [monsterCues, setMonsterCues] = useState(new Map<number, LiveCue>())
  const [heroState, setHeroState] = useState<LiveHeroState | null>(null)
  const [items, setItems] = useState<LiveItem[]>([])
  const [messages, setMessages] = useState<Message[]>([])
  const [inventoryOpen, setInventoryOpen] = useState(false)
  const [party, setParty] = useState<LivePartyMember[]>([])
  const pointer = useRef<GroundPoint | null>(null)
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
  const sent = useRef<MoveInput>({ x: 0, y: 0, attack: false, aimX: 0, aimY: 0 })

  const addMessages = useCallback((texts: string[]) => {
    const until = performance.now() + messageMs
    setMessages(current => [...current, ...texts.map(text => ({ key: ++cueId.current, text, until }))].slice(-4))
  }, [])

  const showHero = useCallback((hero: LiveHeroState) => {
    heroStateRef.current = hero
    setHeroState(hero)
  }, [])

  const applyEvents = useCallback((events: LiveEvent[], snapshot: LiveSnapshot) => {
    const welcome = welcomeRef.current
    if (!welcome || events.length === 0) return

    const nextHeroCues = new Map<number, Cue>()
    const nextMonsterCues = new Map<number, LiveCue>()
    const fallen: Corpse[] = []
    const fresh: Popup[] = []
    const nextBlasts: Blast[] = []
    const now = performance.now()
    const equipment = heroStateRef.current?.player.equipment ?? welcome.equipment
    const mainHand = equipment.find(equipped => equipped.slot === 'MainHand')?.item
    const offHand = equipment.find(equipped => equipped.slot === 'OffHand')?.item
    const notes: string[] = []
    const heroSpot = (heroId: number | undefined) => snapshot.heroes.find(candidate => candidate.id === heroId)
    const delayMs = renderDelaySeconds * 1000
    const cue = (clip: string, hold = false, durationMs?: number): LiveCue => ({ id: ++cueId.current, clip, delayMs, hold, durationMs, at: now + delayMs })
    const spotOf = (monsterId: number | undefined) =>
      snapshot.monsters.find(monster => monster.id === monsterId) ?? (monsterId !== undefined ? lastMonsters.current.get(monsterId) : undefined)

    for (const event of events) {
      switch (event.type) {
        case 'HeroSwung':
          if (event.heroId !== undefined) nextHeroCues.set(event.heroId, cue(heroAttackClip(mainHand, offHand)))
          break
        case 'MonsterWindup':
          if (event.monsterId !== undefined) nextMonsterCues.set(event.monsterId, cue('Windup', false, welcome.monsterWindupSeconds * 1000))
          break
        case 'MonsterHit': {
          const spot = spotOf(event.monsterId)
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
          if (event.monsterId !== undefined) nextMonsterCues.set(event.monsterId, cue('Death', true))
          if (spot) fallen.push({ monster: { ...spot, health: 0 }, until: now + corpseMs })
          break
        }
        case 'HeroHit': {
          const hero = snapshot.heroes.find(candidate => candidate.id === event.heroId)
          if (event.heroId !== undefined && !event.missed && hero && !hero.dead) nextHeroCues.set(event.heroId, cue('Hit_A'))
          if (hero) fresh.push({ key: ++cueId.current, x: hero.x, y: hero.y, text: event.missed ? 'Miss' : `-${event.amount ?? 0}`, kind: event.missed ? 'miss' : 'hurt', until: now + popupMs })
          break
        }
        case 'HeroFell':
          if (event.heroId !== undefined) nextHeroCues.set(event.heroId, cue('Death_A', true))
          notes.push('You fell. Your hero rises again at the entrance.')
          break
        case 'HeroRewarded': {
          const hero = heroSpot(event.heroId)
          if (hero) fresh.push({ key: ++cueId.current, x: hero.x, y: hero.y, text: `+${event.experience ?? 0} XP`, kind: 'reward', until: now + popupMs })
          if (event.levels) {
            notes.push(`Level up! You reached a new level and gained stat points.`)
            if (hero) fresh.push({ key: ++cueId.current, x: hero.x, y: hero.y, text: 'Level up!', kind: 'level', until: now + popupMs * 2 })
          }
          break
        }
        case 'LootPickedUp':
          if (event.itemName) notes.push(`You pick up ${event.itemName}.`)
          break
        case 'BackpackFull':
          notes.push('Your backpack is full: press Tab to make room.')
          break
        case 'BoostFaded':
          if (event.itemName) notes.push(`The effect of ${event.itemName} wears off.`)
          break
        case 'AbilityActivated': {
          const hero = heroSpot(event.heroId)
          if (event.heroId !== undefined) nextHeroCues.set(event.heroId, cue('Spellcast_Raise'))
          if (hero) fresh.push({ key: ++cueId.current, x: hero.x, y: hero.y, text: `${event.ability ?? ''}!`, kind: 'level', until: now + popupMs * 1.5 })
          break
        }
        case 'AbilityFaded':
          if (event.heroId === welcome.heroId && event.ability) notes.push(`${event.ability} fades.`)
          break
        case 'MonsterScorched': {
          const spot = spotOf(event.monsterId)
          if (spot) fresh.push({ key: ++cueId.current, x: spot.x, y: spot.y, text: `${event.amount ?? 0}`, kind: 'burn', until: now + popupMs })
          break
        }
        case 'MonsterFrozen':
        case 'MonsterIgnited': {
          const spot = spotOf(event.monsterId)
          if (spot) fresh.push({ key: ++cueId.current, x: spot.x, y: spot.y, text: event.type === 'MonsterFrozen' ? 'Frozen' : 'Burning', kind: event.type === 'MonsterFrozen' ? 'frozen' : 'burn', until: now + popupMs })
          break
        }
        case 'SpellUnleashed':
          if (event.x !== undefined && event.y !== undefined)
            nextBlasts.push({ key: ++cueId.current, x: event.x, y: event.y, radius: event.radius ?? 0.6, element: event.element, start: now + delayMs })
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
    if (nextBlasts.length > 0) setBlasts(current => [...current.filter(blast => blast.start > now - 1000), ...nextBlasts])
  }, [addMessages])

  useEffect(() => {
    let active = true
    const room = new RoomConnection({
      onSnapshot: (snapshot, receivedAt) => {
        heroBuffer.push(snapshot.time, snapshot.heroes, receivedAt)
        monsterBuffer.push(snapshot.time, snapshot.monsters, receivedAt)
        const flying = snapshot.projectiles.map(projectile => ({ ...projectile, moving: true }))
        projectileBuffer.push(snapshot.time, flying, receivedAt)
        setProjectiles(current => (current.length === 0 && flying.length === 0 ? current : flying))
        if (snapshot.hero) showHero(snapshot.hero)
        if (snapshot.items) setItems(snapshot.items)
        if (snapshot.party) setParty(snapshot.party)
        applyEvents(snapshot.events, snapshot)
        lastMonsters.current = new Map(snapshot.monsters.map(monster => [monster.id, monster]))
        setHeroes(snapshot.heroes)
        setMonsters(snapshot.monsters)
      },
      onClosed: reason => {
        if (active) setPhase({ kind: 'closed', reason })
      },
    })
    connection.current = room

    const opening = window.setTimeout(() => {
      const entering = mode.kind === 'join' ? room.join(mode.code, classId, heroName) : room.create(classId, heroName)
      entering
      .then(welcome => {
        if (!active) return
        welcomeRef.current = welcome
        setParty(welcome.party)
        showHero(welcome.hero)
        setItems(welcome.items)
        setPhase({ kind: 'playing', welcome })
      })
      .catch((error: unknown) => {
        if (active) setPhase({ kind: 'closed', reason: error instanceof Error ? error.message : 'Could not open the room.' })
      })
    }, 0)

    return () => {
      active = false
      window.clearTimeout(opening)
      void room.leave()
      connection.current = null
    }
  }, [mode, classId, heroName, applyEvents, showHero, heroBuffer, monsterBuffer, projectileBuffer])

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
  }, [])

  const pushInput = useCallback(() => {
    const keys = moveVector(pressed.current)
    const move = keys.x !== 0 || keys.y !== 0 ? keys : touchMove.current
    const welcome = welcomeRef.current
    const here = welcome ? heroBuffer.sample(welcome.heroId, performance.now()) : undefined
    const aim = here ? aimToward(here, pointer.current) : { aimX: 0, aimY: 0 }
    const input = { ...move, ...aim, attack: attacking.current.key || attacking.current.mouse }
    if (sameInput(input, sent.current)) return
    sent.current = input
    connection.current?.send(input)
  }, [heroBuffer])

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
          setInventoryOpen(open => !open)
        }
        return
      }
      if (inventoryOpen) {
        if (event.code === 'Escape') setInventoryOpen(false)
        return
      }
      if (event.code === 'Escape') {
        onExit()
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
      else return
      event.preventDefault()
      pushInput()
    }
    const handleUp = (event: KeyboardEvent) => {
      if (inventoryOpen) return
      if (isMoveKey(event.code)) pressed.current.delete(event.code)
      else if (attackKeys.has(event.code)) attacking.current.key = false
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
  }, [phase.kind, onExit, pushInput, inventoryOpen, act, releaseAll])

  const setMouseAttack = (down: boolean) => {
    if (inventoryOpen) return
    attacking.current.mouse = down
    pushInput()
  }

  if (phase.kind === 'connecting') return <div className="live-message">Opening the vault…</div>

  if (phase.kind === 'closed')
    return (
      <div className="live-message">
        <p>{phase.reason}</p>
        <button className="primary" onClick={onExit}>
          Back
        </button>
      </div>
    )

  const me = heroes.find(hero => hero.id === phase.welcome.heroId)
  const player = heroState && me ? { ...heroState.player, health: me.health, maxHealth: me.maxHealth, mana: me.mana, maxMana: me.maxMana } : heroState?.player
  const equipment = heroState?.player.equipment ?? phase.welcome.equipment
  const partyGear = new Map<number, EquippedItem[]>(party.map(member => [member.heroId, member.equipment]))

  return (
    <div
      className="live-game"
      onPointerDown={event => {
        if (event.pointerType === 'mouse' && event.button === 0) setMouseAttack(true)
      }}
      onPointerUp={event => {
        if (event.pointerType === 'mouse') setMouseAttack(false)
      }}
      onPointerLeave={event => {
        if (event.pointerType === 'mouse') setMouseAttack(false)
      }}
    >
      <LiveScene
        welcome={phase.welcome}
        equipment={equipment}
        party={partyGear}
        pointer={pointer}
        items={items}
        heroes={heroes}
        monsters={monsters}
        corpses={corpses}
        heroBuffer={heroBuffer}
        monsterBuffer={monsterBuffer}
        projectiles={projectiles}
        projectileBuffer={projectileBuffer}
        blasts={blasts}
        heroCues={heroCues}
        monsterCues={monsterCues}
        popups={popups}
      />
      {player && heroState && (
        <div className="live-status" onPointerDown={event => event.stopPropagation()}>
          <StatusBar
            player={player}
            deaths={heroState.deaths}
            floor={heroState.floor}
            floorCount={heroState.floorCount}
            biome={phase.welcome.biome}
            difficulty={heroState.difficulty}
            onOpenInventory={() => {
              releaseAll()
              pushInput()
              setInventoryOpen(true)
            }}
          />
        </div>
      )}
      <div className="live-party" onPointerDown={event => event.stopPropagation()}>
        <span>
          World code <strong>{phase.welcome.roomCode}</strong> · {party.length}/{phase.welcome.maxHeroes}
        </span>
        <ul>
          {party.map(member => (
            <li key={member.heroId} className={member.heroId === phase.welcome.heroId ? 'me' : undefined}>
              {member.name}
              {member.isHost ? ' ★' : ''}
            </li>
          ))}
        </ul>
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
          {messages.map(message => (
            <li key={message.key}>{message.text}</li>
          ))}
        </ul>
      )}
      {me?.dead && <div className="live-fallen">You fell. Rising at the entrance…</div>}
      <p className="live-hint">Real-time prototype · WASD or arrows to move · Space or click to attack · E ability · 1–4 spells · Q R F quick slots · Tab backpack · Esc to leave</p>
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
            onPreview={(entryId: number, hand?: EquipmentSlot) => connection.current?.preview(entryId, hand) ?? Promise.reject(new Error('Not connected.'))}
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
