import { useCallback, useEffect, useRef, useState } from 'react'
import { attack, consumeItem, consumeQuickSlot, createGame, equip, move, setQuickSlot, unequip } from './api/gamesApi'
import type { Direction, EquipmentSlot, GameState, TurnResponse } from './api/types'
import { Board } from './components/Board'
import { ClassSelect } from './components/ClassSelect'
import { Controls } from './components/Controls'
import { EventLog } from './components/EventLog'
import { InventoryScreen } from './components/InventoryScreen'
import { StatusBar } from './components/StatusBar'
import { VictoryScreen } from './components/VictoryScreen'
import { describeEvent } from './game/describeEvent'
import { effectsDuration, effectsForTurn, type Effect } from './game/effects'
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
  const [effects, setEffects] = useState<Effect[]>([])
  const [victoryDismissed, setVictoryDismissed] = useState(false)
  const turnCounter = useRef(0)
  const compact = useCompactLandscape()
  const view = compact ? compactView : standardView
  const hasGame = game !== null

  async function startGame(classId: string, name: string) {
    setBusy(true)
    setError(null)

    try {
      setGame(await createGame(classId, name))
      setLog(['You enter the vault. It is dark.'])
      setInventoryOpen(false)
      setVictoryDismissed(false)
    } catch {
      setError('Could not start a new game. Is the API running?')
    } finally {
      setBusy(false)
    }
  }

  function returnToClassSelect() {
    setGame(null)
    setLog([])
    setError(null)
    setInventoryOpen(false)
  }

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
    (entryId: number) => void play(gameId => equip(gameId, entryId)),
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
    (entryId: number) => void play(gameId => setQuickSlot(gameId, entryId)),
    [play],
  )

  const handleUseQuickSlot = useCallback(() => void play(consumeQuickSlot), [play])

  useEffect(() => {
    if (effects.length === 0) return

    const timer = window.setTimeout(() => setEffects([]), effectsDuration(effects))
    return () => window.clearTimeout(timer)
  }, [effects])

  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if (!hasGame) return

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
        handleUseQuickSlot()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [hasGame, inventoryOpen, handleMove, handleAttack, handleUseQuickSlot])

  if (!game) {
    return (
      <main className="app start">
        <h1>Vaultrift</h1>
        <p className="label">Choose your hero</p>
        <ClassSelect disabled={busy} onChoose={(classId, name) => void startGame(classId, name)} />
        {error && <p className="error">{error}</p>}
      </main>
    )
  }

  return (
    <main className="app">
      <header className="app-header">
        <h1>Vaultrift</h1>
        <div className="header-actions">
          <button onClick={() => setInventoryOpen(true)}>Backpack</button>
          <button onClick={returnToClassSelect} disabled={busy}>
            New game
          </button>
        </div>
      </header>
      <StatusBar player={game.player} deaths={game.deaths} floor={game.floor} floorCount={game.floorCount} />
      <Board game={game} viewColumns={view.columns} viewRows={view.rows} effects={effects} onAttack={handleAttack} />
      <p className="keyboard-hint">WASD to move · Space or click to attack · Q quick slot · Tab backpack</p>
      <Controls
        disabled={busy}
        quickSlot={game.player.quickSlot}
        quickSlotQuantity={game.player.quickSlotQuantity}
        onMove={handleMove}
        onAttack={handleAttack}
        onUseQuickSlot={handleUseQuickSlot}
      />
      {error && <p className="error">{error}</p>}
      <EventLog entries={log} />
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