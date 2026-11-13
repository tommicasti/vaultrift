import { useCallback, useEffect, useState } from 'react'
import { attack, consumeItem, consumeQuickSlot, createGame, equip, move, setQuickSlot } from './api/gamesApi'
import type { Direction, GameState, TurnResponse } from './api/types'
import { Board } from './components/Board'
import { Controls } from './components/Controls'
import { EventLog } from './components/EventLog'
import { InventoryScreen } from './components/InventoryScreen'
import { StatusBar } from './components/StatusBar'
import { describeEvent } from './game/describeEvent'
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
  const compact = useCompactLandscape()
  const view = compact ? compactView : standardView
  const hasGame = game !== null

  async function startGame() {
    setBusy(true)
    setError(null)

    try {
      setGame(await createGame())
      setLog(['You enter the vault. It is dark.'])
      setInventoryOpen(false)
    } catch {
      setError('Could not start a new game. Is the API running?')
    } finally {
      setBusy(false)
    }
  }

  const play = useCallback(
    async (action: (gameId: string) => Promise<TurnResponse>) => {
      if (!game || busy) return

      setBusy(true)
      setError(null)

      try {
        const turn = await action(game.id)
        const messages = turn.events
          .map(describeEvent)
          .filter((message): message is string => message !== null)

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
    function handleKeyDown(event: KeyboardEvent) {
      if (!hasGame) return

      if (event.code === 'KeyI') {
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
        <button className="primary" onClick={startGame} disabled={busy}>
          New game
        </button>
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
          <button onClick={startGame} disabled={busy}>
            New game
          </button>
        </div>
      </header>
      <StatusBar player={game.player} deaths={game.deaths} />
      <Board game={game} viewColumns={view.columns} viewRows={view.rows} onAttack={handleAttack} />
      <p className="keyboard-hint">WASD to move · Space or click to attack · Q quick slot · I backpack</p>
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
          onUse={handleUse}
          onAssignQuickSlot={handleAssignQuickSlot}
          onClose={() => setInventoryOpen(false)}
        />
      )}
    </main>
  )
}

export default App
