import type { Element } from '../api/types'
import { elementNames } from '../game/items'
import { elementSprites, sheets } from '../game/sprites'
import { Sprite } from './Sprite'

interface ElementIconProps {
  element: Element
  className?: string
}

export function ElementIcon({ element, className }: ElementIconProps) {
  return (
    <Sprite
      sheet={sheets.items}
      index={elementSprites[element]}
      className={className ? `element-icon ${className}` : 'element-icon'}
      label={elementNames[element]}
    />
  )
}