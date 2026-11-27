import { useEffect, useState } from 'react'
import type { InventoryEntryState, MerchantState, OfferInfo, PlayerState, StatsPreview } from '../api/types'
import { sortForSale } from '../game/backpackSort'
import { describeItem, describeKind, elementNames } from '../game/items'
import { coinSprite } from '../game/sprites'
import { ItemIcon } from './ItemIcon'
import { MarkBadge, MarkIcon } from './MarkBadge'
import { Sprite } from './Sprite'

interface ShopScreenProps {
  merchant: MerchantState
  coins: number
  entries: InventoryEntryState[]
  capacity: number
  disabled: boolean
  player?: PlayerState
  onPreviewOffer?: (offerId: number) => Promise<StatsPreview>
  onBuy: (offerId: number) => void
  onBuyUpgrade: () => void
  onSell: (entryId: number) => void
  onSellJunk: () => void
  onClose: () => void
}

const notWearable = new Set(['Potion', 'Scroll', 'Rune'])

type StatKey = 'maxHealth' | 'maxMana' | 'attack' | 'defense' | 'evasion' | 'critical' | 'block'

const comparedStats: { key: StatKey; label: string; suffix: string }[] = [
  { key: 'maxHealth', label: 'Health', suffix: '' },
  { key: 'maxMana', label: 'Mana', suffix: '' },
  { key: 'attack', label: 'Attack', suffix: '' },
  { key: 'defense', label: 'Defense', suffix: '' },
  { key: 'evasion', label: 'Evasion', suffix: '%' },
  { key: 'critical', label: 'Critical', suffix: '%' },
  { key: 'block', label: 'Block', suffix: '%' },
]

function OfferStats({ player, preview }: { player: PlayerState; preview: StatsPreview | undefined }) {
  if (!preview) return null
  if (preview.result === 'WrongClass') return <span className="offer-stats down">Not for your class</span>
  if (preview.result !== 'Equipped') return null

  const changes = comparedStats.filter(stat => preview[stat.key] !== player[stat.key])
  const resistanceChanged = (preview.resistance ?? null) !== (player.resistance ?? null)
  if (changes.length === 0 && !resistanceChanged) return <span className="offer-stats">No change if equipped</span>

  return (
    <span className="offer-stats" title="Your stats if you equip it">
      {changes.map(stat => {
        const up = preview[stat.key] > player[stat.key]
        return (
          <span key={stat.key} className={up ? 'stat-change up' : 'stat-change down'}>
            {stat.label} {player[stat.key]}
            {stat.suffix} {up ? '▲' : '▼'} {preview[stat.key]}
            {stat.suffix}
          </span>
        )
      })}
      {resistanceChanged && (
        <span className={preview.resistance ? 'stat-change up' : 'stat-change down'}>
          Resists {player.resistance ? elementNames[player.resistance] : '—'} → {preview.resistance ? elementNames[preview.resistance] : '—'}
        </span>
      )}
    </span>
  )
}

function usePreviews(offers: OfferInfo[], player: PlayerState | undefined, onPreviewOffer: ShopScreenProps['onPreviewOffer']) {
  const wearable = offers.filter(offer => !notWearable.has(offer.item.category))
  const key = player
    ? `${wearable.map(offer => offer.id).join(',')}|${player.equipment.map(equipped => equipped.entryId).join(',')}|${player.level}|${player.unspentPoints}`
    : ''
  const [loaded, setLoaded] = useState<{ key: string; previews: Map<number, StatsPreview> } | null>(null)

  useEffect(() => {
    if (!onPreviewOffer || key === '') return
    let current = true
    const ids = key.split('|')[0].split(',').filter(Boolean).map(Number)
    Promise.all(ids.map(id => onPreviewOffer(id).then(preview => [id, preview] as const).catch(() => null)))
      .then(results => {
        if (!current) return
        const previews = new Map<number, StatsPreview>()
        for (const result of results) if (result) previews.set(result[0], result[1])
        setLoaded({ key, previews })
      })
      .catch(() => undefined)
    return () => {
      current = false
    }
  }, [key, onPreviewOffer])

  return loaded?.key === key ? loaded.previews : undefined
}

function Coins({ amount }: { amount: number }) {
  return (
    <span className="coins">
      <Sprite sheet={coinSprite.sheet} index={coinSprite.index} className="coin-icon" />
      {amount}
    </span>
  )
}

