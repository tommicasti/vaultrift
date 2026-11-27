import { useCallback, useEffect, useState } from 'react'
import { claimCodes, getAccount, logout, type AccountInfo } from './api/accountApi'
import type { Difficulty } from './api/types'
import { createHero, createWorld, deleteHero, deleteWorld, getVault, VaultError, type VaultHero, type VaultInfo, type VaultWorld } from './api/vaultApi'
import { HomeScreen } from './components/HomeScreen'
import { clearLocalHeroes, localSaveCodes } from './game/savedHeroes'
import { LiveGame, type LiveMode } from './realtime/LiveGame'
import './App.css'

const unreachable = 'Could not reach the vault. Try again in a moment.'
const emptyVault: VaultInfo = { heroes: [], worlds: [] }

function App() {
  const [account, setAccount] = useState<AccountInfo | null | undefined>(undefined)
  const [vault, setVault] = useState<VaultInfo>(emptyVault)
  const [live, setLive] = useState<{ mode: LiveMode; heroId: string } | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  const adoptAccount = useCallback(async (info: AccountInfo | null) => {
    setAccount(info)
    if (!info) {
      setVault(emptyVault)
      return
    }
    if (info.claimed > 0) setNotice(`${info.claimed} ${info.claimed === 1 ? 'hero is' : 'heroes are'} now linked to your account.`)
    try {
      setVault(await getVault())
      setError(null)
    } catch (failure) {
      setError(failure instanceof VaultError ? failure.message : unreachable)
    }
  }, [])

  const handleSignedIn = useCallback(
    (info: AccountInfo) => {
      clearLocalHeroes()
      void adoptAccount(info)
    },
    [adoptAccount],
  )

  const atHome = live === null

  useEffect(() => {
    if (!atHome) return
    let cancelled = false
    getAccount()
      .then(async info => {
        const codes = info ? localSaveCodes() : []
        const linked = codes.length > 0 ? await claimCodes(codes) : info
        if (codes.length > 0) clearLocalHeroes()
        if (!cancelled) await adoptAccount(linked)
      })
      .catch(() => {
        if (!cancelled) setAccount(current => current ?? null)
      })
    return () => {
      cancelled = true
    }
  }, [adoptAccount, atHome])

  const handleSignOut = useCallback(() => {
    void logout()
      .catch(() => undefined)
      .finally(() => {
        setAccount(null)
        setVault(emptyVault)
        setNotice(null)
      })
  }, [])

  async function guarded<T>(work: () => Promise<T>): Promise<T | null> {
    setBusy(true)
    setError(null)
    try {
      return await work()
    } catch (failure) {
      if (failure instanceof VaultError && failure.status === 401) {
        setAccount(null)
        setError('Your session has expired. Sign in again.')
      } else setError(failure instanceof VaultError ? failure.message : unreachable)
      return null
    } finally {
      setBusy(false)
    }
  }

  async function newHero(classId: string, name: string): Promise<string | null> {
    const hero = await guarded(async () => {
      const created = await createHero(classId, name)
      setVault(await getVault())
      return created
    })
    return hero?.id ?? null
  }

  async function forgetHero(hero: VaultHero) {
    if (!window.confirm(`Remove ${hero.name} from your account? This hero will be gone for good.`)) return
    const next = await guarded(() => deleteHero(hero.id))
    if (next) setVault(next)
  }

  async function forgetWorld(world: VaultWorld) {
    if (!window.confirm(`Delete this ${world.difficulty} world on floor ${world.floor}? It will be gone for good.`)) return
    const next = await guarded(() => deleteWorld(world.id))
    if (next) setVault(next)
  }

  async function newWorld(hero: VaultHero, difficulty: Difficulty, shared: boolean) {
    const world = await guarded(() => createWorld(difficulty, hero.classId))
    if (world) setLive({ mode: { kind: 'create', worldId: world.id, shared }, heroId: hero.id })
  }

  if (live && account) return <LiveGame mode={live.mode} heroId={live.heroId} onExit={() => setLive(null)} />

  return (
    <HomeScreen
      account={account}
      codes={localSaveCodes()}
      heroes={vault.heroes}
      worlds={vault.worlds}
      busy={busy}
      error={error}
      notice={notice}
      onSignedIn={handleSignedIn}
      onSignOut={handleSignOut}
      onNewHero={newHero}
      onForgetHero={hero => void forgetHero(hero)}
      onPlay={(hero, world, shared) => setLive({ mode: { kind: 'create', worldId: world.id, shared }, heroId: hero.id })}
      onNewWorld={(hero, difficulty, shared) => void newWorld(hero, difficulty, shared)}
      onForgetWorld={world => void forgetWorld(world)}
      onJoin={(hero, code) => setLive({ mode: { kind: 'join', code }, heroId: hero.id })}
    />
  )
}

export default App
