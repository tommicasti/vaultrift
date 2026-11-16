import { spriteStyle, type SpriteSheet } from '../game/sprites'

interface SpriteProps {
  sheet: SpriteSheet
  index: number
  className?: string
  label?: string
}

export function Sprite({ sheet, index, className, label }: SpriteProps) {
  return (
    <span
      className={className ? `sprite ${className}` : 'sprite'}
      style={spriteStyle(sheet, index)}
      role={label ? 'img' : undefined}
      aria-label={label}
      aria-hidden={label ? undefined : true}
    />
  )
}