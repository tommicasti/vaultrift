import type { ItemInfo } from '../api/types'
import { categoryIcons } from '../game/items'

export function ItemIcon({ item }: { item: ItemInfo }) {
  return (
    <span className={`item-icon rarity-${item.rarity.toLowerCase()}`}>
      {categoryIcons[item.category]}
    </span>
  )
}