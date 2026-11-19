import type { SpellInfo } from '../api/types'
import { describeSpell } from '../game/items'
import { ElementIcon } from './ElementIcon'

interface SpellBarProps {
  spells: (SpellInfo | null)[]
  active: number
  mana: number
  overloaded: boolean
  disabled: boolean
  onSelect: (index: number) => void
}

export function SpellBar({ spells, active, mana, overloaded, disabled, onSelect }: SpellBarProps) {
  if (spells.length === 0) return null

  return (
    <section className={overloaded ? 'spell-bar overloaded' : 'spell-bar'} aria-label="Spellbook">
      {spells.map((spell, index) => {
        if (!spell) {
          return (
            <span key={`empty-${index}`} className="spell empty" title="Learn a scroll on this key">
              <span className="spell-key">{index + 1}</span>
              <span className="spell-name">empty</span>
            </span>
          )
        }

        const cost = overloaded ? 0 : spell.manaCost
        const classes = [
          'spell',
          `element-${spell.element.toLowerCase()}`,
          index === active ? 'active' : '',
          cost > mana ? 'too-costly' : '',
        ].join(' ')

        return (
          <button
            key={spell.id}
            className={classes}
            disabled={disabled}
            aria-pressed={index === active}
            title={describeSpell(spell)}
            onClick={() => onSelect(index)}
          >
            <span className="spell-key">{index + 1}</span>
            <ElementIcon element={spell.element} className="spell-icon" />
            <span className="spell-name">{spell.name}</span>
            <span className="spell-cost">{cost}</span>
          </button>
        )
      })}
    </section>
  )
}
