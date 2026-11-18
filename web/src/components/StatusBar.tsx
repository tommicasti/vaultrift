import type { PlayerState } from '../api/types'
import { biomeNames } from '../game/biomes'
import { abilityNames, describeModifier, elementNames } from '../game/items'
import { ElementIcon } from './ElementIcon'
import { ItemIcon } from './ItemIcon'

interface StatusBarProps {
  player: PlayerState
  deaths: number
  floor: number
  floorCount: number
  biome?: string
  onOpenInventory: () => void
}

export function StatusBar({ player, deaths, floor, floorCount, biome, onOpenInventory }: StatusBarProps) {
  const healthPercent = (player.health / player.maxHealth) * 100
  const experiencePercent = (player.experience / player.experienceToNextLevel) * 100
  const manaPercent = player.maxMana > 0 ? (player.mana / player.maxMana) * 100 : 0
  const ability = player.ability
  const hasExtras =
    ability !== undefined ||
    player.quickSlot !== undefined ||
    player.boosts.length > 0 ||
    player.resistance !== undefined ||
    player.freeSteps > 0 ||
    player.unspentPoints > 0

  return (
    <section className="status-bar">
      <div className="health">
        <div className="label">
          <strong className="hero-name">{player.name}</strong> · Lv {player.level} · Health {player.health} /{' '}
          {player.maxHealth} · Mana {player.mana} / {player.maxMana}
        </div>
        <div className="health-track">
          <div className="health-fill" style={{ width: `${healthPercent}%` }} />
        </div>
        <div
          className="mana-track"
          role="progressbar"
          aria-label="Mana"
          aria-valuemin={0}
          aria-valuemax={player.maxMana}
          aria-valuenow={player.mana}
        >
          <div className="mana-fill" style={{ width: `${manaPercent}%` }} />
        </div>
        <div
          className="experience-track"
          role="progressbar"
          aria-label="Experience"
          aria-valuemin={0}
          aria-valuemax={player.experienceToNextLevel}
          aria-valuenow={player.experience}
          title={`XP ${player.experience} / ${player.experienceToNextLevel}`}
        >
          <div className="experience-fill" style={{ width: `${experiencePercent}%` }} />
        </div>
      </div>
      <div className="label stats">
        ATK {player.attack} · DEF {player.defense} · EVA {player.evasion}% · CRIT {player.critical}%
      </div>
      <div className="label">
        Floor {floor} / {floorCount}
        {biome && ` · ${biomeNames[biome] ?? biome}`} · Deaths: {deaths}
      </div>
      {hasExtras && (
        <div className="status-extras">
          {ability && ability.activeTurns > 0 && (
            <span className="boost-tag ability-tag active">
              {abilityNames[ability.kind]} · {ability.activeTurns}
            </span>
          )}
          {ability && ability.activeTurns === 0 && (
            <span className={ability.cooldown > 0 ? 'boost-tag ability-tag' : 'boost-tag ability-tag ready'}>
              {abilityNames[ability.kind]} {ability.cooldown > 0 ? `in ${ability.cooldown}` : 'ready'}
            </span>
          )}
          {player.unspentPoints > 0 && (
            <button className="boost-tag points-tag" onClick={onOpenInventory}>
              {player.unspentPoints} {player.unspentPoints === 1 ? 'point' : 'points'} to spend
            </button>
          )}
          {player.quickSlot && (
            <span className="label quick-slot-info">
              Quick slot <ItemIcon item={player.quickSlot} /> ×{player.quickSlotQuantity}
            </span>
          )}
          {player.resistance && (
            <span className="label resistance-info">
              Resists <ElementIcon element={player.resistance} /> {elementNames[player.resistance]}
            </span>
          )}
          {player.freeSteps > 0 && <span className="boost-tag free-step-tag">Free step</span>}
          {player.boosts.map(boost => (
            <span key={boost.item.id} className="boost-tag">
              {describeModifier(boost.modifier)} · {boost.remainingTurns}
            </span>
          ))}
        </div>
      )}
    </section>
  )
}
