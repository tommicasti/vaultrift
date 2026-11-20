import { useCallback, useEffect, useRef, useState } from 'react'
import {
  ApiError,
  activateAbility,
  attack,
  buyBackpackUpgrade,
  buyOffer,
  consumeItem,
  consumeQuickSlot,
  createGame,
  discardItem,
  discardJunk,
  equip,
  face,
  getGame,
  getGameByCode,
  previewEquip,
  learnSpell,
  markItem,
  move,
  selectSpell,
  sellItem,
  sellJunk,
  setQuickSlot,
  spendPoint,
  unequip,
} from './api/gamesApi'
import type { Difficulty, Direction, EquipmentSlot, GameState, ItemMark, StatPoint, TurnResponse } from './api/types'
import { Board } from './components/Board'
import { ShopScreen } from './components/ShopScreen'
import { SpellBar } from './components/SpellBar'
import { ClassSelect } from './components/ClassSelect'
import { ResumePanel } from './components/ResumePanel'
import { SaveCodeChip } from './components/SaveCodeChip'
import { Controls } from './components/Controls'
import { EventLog } from './components/EventLog'
import { HeaderToggles } from './components/HeaderToggles'
import { InventoryScreen } from './components/InventoryScreen'
import { StatusBar } from './components/StatusBar'
import { VictoryScreen } from './components/VictoryScreen'
import { HeroLostScreen } from './components/HeroLostScreen'
import { describeEvent } from './game/describeEvent'
import { effectsDuration, effectsForTurn, type Effect } from './game/effects'
import { forgetHero, loadHeroes, rememberHero, type SavedHero } from './game/savedHeroes'
import { useCompactLandscape } from './game/useCompactLandscape'
import './App.css'

const keyDirections: Record<string, Direction> = {
  KeyW: 'Up',
  KeyS: 'Down',
  KeyA: 'Left',
  KeyD: 'Right',
}

const maxLogEntries = 30
const stepIntervalMs = 120
const compactView = { columns: 11, rows: 7 }
const standardView = { columns: 13, rows: 11 }

const difficultyIntro: Record<Difficulty, string> = {
  Normal: 'You enter the vault. It is dark.',
  Hard: 'You enter the vault. It is dark, and the monsters here are stronger.',
  Hardcore: 'You enter the vault. It is dark. You will only get one chance.',
}

const incompatibleSaveMessage =
  'This save comes from an older version of Vaultrift and can no longer be loaded. Start a new adventure!'

