import type { ItemInfo } from '../api/types'
import { itemSprite } from '../game/items'
import { Sprite } from './Sprite'

interface ItemIconProps {
  item: ItemInfo
  className?: string
}

export function ItemIcon({ item, className }: ItemIconProps) {
  const sprite = itemSprite(item)
  const classes = ['item-icon', `rarity-${item.rarity.toLowerCase()}`, className].filter(Boolean).join(' ')

  return <Sprite sheet={sprite.sheet} index={sprite.index} className={classes} />
}