export function ShopScreen({
  merchant,
  coins,
  entries,
  capacity,
  disabled,
  player,
  onPreviewOffer,
  onBuy,
  onBuyUpgrade,
  onSell,
  onSellJunk,
  onClose,
}: ShopScreenProps) {
  const junk = entries.filter(entry => entry.mark === 'Junk')
  const junkValue = junk.reduce((total, entry) => total + entry.sellPrice * entry.quantity, 0)
  const backpackFull = entries.length >= capacity
  const previews = usePreviews(merchant.offers, player, onPreviewOffer)

  return (
    <div className="overlay" role="dialog" aria-modal="true" aria-label="Merchant">
      <section className="shop-screen">
        <header className="inventory-header">
          <h2>Merchant</h2>
          <Coins amount={coins} />
          <button aria-label="Close the shop" onClick={onClose}>
            ✕
          </button>
        </header>

        {!merchant.isNearby && <p className="label">Step next to the merchant to trade.</p>}

        <div className="shop-body">
          <div className="shop-column">
            <h3 className="label">For sale</h3>
            <ul className="shop-list">
              {merchant.offers.map(offer => (
                <li
                  key={offer.id}
                  className={`shop-row rarity-${offer.item.rarity.toLowerCase()}${offer.kind === 'Featured' ? ' featured' : ''}`}
                >
                  <ItemIcon item={offer.item} />
                  <div className="shop-row-text">
                    <span className="item-name">{offer.item.name}</span>
                    {offer.kind === 'Featured' && <span className="offer-tag featured-tag">Picked for your class</span>}
                    {offer.kind === 'Unlimited' && <span className="offer-tag">Always in stock · +25% each time</span>}
                    <span className="label">{[describeKind(offer.item).name, describeItem(offer.item)].filter(Boolean).join(' · ')}</span>
                    {player && <OfferStats player={player} preview={previews?.get(offer.id)} />}
                  </div>
                  <button
                    disabled={disabled || !merchant.isNearby || coins < offer.price || backpackFull}
                    onClick={() => onBuy(offer.id)}
                  >
                    <Coins amount={offer.price} />
                  </button>
                </li>
              ))}
              {merchant.upgradePrice !== undefined && (
                <li className="shop-row upgrade">
                  <span className="upgrade-icon" aria-hidden="true">+{merchant.upgradeSize}</span>
                  <div className="shop-row-text">
                    <span className="item-name">Bigger backpack</span>
                    <span className="label">
                      {capacity} → {capacity + merchant.upgradeSize} slots
                    </span>
                  </div>
                  <button
                    disabled={disabled || !merchant.isNearby || coins < merchant.upgradePrice}
                    onClick={onBuyUpgrade}
                  >
                    <Coins amount={merchant.upgradePrice} />
                  </button>
                </li>
              )}
              {merchant.offers.length === 0 && merchant.upgradePrice === undefined && (
                <li className="label">Sold out.</li>
              )}
            </ul>
          </div>

          <div className="shop-column">
            <h3 className="label">
              Your backpack {entries.length} / {capacity}
            </h3>
            {junk.length > 0 && (
              <button className="junk-button" disabled={disabled || !merchant.isNearby} onClick={onSellJunk}>
                <MarkIcon mark="Junk" />
                Sell all junk · <Coins amount={junkValue} />
              </button>
            )}
            <ul className="shop-list">
              {sortForSale(entries).map(entry => (
                <li key={entry.id} className={`shop-row rarity-${entry.item.rarity.toLowerCase()}`}>
                  <span className="shop-icon">
                    <ItemIcon item={entry.item} />
                    <MarkBadge mark={entry.mark} />
                  </span>
                  <div className="shop-row-text">
                    <span className="item-name">
                      {entry.item.name}
                      {entry.quantity > 1 && ` ×${entry.quantity}`}
                    </span>
                    <span className="label">{describeKind(entry.item).name}</span>
                  </div>
                  <button
                    disabled={disabled || !merchant.isNearby || entry.mark === 'Favorite'}
                    title={entry.mark === 'Favorite' ? 'Favorites cannot be sold' : undefined}
                    onClick={() => onSell(entry.id)}
                  >
                    Sell <Coins amount={entry.sellPrice} />
                  </button>
                </li>
              ))}
            </ul>
          </div>
        </div>
      </section>
    </div>
  )
}
