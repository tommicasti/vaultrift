import { lazy, Suspense, useState } from 'react'
import { DepositScreen } from './DepositScreen'
import { PlayersScreen } from './PlayersScreen'
import type { AccountInfo } from '../api/accountApi'
import type { Difficulty } from '../api/types'
import type { VaultHero, VaultWorld } from '../api/vaultApi'
import { biomeNames } from '../game/biomes'
import { classLabel } from '../game/heroNames'
import { biomeIds } from '../scene3d/models'
import { AccountPanel } from './AccountPanel'
import { ClassSelect } from './ClassSelect'
import './home.css'

const HomeBackdrop = lazy(() => import('../scene3d/HomeBackdrop'))

export type HomeTab = 'solo' | 'coop'

const tabKey = 'vaultrift.homeTab'
const heroKey = 'vaultrift.selectedHero'
const difficultyKey = 'vaultrift.worldDifficulty'
const worldCodeLength = 6
const defaultClass = 'knight'
const coopDifficulty: Difficulty = 'Hard'

const difficulties: { id: Difficulty; description: string }[] = [
  { id: 'Normal', description: 'Fall and you rise at the entrance, a little poorer.' },
  { id: 'Hard', description: 'Monsters are two levels stronger. The difficulty of co-op worlds.' },
  { id: 'Hardcore', description: 'Hard with a single life. Fall once and the world is gone: your hero keeps their level and points, but loses gear, backpack, coins and spells.' },
]

function load(key: string): string | null {
  try {
    return localStorage.getItem(key)
  } catch {
    return null
  }
}

function store(key: string, value: string) {
  try {
    localStorage.setItem(key, value)
  } catch {
    return
  }
}

function loadDifficulty(): Difficulty {
  const saved = load(difficultyKey)
  return difficulties.find(option => option.id === saved)?.id ?? 'Normal'
}

function worldLabel(world: VaultWorld) {
  const biome = world.biome ? ` · ${biomeNames[world.biome] ?? world.biome}` : ''
  return `Floor ${world.floor} of ${world.floorCount}${biome}`
}

function playTime(seconds: number) {
  const minutes = Math.floor(seconds / 60)
  if (minutes < 60) return `${minutes} min`
  return `${Math.floor(minutes / 60)} h ${minutes % 60} min`
}

interface HomeScreenProps {
  account: AccountInfo | null | undefined
  codes: string[]
  heroes: VaultHero[]
  worlds: VaultWorld[]
  busy: boolean
  error: string | null
  notice: string | null
  onSignedIn: (account: AccountInfo) => void
  onSignOut: () => void
  onNewHero: (classId: string, name: string) => Promise<string | null>
  onForgetHero: (hero: VaultHero) => void
  onPlay: (hero: VaultHero, world: VaultWorld, shared: boolean) => void
  onNewWorld: (hero: VaultHero, difficulty: Difficulty, shared: boolean) => void
  onForgetWorld: (world: VaultWorld) => void
  onJoin: (hero: VaultHero, code: string) => void
}

