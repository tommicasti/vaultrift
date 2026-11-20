import type { InventoryEntryState, MerchantState } from '../api/types'
import { sortForSale } from '../game/backpackSort'
import { describeItem, describeKind } from '../game/items'
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
  onBuy: (offerId: number) => void
  onBuyUpgrade: () => void
  onSell: (entryId: number) => void
  onSellJunk: () => void
  onClose: () => void
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
  onBuy,
  onBuyUpgrade,
  onSell,
  onSellJunk,
  onClose,
}: ShopScreenProps) {
  const junk = entries.filter(entry => entry.mark === 'Junk')
  const junkValue = junk.reduce((total, entry) => total + entry.sellPrice * entry.quantity, 0)
  const backpackFull = entries.length >= capacity

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
