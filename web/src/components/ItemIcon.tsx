import type { ItemInfo } from '../api/types'
import { itemSprite } from '../game/items'
import { elementSprites, sheets } from '../game/sprites'
import { Sprite } from './Sprite'

interface ItemIconProps {
  item: ItemInfo
  className?: string
}

export function ItemIcon({ item, className }: ItemIconProps) {
  const sprite = itemSprite(item)
  const classes = ['item-icon', `rarity-${item.rarity.toLowerCase()}`, className].filter(Boolean).join(' ')

  return (
    <span className={classes}>
      <Sprite sheet={sprite.sheet} index={sprite.index} className="item-sprite" />
      {item.element && <Sprite sheet={sheets.items} index={elementSprites[item.element]} className="element-badge" />}
    </span>
  )
}