export function HomeScreen(props: HomeScreenProps) {
  const { account, heroes, worlds, busy, error, notice } = props
  const [tab, setTab] = useState<HomeTab>(() => (load(tabKey) === 'coop' ? 'coop' : 'solo'))
  const [chosenId, setChosenId] = useState<string | null>(() => load(heroKey))
  const [creating, setCreating] = useState(false)
  const [choosingWorld, setChoosingWorld] = useState(false)
  const [depositOpen, setDepositOpen] = useState(false)
  const [playersOpen, setPlayersOpen] = useState(false)
  const [listOpen, setListOpen] = useState(false)
  const [previewClass, setPreviewClass] = useState<string | null>(null)
  const [biome] = useState(() => biomeIds[Math.floor(Math.random() * biomeIds.length)])

  const hero = heroes.find(candidate => candidate.id === chosenId) ?? heroes[0] ?? null
  const index = hero ? heroes.indexOf(hero) : -1
  const shownClass = (creating && previewClass) || hero?.classId || defaultClass
  const shownGear = creating || !hero ? [] : hero.gear
  const shared = tab === 'coop'

  function chooseTab(next: HomeTab) {
    store(tabKey, next)
    setTab(next)
  }

  function chooseHeroId(id: string) {
    store(heroKey, id)
    setChosenId(id)
  }

  function closeCreation() {
    setCreating(false)
    setPreviewClass(null)
  }

  function step(by: number) {
    if (heroes.length === 0) return
    chooseHeroId(heroes[(index + by + heroes.length) % heroes.length].id)
  }

  return (
    <main className="home">
      <Suspense fallback={null}>
        <HomeBackdrop biome={biome} classId={shownClass} gear={shownGear} />
      </Suspense>
      <div className="home-shade" />
      <header className="home-top">
        <h1 className="home-title">Vaultrift</h1>
        {account && <AccountPanel account={account} codes={props.codes} onSignedIn={props.onSignedIn} onSignOut={props.onSignOut} />}
      </header>

      {account === undefined && <p className="home-loading label">Opening the vault…</p>}

      {account === null && (
        <section className="home-card home-sign-in">
          <AccountPanel account={null} codes={props.codes} onSignedIn={props.onSignedIn} onSignOut={props.onSignOut} />
        </section>
      )}

      {account && (
        <>
          <nav className="home-tabs" role="tablist" aria-label="How to play">
            <button role="tab" aria-selected={tab === 'solo'} className={tab === 'solo' ? 'active' : undefined} onClick={() => chooseTab('solo')}>
              Solo
            </button>
            <button role="tab" aria-selected={tab === 'coop'} className={tab === 'coop' ? 'active' : undefined} onClick={() => chooseTab('coop')}>
              Co-op
            </button>
          </nav>

          <section className="home-card home-hero">
            {hero ? (
              <HeroPicker
                hero={hero}
                position={index + 1}
                count={heroes.length}
                busy={busy}
                onStep={step}
                onList={() => setListOpen(true)}
                onForget={() => props.onForgetHero(hero)}
              />
            ) : (
              <p className="label">No heroes yet. Create your first one!</p>
            )}

            {hero && hero.inWorld && <p className="home-refusal">This hero is in a world right now.</p>}

            {hero && tab === 'solo' && (
              <div className="home-actions">
                <button className="primary home-play" disabled={busy || hero.inWorld} onClick={() => setChoosingWorld(true)}>
                  Continue
                  <span className="home-world">{worlds.length === 0 ? 'Start a new world' : worlds.length === 1 ? '1 world' : `${worlds.length} worlds`}</span>
                </button>
              </div>
            )}

            {tab === 'coop' && <CoopActions hero={hero} busy={busy} onHost={() => setChoosingWorld(true)} onJoin={props.onJoin} />}

            <div className="home-secondary">
              <button className="home-new" disabled={busy} onClick={() => setCreating(true)}>
                + New hero
              </button>
              {hero && (
                <button className="home-new" disabled={busy} onClick={() => setDepositOpen(true)}>
                  Gear &amp; deposit
                </button>
              )}
              {account.isAdmin && (
                <button className="home-new" onClick={() => setPlayersOpen(true)}>
                  Players
                </button>
              )}
            </div>
            {notice && <p className="account-notice">{notice}</p>}
            {error && <p className="error">{error}</p>}
          </section>

          {choosingWorld && hero && (
            <WorldPicker
              hero={hero}
              worlds={shared ? worlds.filter(world => world.difficulty === coopDifficulty) : worlds}
              shared={shared}
              busy={busy}
              onClose={() => setChoosingWorld(false)}
              onPlay={world => {
                setChoosingWorld(false)
                props.onPlay(hero, world, shared)
              }}
              onNew={difficulty => {
                setChoosingWorld(false)
                props.onNewWorld(hero, difficulty, shared)
              }}
              onForget={props.onForgetWorld}
            />
          )}

          {depositOpen && hero && <DepositScreen hero={hero} onClose={() => setDepositOpen(false)} />}
          {playersOpen && <PlayersScreen onClose={() => setPlayersOpen(false)} />}
          {listOpen && (
            <HeroList
              heroes={heroes}
              chosen={hero}
              onClose={() => setListOpen(false)}
              onChoose={chosen => {
                chooseHeroId(chosen.id)
                setListOpen(false)
              }}
            />
          )}

          {creating && (
            <div className="home-create" role="dialog" aria-label="New hero">
              <div className="home-create-head">
                <h2>A new hero</h2>
                <button onClick={closeCreation}>Cancel</button>
              </div>
              <ClassSelect
                disabled={busy}
                onPreview={setPreviewClass}
                onChoose={(classId, name) => {
                  closeCreation()
                  void props.onNewHero(classId, name).then(id => {
                    if (id) chooseHeroId(id)
                  })
                }}
              />
            </div>
          )}
        </>
      )}
    </main>
  )
}

