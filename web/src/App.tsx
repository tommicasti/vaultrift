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
  getGame,
  getGameByCode,
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
import type { Direction, EquipmentSlot, GameState, ItemMark, StatPoint, TurnResponse } from './api/types'
import { Board } from './components/Board'
import { ShopScreen } from './components/ShopScreen'
import { SpellBar } from './components/SpellBar'
import { ClassSelect } from './components/ClassSelect'
import { ResumePanel } from './components/ResumePanel'
import { SaveCodeChip } from './components/SaveCodeChip'
import { Controls } from './components/Controls'
import { EventLog } from './components/EventLog'
import { InventoryScreen } from './components/InventoryScreen'
import { StatusBar } from './components/StatusBar'
import { VictoryScreen } from './components/VictoryScreen'
import { describeEvent } from './game/describeEvent'
import { effectsDuration, effectsForTurn, type Effect } from './game/effects'
import { forgetSavedGame, loadSavedGame, rememberGame, type SavedGame } from './game/savedGame'
import { useCompactLandscape } from './game/useCompactLandscape'
import './App.css'

const keyDirections: Record<string, Direction> = {
  KeyW: 'Up',
  KeyS: 'Down',
  KeyA: 'Left',
  KeyD: 'Right',
}

const maxLogEntries = 30
const compactView = { columns: 11, rows: 7 }
const standardView = { columns: 13, rows: 11 }

function App() {
  const [game, setGame] = useState<GameState | null>(null)
  const [log, setLog] = useState<string[]>([])
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [inventoryOpen, setInventoryOpen] = useState(false)
  const [shopOpen, setShopOpen] = useState(false)
  const [effects, setEffects] = useState<Effect[]>([])
  const [victoryDismissed, setVictoryDismissed] = useState(false)
  const [saved, setSaved] = useState<SavedGame | null>(loadSavedGame)
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
  }

  async function startGame(classId: string, name: string) {
    setBusy(true)
    setError(null)

    try {
      enterGame(await createGame(classId, name), 'You enter the vault. It is dark.')
    } catch {
      setError('Could not start a new game. Is the API running?')
    } finally {
      setBusy(false)
    }
  }

  async function continueGame(savedGame: SavedGame) {
    setBusy(true)
    setError(null)

    try {
      const state = await getGame(savedGame.id).catch(failure => {
        if (failure instanceof ApiError && failure.status === 404) return getGameByCode(savedGame.code)
        throw failure
      })
      enterGame(state, 'You return to the vault.')
    } catch (failure) {
      if (failure instanceof ApiError && failure.status === 404) {
        forgetSavedGame()
        setSaved(null)
        setError('That save no longer exists.')
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
    setSaved(loadSavedGame())
  }

  useEffect(() => {
    if (game) rememberGame(game)
  }, [game])

  const play = useCallback(
    async (action: (gameId: string) => Promise<TurnResponse>) => {
      if (!game || busy) return

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
    (direction: Direction) => void play(gameId => move(gameId, direction)),
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
        handleMove(direction)
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
  }, [hasGame, inventoryOpen, shopOpen, spellCount, handleMove, handleAttack, handleUseQuickSlot, handleUseAbility, handleSelectSpell])

  if (!game) {
    return (
      <main className="app start">
        <h1>Vaultrift</h1>
        <ResumePanel
          saved={saved}
          disabled={busy}
          onContinue={savedGame => void continueGame(savedGame)}
          onLoadCode={code => void loadByCode(code)}
        />
        <p className="label">{saved ? 'Or start a new adventure' : 'Choose your hero'}</p>
        {error && <p className="error">{error}</p>}
        <ClassSelect disabled={busy} onChoose={(classId, name) => void startGame(classId, name)} />
      </main>
    )
  }

  return (
    <main className="app">
      <header className="app-header">
        <h1>Vaultrift</h1>
        <div className="header-actions">
          <SaveCodeChip code={game.saveCode} />
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
      <p className="keyboard-hint">WASD to move · Space or click to attack · E ability · Q R potions · F scroll · 1–4 spells · Tab backpack</p>
      <Controls
        disabled={busy}
        quickSlots={game.player.quickSlots}
        ability={game.player.ability}
        onMove={handleMove}
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
    </main>
  )
}

export default App