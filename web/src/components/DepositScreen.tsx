import { useEffect, useState } from 'react'
import type { ItemInfo } from '../api/types'
import {
  equipFromVault,
  getDeposit,
  storeEquipped,
  storeInDeposit,
  takeFromDeposit,
  unequipInVault,
  type DepositInfo,
  type VaultHero,
} from '../api/vaultApi'
import { describeItem, describeKind, slotLabels } from '../game/items'
import { ItemIcon } from './ItemIcon'

interface DepositScreenProps {
  hero: VaultHero
  onClose: () => void
}

interface RowAction {
  label: string
  disabled: boolean
  act: () => void
}

const notWearable = new Set(['Potion', 'Scroll', 'Rune'])

function ItemRow({ item, quantity, note, actions }: { item: ItemInfo; quantity: number; note?: string; actions: RowAction[] }) {
  return (
    <li className={`shop-row rarity-${item.rarity.toLowerCase()}`}>
      <ItemIcon item={item} />
      <div className="shop-row-text">
        <span className="item-name">
          {item.name}
          {quantity > 1 && ` ×${quantity}`}
        </span>
        <span className="label">{[note, describeKind(item).name, describeItem(item)].filter(Boolean).join(' · ')}</span>
      </div>
      <div className="deposit-actions">
        {actions.map(action => (
          <button key={action.label} disabled={action.disabled} onClick={action.act}>
            {action.label}
          </button>
        ))}
      </div>
    </li>
  )
}

export function DepositScreen({ hero, onClose }: DepositScreenProps) {
  const [deposit, setDeposit] = useState<DepositInfo | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let current = true
    getDeposit(hero.id)
      .then(found => {
        if (current) setDeposit(found)
      })
      .catch((failure: unknown) => {
        if (current) setError(failure instanceof Error ? failure.message : 'Could not open the deposit.')
      })
    return () => {
      current = false
    }
  }, [hero.id])

  const move = (request: Promise<DepositInfo>) => {
    setBusy(true)
    setError(null)
    request
      .then(setDeposit)
      .catch((failure: unknown) => setError(failure instanceof Error ? failure.message : 'The deposit did not answer.'))
      .finally(() => setBusy(false))
  }

  const depositFull = deposit !== null && deposit.items.length >= deposit.capacity
  const backpackFull = deposit !== null && deposit.backpack.length >= deposit.backpackCapacity
  const locked = busy || hero.inWorld

  return (
    <div className="home-worlds" role="dialog" aria-label="Deposit">
      <div className="home-card home-worlds-card deposit-card">
        <div className="home-create-head">
          <h2>Gear and deposit</h2>
          <button onClick={onClose}>Close</button>
        </div>
        <p className="label">Change {hero.name}'s gear before entering a world, and keep items in the account deposit: any of your heroes can take them.</p>
        {hero.inWorld && <p className="home-refusal">{hero.name} is in a world right now: come back to the vault first.</p>}
        {error && <p className="error">{error}</p>}
        {!deposit && !error && <p className="label">Opening the deposit…</p>}
        {deposit && (
          <div className="shop-body deposit-body">
            <section className="shop-column">
              <h3>Worn by {hero.name}</h3>
              {deposit.equipment.length === 0 ? (
                <p className="label">Nothing worn.</p>
              ) : (
                <ul className="shop-list">
                  {deposit.equipment.map(equipped => (
                    <ItemRow
                      key={equipped.slot}
                      item={equipped.item}
                      quantity={1}
                      note={slotLabels[equipped.slot]}
                      actions={[
                        { label: 'Unequip', disabled: locked || backpackFull, act: () => move(unequipInVault(hero.id, equipped.slot)) },
                        { label: 'Store', disabled: locked || depositFull, act: () => move(storeEquipped(hero.id, equipped.slot)) },
                      ]}
                    />
                  ))}
                </ul>
              )}
              <h3>
                Backpack · {deposit.backpack.length}/{deposit.backpackCapacity}
              </h3>
              {deposit.backpack.length === 0 ? (
                <p className="label">The backpack is empty.</p>
              ) : (
                <ul className="shop-list">
                  {deposit.backpack.map(entry => (
                    <ItemRow
                      key={entry.id}
                      item={entry.item}
                      quantity={entry.quantity}
                      actions={[
                        ...(notWearable.has(entry.item.category)
                          ? []
                          : [{ label: 'Equip', disabled: locked, act: () => move(equipFromVault(hero.id, entry.id)) }]),
                        { label: 'Store', disabled: locked || depositFull, act: () => move(storeInDeposit(hero.id, entry.id)) },
                      ]}
                    />
                  ))}
                </ul>
              )}
            </section>
            <section className="shop-column">
              <h3>
                Account deposit · {deposit.items.length}/{deposit.capacity}
              </h3>
              {deposit.items.length === 0 ? (
                <p className="label">Nothing stored yet.</p>
              ) : (
                <ul className="shop-list">
                  {deposit.items.map(slot => (
                    <ItemRow
                      key={`${slot.index}-${slot.item.id}`}
                      item={slot.item}
                      quantity={slot.quantity}
                      actions={[{ label: 'Take', disabled: locked || backpackFull, act: () => move(takeFromDeposit(hero.id, slot.index)) }]}
                    />
                  ))}
                </ul>
              )}
            </section>
          </div>
        )}
      </div>
    </div>
  )
}