interface HeroPickerProps {
  hero: VaultHero
  position: number
  count: number
  busy: boolean
  onStep: (by: number) => void
  onList: () => void
  onForget: () => void
}

function HeroPicker({ hero, position, count, busy, onStep, onList, onForget }: HeroPickerProps) {
  return (
    <div className="home-picker">
      <button className="home-arrow" aria-label="Previous hero" disabled={count < 2} onClick={() => onStep(-1)}>
        ‹
      </button>
      <div className="home-hero-details">
        <strong>{hero.name}</strong>
        <span className="label">
          {classLabel(hero.classId)} · Level {hero.level}
        </span>
        {hero.played.length > 0 && (
          <span className="home-played" title="Difficulties this hero has played">
            {hero.played.map(difficulty => (
              <span key={difficulty} className={`home-badge difficulty-${difficulty.toLowerCase()}`}>
                {difficulty}
              </span>
            ))}
          </span>
        )}
        <span className="home-hero-meta">
          {count > 1 && (
            <button className="home-hero-count" title="All your heroes" onClick={onList}>
              {position} / {count} · All heroes
            </button>
          )}
          <button className="forget-button" disabled={busy || hero.inWorld} aria-label={`Forget ${hero.name}`} title="Remove from your account" onClick={onForget}>
            ×
          </button>
        </span>
      </div>
      <button className="home-arrow" aria-label="Next hero" disabled={count < 2} onClick={() => onStep(1)}>
        ›
      </button>
    </div>
  )
}

interface HeroListProps {
  heroes: VaultHero[]
  chosen: VaultHero | null
  onClose: () => void
  onChoose: (hero: VaultHero) => void
}

function HeroList({ heroes, chosen, onClose, onChoose }: HeroListProps) {
  return (
    <div className="home-worlds" role="dialog" aria-label="Your heroes">
      <div className="home-card home-worlds-card">
        <div className="home-create-head">
          <h2>Your heroes</h2>
          <button onClick={onClose}>Close</button>
        </div>
        <ul className="home-world-list hero-list">
          {heroes.map(candidate => (
            <li key={candidate.id}>
              <button className={`hero-list-row${candidate.id === chosen?.id ? ' chosen' : ''}`} onClick={() => onChoose(candidate)}>
                <strong>{candidate.name}</strong>
                <span className="label">
                  {classLabel(candidate.classId)} · Level {candidate.level} · {playTime(candidate.playSeconds)}
                  {candidate.inWorld && ' · in a world'}
                </span>
                {candidate.played.length > 0 && (
                  <span className="home-played">
                    {candidate.played.map(difficulty => (
                      <span key={difficulty} className={`home-badge difficulty-${difficulty.toLowerCase()}`}>
                        {difficulty}
                      </span>
                    ))}
                  </span>
                )}
              </button>
            </li>
          ))}
        </ul>
      </div>
    </div>
  )
}

interface WorldPickerProps {
  hero: VaultHero
  worlds: VaultWorld[]
  shared: boolean
  busy: boolean
  onClose: () => void
  onPlay: (world: VaultWorld) => void
  onNew: (difficulty: Difficulty) => void
  onForget: (world: VaultWorld) => void
}