function App() {
  const [game, setGame] = useState<GameState | null>(null)
  const [log, setLog] = useState<string[]>([])
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [inventoryOpen, setInventoryOpen] = useState(false)
  const [shopOpen, setShopOpen] = useState(false)
  const [effects, setEffects] = useState<Effect[]>([])
  const queuedMove = useRef<Direction | null>(null)
  const lastMoveAt = useRef(0)
  const moveTimer = useRef<number | null>(null)
  const [moveTick, setMoveTick] = useState(0)
  const heldKey = useRef<string | null>(null)
  const holdTimer = useRef<number | null>(null)
  const moveRef = useRef<(direction: Direction) => void>(() => undefined)
  const [victoryDismissed, setVictoryDismissed] = useState(false)
  const [heroes, setHeroes] = useState<SavedHero[]>(loadHeroes)
  const [lostDismissed, setLostDismissed] = useState(false)
  const turnCounter = useRef(0)
  const compact = useCompactLandscape()
  const view = compact ? compactView : standardView
  const hasGame = game !== null

  function enterGame(state: GameState, message: string) {
    setGame(state)
    setLog([message])
    setInventoryOpen(false)
    setShopOpen(false)
    setVictoryDismissed(state.isConquered)
    setLostDismissed(false)
  }

  async function startGame(classId: string, name: string, difficulty: Difficulty) {
    setBusy(true)
    setError(null)

    try {
      enterGame(await createGame(classId, name, difficulty), difficultyIntro[difficulty])
    } catch {
      setError('Could not start a new game. Is the API running?')
    } finally {
      setBusy(false)
    }
  }

  async function continueGame(hero: SavedHero) {
    setBusy(true)
    setError(null)

    try {
      const state = await getGame(hero.id).catch(failure => {
        if (failure instanceof ApiError && failure.status === 404) return getGameByCode(hero.code)
        throw failure
      })
      enterGame(state, state.isLost ? `${state.player.name} rests here forever.` : 'You return to the vault.')
    } catch (failure) {
      if (failure instanceof ApiError && (failure.status === 404 || failure.status === 409)) {
        forgetHero(hero.id)
        setHeroes(loadHeroes())
        setError(failure.status === 409 ? incompatibleSaveMessage : 'That save no longer exists.')
      } else {
        setError('Could not reach the vault. Try again in a moment.')
      }
    } finally {
      setBusy(false)
    }
  }

  async function loadByCode(code: string) {
    setBusy(true)
    setError(null)

    try {
      enterGame(await getGameByCode(code), 'You return to the vault.')
    } catch (failure) {
      setError(
        failure instanceof ApiError && failure.status === 404
          ? 'No game has that save code.'
          : failure instanceof ApiError && failure.status === 409
            ? incompatibleSaveMessage
            : 'Could not reach the vault. Try again in a moment.',
      )
    } finally {
      setBusy(false)
    }
  }

  function returnToClassSelect() {
    setGame(null)
    setLog([])
    setError(null)
    setInventoryOpen(false)
    setShopOpen(false)
    setHeroes(loadHeroes())
  }

  function handleForget(hero: SavedHero) {
    forgetHero(hero.id)
    setHeroes(loadHeroes())
  }

  useEffect(() => {
    if (game) rememberHero(game)
  }, [game])

  const play = useCallback(
    async (action: (gameId: string) => Promise<TurnResponse>) => {
      if (!game || busy || game.isLost) return

      setBusy(true)
      setError(null)

      try {
        const turn = await action(game.id)
        const enemyNames = new Map(
          [...game.enemies, ...turn.state.enemies].map(enemy => [enemy.id, enemy.name]),
        )
        const messages = turn.events
          .map(gameEvent => describeEvent(gameEvent, enemyNames))
          .filter((message): message is string => message !== null)

        turnCounter.current++
        setEffects(effectsForTurn(turn.events, game.enemies, turn.state.enemies, turn.state.player, turnCounter.current))
        setGame(turn.state)

        if (turn.events.some(gameEvent => gameEvent.type === 'MerchantGreeted')) setShopOpen(true)
        else if (!turn.state.merchant?.isNearby) setShopOpen(false)

        setLog(previous => [...messages.reverse(), ...previous].slice(0, maxLogEntries))
      } catch {
        setError('The action failed. Try again.')
      } finally {
        setBusy(false)
      }
    },
    [game, busy],
  )

  const handleMove = useCallback(
    (direction: Direction) => {
      const wait = stepIntervalMs - (performance.now() - lastMoveAt.current)

      if (busy || wait > 0) {
        queuedMove.current = direction

        if (!busy && moveTimer.current === null)
          moveTimer.current = window.setTimeout(() => {
            moveTimer.current = null
            setMoveTick(tick => tick + 1)
          }, wait)

        return
      }

      lastMoveAt.current = performance.now()
      void play(gameId => move(gameId, direction))
    },
    [play, busy],
  )

  useEffect(() => {
    moveRef.current = handleMove
  }, [handleMove])

  useEffect(() => {
    function stopHolding() {
      heldKey.current = null
      if (holdTimer.current !== null) window.clearInterval(holdTimer.current)
      holdTimer.current = null
    }

    const handleKeyUp = (event: KeyboardEvent) => {
      if (event.code === heldKey.current) stopHolding()
    }

    window.addEventListener('keyup', handleKeyUp)
    window.addEventListener('blur', stopHolding)
    return () => {
      window.removeEventListener('keyup', handleKeyUp)
      window.removeEventListener('blur', stopHolding)
      stopHolding()
    }
  }, [])

  useEffect(() => {
    if (busy || queuedMove.current === null) return

    const direction = queuedMove.current
    queuedMove.current = null
    handleMove(direction)
  }, [busy, handleMove, moveTick])

  const gameId = game?.id

  const handlePreview = useCallback(
    (entryId: number, hand?: EquipmentSlot) =>
      gameId ? previewEquip(gameId, entryId, hand) : Promise.reject(new Error('No game')),
    [gameId],
  )

  const handleFace = useCallback(
    (direction: Direction) => void play(gameId => face(gameId, direction)),
    [play],
  )

  const handleAttack = useCallback(() => void play(attack), [play])

  const handleEquip = useCallback(
    (entryId: number, hand?: EquipmentSlot) => void play(gameId => equip(gameId, entryId, hand)),
    [play],
  )

  const handleUnequip = useCallback(
    (slot: EquipmentSlot) => void play(gameId => unequip(gameId, slot)),
    [play],
  )

  const handleUse = useCallback(
    (entryId: number) => void play(gameId => consumeItem(gameId, entryId)),
    [play],
  )

  const handleAssignQuickSlot = useCallback(
    (entryId: number, slot: number) => void play(gameId => setQuickSlot(gameId, entryId, slot)),
    [play],
  )

  const handleUseQuickSlot = useCallback(
    (slot: number) => void play(gameId => consumeQuickSlot(gameId, slot)),
    [play],
  )

  const handleSelectSpell = useCallback(
    (index: number) => void play(gameId => selectSpell(gameId, index)),
    [play],
  )

  const handleLearn = useCallback(
    (entryId: number, key: number) => void play(gameId => learnSpell(gameId, entryId, key)),
    [play],
  )

  const handleBuy = useCallback((offerId: number) => void play(gameId => buyOffer(gameId, offerId)), [play])

  const handleBuyUpgrade = useCallback(() => void play(buyBackpackUpgrade), [play])

  const handleSell = useCallback((entryId: number) => void play(gameId => sellItem(gameId, entryId)), [play])

  const handleSellJunk = useCallback(() => void play(sellJunk), [play])

  const spellCount = game?.player.spells.length ?? 0

  const handleUseAbility = useCallback(() => void play(activateAbility), [play])

  const handleDiscard = useCallback(
    (entryId: number) => void play(gameId => discardItem(gameId, entryId)),
    [play],
  )

  const handleDiscardJunk = useCallback(() => void play(discardJunk), [play])

  const handleMark = useCallback(
    (entryId: number, mark: ItemMark) => void play(gameId => markItem(gameId, entryId, mark)),
    [play],
  )

  const handleSpendPoint = useCallback(
    (stat: StatPoint) => void play(gameId => spendPoint(gameId, stat)),
    [play],
  )

  useEffect(() => {
    if (effects.length === 0) return

    const timer = window.setTimeout(() => setEffects([]), effectsDuration(effects))
    return () => window.clearTimeout(timer)
  }, [effects])

  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if (!hasGame) return

      if (shopOpen) {
        if (event.code === 'Escape') setShopOpen(false)
        if (event.code === 'Space' || event.code === 'Tab') event.preventDefault()
        return
      }

      if (event.code === 'Tab') {
        event.preventDefault()
        setInventoryOpen(open => !open)
        return
      }

      if (inventoryOpen) {
        if (event.code === 'Escape') setInventoryOpen(false)
        if (event.code === 'Space') event.preventDefault()
        return
      }

      const direction = keyDirections[event.code]

      if (direction) {
        event.preventDefault()
        if (event.repeat) return

        if (event.shiftKey) {
          handleFace(direction)
          return
        }

        heldKey.current = event.code
        handleMove(direction)
        if (holdTimer.current !== null) window.clearInterval(holdTimer.current)
        holdTimer.current = window.setInterval(() => {
          const held = heldKey.current ? keyDirections[heldKey.current] : undefined
          if (held && !document.querySelector('[role="dialog"]')) moveRef.current(held)
        }, stepIntervalMs)
      } else if (event.code === 'Space') {
        event.preventDefault()

        if (event.target instanceof HTMLElement)
          event.target.blur()

        handleAttack()
      } else if (event.code === 'KeyQ') {
        event.preventDefault()
        handleUseQuickSlot(0)
      } else if (event.code === 'KeyR') {
        event.preventDefault()
        handleUseQuickSlot(1)
      } else if (event.code === 'KeyF') {
        event.preventDefault()
        handleUseQuickSlot(2)
      } else if (/^Digit[1-4]$/.test(event.code) && Number(event.code.slice(5)) <= spellCount) {
        event.preventDefault()
        handleSelectSpell(Number(event.code.slice(5)) - 1)
      } else if (event.code === 'KeyE') {
        event.preventDefault()
        handleUseAbility()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [hasGame, inventoryOpen, shopOpen, spellCount, handleMove, handleFace, handleAttack, handleUseQuickSlot, handleUseAbility, handleSelectSpell])

  if (!game) {
    return (
      <main className="app start">
        <h1>Vaultrift</h1>
        <ResumePanel
          heroes={heroes}
          disabled={busy}
          onContinue={hero => void continueGame(hero)}
          onForget={handleForget}
          onLoadCode={code => void loadByCode(code)}
        />
        <p className="label">{heroes.length > 0 ? 'Or start a new adventure' : 'Choose your hero'}</p>
        {error && <p className="error">{error}</p>}
        <ClassSelect disabled={busy} onChoose={(classId, name, difficulty) => void startGame(classId, name, difficulty)} />
      </main>
    )
  }

  return (
    <main className="app">
      <header className="app-header">
        <h1>Vaultrift</h1>
        <div className="header-actions">
          <SaveCodeChip code={game.saveCode} />
          <HeaderToggles />
          {game.merchant?.isNearby && <button onClick={() => setShopOpen(true)}>Trade</button>}
          <button onClick={() => setInventoryOpen(true)}>Backpack</button>
          <button onClick={returnToClassSelect} disabled={busy}>
            Menu
          </button>
        </div>
      </header>
      <StatusBar
        player={game.player}
        deaths={game.deaths}
        floor={game.floor}
        floorCount={game.floorCount}
        biome={game.biome}
        difficulty={game.difficulty}
        onOpenInventory={() => setInventoryOpen(true)}
      />
      <SpellBar
        spells={game.player.spells}
        active={game.player.activeSpell}
        mana={game.player.mana}
        overloaded={game.player.isOverloaded}
        disabled={busy}
        onSelect={handleSelectSpell}
      />
      <Board game={game} viewColumns={view.columns} viewRows={view.rows} effects={effects} onAttack={handleAttack} />
      <p className="keyboard-hint">WASD to move · Shift+WASD to turn · Space or click to attack · E ability · Q R potions · F scroll · 1–4 spells · Tab backpack</p>
      <Controls
        disabled={game.isLost}
        quickSlots={game.player.quickSlots}
        ability={game.player.ability}
        onMove={handleMove}
        onFace={handleFace}
        onAttack={handleAttack}
        onUseQuickSlot={handleUseQuickSlot}
        onUseAbility={handleUseAbility}
      />
      {error && <p className="error">{error}</p>}
      <EventLog entries={log} />
      {shopOpen && game.merchant && (
        <ShopScreen
          merchant={game.merchant}
          coins={game.player.coins}
          entries={game.inventory}
          capacity={game.inventoryCapacity}
          disabled={busy}
          onBuy={handleBuy}
          onBuyUpgrade={handleBuyUpgrade}
          onSell={handleSell}
          onSellJunk={handleSellJunk}
          onClose={() => setShopOpen(false)}
        />
      )}
      {inventoryOpen && (
        <InventoryScreen
          player={game.player}
          entries={game.inventory}
          capacity={game.inventoryCapacity}
          disabled={busy}
          onEquip={handleEquip}
          onUnequip={handleUnequip}
          onUse={handleUse}
          onAssignQuickSlot={handleAssignQuickSlot}
          onSpendPoint={handleSpendPoint}
          onDiscard={handleDiscard}
          onDiscardJunk={handleDiscardJunk}
          onLearn={handleLearn}
          onMark={handleMark}
          onPreview={handlePreview}
          onClose={() => setInventoryOpen(false)}
        />
      )}
      {game.isConquered && !victoryDismissed && (
        <VictoryScreen
          heroName={game.player.name}
          deaths={game.deaths}
          onKeepExploring={() => setVictoryDismissed(true)}
          onNewGame={returnToClassSelect}
        />
      )}
      {game.isLost && !lostDismissed && (
        <HeroLostScreen
          heroName={game.player.name}
          floor={game.floor}
          level={game.player.level}
          onLookAround={() => setLostDismissed(true)}
          onNewGame={returnToClassSelect}
        />
      )}
    </main>
  )
}

export default App