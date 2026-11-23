import { useState, type FormEvent } from 'react'
import { AccountError, login, register, type AccountInfo } from '../api/accountApi'
import { TrophyList } from './TrophyList'

type Mode = 'closed' | 'login' | 'register'

interface AccountPanelProps {
  account: AccountInfo | null
  codes: string[]
  onSignedIn: (account: AccountInfo) => void
  onSignOut: () => void
}

export function AccountPanel({ account, codes, onSignedIn, onSignOut }: AccountPanelProps) {
  const [mode, setMode] = useState<Mode>('closed')
  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [email, setEmail] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [showTrophies, setShowTrophies] = useState(false)

  if (account) {
    const unlocked = account.trophies.filter(trophy => trophy.unlockedAt).length
    return (
      <section className="account-panel signed-in">
        <span>
          Signed in as <strong>{account.userName}</strong>
        </span>
        <div className="account-actions">
          <button type="button" onClick={() => setShowTrophies(true)}>
            🏆 {unlocked} / {account.trophies.length}
          </button>
          <button type="button" onClick={onSignOut}>
            Log out
          </button>
        </div>
        {showTrophies && <TrophyList trophies={account.trophies} onClose={() => setShowTrophies(false)} />}
      </section>
    )
  }

  function open(next: Mode) {
    setMode(next)
    setError(null)
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (busy) return

    setBusy(true)
    setError(null)
    try {
      const signedIn = mode === 'register' ? await register(userName, password, email, codes) : await login(userName, password, codes)
      setPassword('')
      setMode('closed')
      onSignedIn(signedIn)
    } catch (failure) {
      setError(failure instanceof AccountError ? failure.message : 'Could not reach the server. Try again.')
    } finally {
      setBusy(false)
    }
  }

  if (mode === 'closed') {
    return (
      <section className="account-panel">
        <span className="label">Sign in or create an account to play. Your heroes follow you on every device.</span>
        <div className="account-actions">
          <button type="button" onClick={() => open('login')}>
            Sign in
          </button>
          <button type="button" className="primary" onClick={() => open('register')}>
            Create account
          </button>
        </div>
      </section>
    )
  }

  const registering = mode === 'register'

  return (
    <section className="account-panel open">
      <form className="account-form" method="post" action="/api/account" autoComplete="on" onSubmit={event => void submit(event)}>
        <h2>{registering ? 'Create account' : 'Sign in'}</h2>
        <label>
          <span className="label">Name</span>
          <input
            name="username"
            autoComplete="username"
            autoCapitalize="none"
            spellCheck={false}
            required
            minLength={3}
            maxLength={20}
            pattern="[A-Za-z0-9_.\-]{3,20}"
            title="3–20 letters, digits, dots, dashes or underscores"
            value={userName}
            onChange={event => setUserName(event.target.value)}
          />
        </label>
        <label>
          <span className="label">Password</span>
          <input
            name="password"
            type="password"
            autoComplete={registering ? 'new-password' : 'current-password'}
            required
            minLength={registering ? 8 : 1}
            maxLength={128}
            value={password}
            onChange={event => setPassword(event.target.value)}
          />
        </label>
        {registering && (
          <label>
            <span className="label">Email (optional, to recover your password)</span>
            <input name="email" type="email" autoComplete="email" maxLength={254} value={email} onChange={event => setEmail(event.target.value)} />
          </label>
        )}
        {error && <p className="error">{error}</p>}
        <div className="account-actions">
          <button type="submit" className="primary" disabled={busy}>
            {busy ? 'Please wait…' : registering ? 'Create account' : 'Sign in'}
          </button>
          <button type="button" disabled={busy} onClick={() => open(registering ? 'login' : 'register')}>
            {registering ? 'I already have an account' : 'Create an account instead'}
          </button>
          <button type="button" className="account-cancel" disabled={busy} onClick={() => open('closed')}>
            Cancel
          </button>
        </div>
      </form>
    </section>
  )
}