function WorldPicker({ hero, worlds, shared, busy, onClose, onPlay, onNew, onForget }: WorldPickerProps) {
  const [chosen, setChosen] = useState<Difficulty>(loadDifficulty)
  const difficulty = shared ? coopDifficulty : chosen
  const offered = shared ? difficulties.filter(option => option.id === coopDifficulty) : difficulties

  return (
    <div className="home-worlds" role="dialog" aria-label={shared ? 'Host a world' : 'Choose a world'}>
      <div className="home-card home-worlds-card">
        <div className="home-create-head">
          <h2>{shared ? 'Host a world' : `Where does ${hero.name} go?`}</h2>
          <button onClick={onClose}>Cancel</button>
        </div>

        {worlds.length > 0 && (
          <ul className="home-world-list">
            {worlds.map(world => (
              <li key={world.id} className="home-world-row">
                <span className={`home-badge difficulty-${world.difficulty.toLowerCase()}`}>{world.difficulty}</span>
                <span className="home-world-text">
                  <strong>{worldLabel(world)}</strong>
                  <span className="label">
                    {world.isConquered ? 'Vault conquered' : `Deepest floor ${world.deepestFloor}`} · {playTime(world.playSeconds)}
                    {world.graveHeroIds.includes(hero.id) && ' · your grave waits here'}
                    {world.isOpen && ' · open right now'}
                  </span>
                </span>
                <button className="primary" disabled={busy || world.isOpen} onClick={() => onPlay(world)}>
                  Enter
                </button>
                <button className="forget-button" disabled={busy || world.isOpen} aria-label="Delete this world" title="Delete this world" onClick={() => onForget(world)}>
                  ×
                </button>
              </li>
            ))}
          </ul>
        )}

        <fieldset className="difficulty-picker">
          <legend className="label">New world</legend>
          {offered.map(option => (
            <label key={option.id} className={`difficulty-option difficulty-${option.id.toLowerCase()}${option.id === difficulty ? ' active' : ''}`}>
              <input
                type="radio"
                name="world-difficulty"
                value={option.id}
                checked={option.id === difficulty}
                onChange={() => {
                  store(difficultyKey, option.id)
                  setChosen(option.id)
                }}
              />
              <strong>{option.id}</strong>
              <span>{option.description}</span>
            </label>
          ))}
        </fieldset>
        <button className="home-play" disabled={busy} onClick={() => onNew(difficulty)}>
          Start a new {difficulty} world
        </button>
      </div>
    </div>
  )
}

interface CoopActionsProps {
  hero: VaultHero | null
  busy: boolean
  onHost: () => void
  onJoin: (hero: VaultHero, code: string) => void
}

function CoopActions({ hero, busy, onHost, onJoin }: CoopActionsProps) {
  const [code, setCode] = useState('')
  const cleanCode = code.replace(/[^a-z0-9]/gi, '').toUpperCase()
  const blocked = busy || !hero || hero.inWorld

  return (
    <div className="home-actions">
      <p className="label">Up to 6 heroes, always on Hard. What your hero earns comes home with them.</p>
      {!hero && <p className="home-refusal">Create a hero to play together.</p>}
      <button className="primary home-play" disabled={blocked} onClick={onHost}>
        Host a world
      </button>
      <form
        className="code-form-row"
        onSubmit={event => {
          event.preventDefault()
          if (hero && cleanCode.length === worldCodeLength) onJoin(hero, cleanCode)
        }}
      >
        <input
          aria-label="World code"
          placeholder="World code"
          maxLength={worldCodeLength + 2}
          value={code}
          autoComplete="off"
          autoCapitalize="characters"
          spellCheck={false}
          onChange={event => setCode(event.target.value)}
        />
        <button type="submit" disabled={blocked || cleanCode.length !== worldCodeLength}>
          Join
        </button>
      </form>
    </div>
  )
}
