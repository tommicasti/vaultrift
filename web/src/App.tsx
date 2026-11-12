import { useCallback, useEffect, useState } from 'react'
import { attack, createGame, move } from './api/gamesApi'
import type { Direction, GameState, TurnResponse } from './api/types'
import { Board } from './components/Board'
import { Controls } from './components/Controls'
import { EventLog } from './components/EventLog'
import { Inventory } from './components/Inventory'
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
  const compact = useCompactLandscape()
  const view = compact ? compactView : standardView

  async function startGame() {
    setBusy(true)
    setError(null)

    try {
      setGame(await createGame())
      setLog(['You enter the vault. It is dark.'])
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

  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      const direction = keyDirections[event.code]

      if (direction) {
        event.preventDefault()
        handleMove(direction)
      } else if (event.code === 'Space') {
        event.preventDefault()

        if (event.target instanceof HTMLElement)
          event.target.blur()

        handleAttack()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [handleMove, handleAttack])

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
        <button onClick={startGame} disabled={busy}>
          New game
        </button>
      </header>
      <StatusBar player={game.player} deaths={game.deaths} />
      <Board game={game} viewColumns={view.columns} viewRows={view.rows} onAttack={handleAttack} />
      <p className="keyboard-hint">WASD to move · Space or click to attack</p>
      <Controls disabled={busy} onMove={handleMove} onAttack={handleAttack} />
      {error && <p className="error">{error}</p>}
      <EventLog entries={log} />
      <Inventory entries={game.inventory} capacity={game.inventoryCapacity} />
    </main>
  )
}

export default App