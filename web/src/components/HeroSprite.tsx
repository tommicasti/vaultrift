import type { PlayerState } from '../api/types'
import { classSprite, cloakSprites, helmetSprites, legendaryOutfits, outfitSprite, sheets } from '../game/sprites'
import { Sprite } from './Sprite'

interface HeroSpriteProps {
  player: PlayerState
  className: string
}

export function HeroSprite({ player, className }: HeroSpriteProps) {
  const equipped = new Map(player.equipment.map(entry => [entry.slot, entry.item]))
  const body = equipped.get('Body')
  const head = equipped.get('Head')
  const cloak = equipped.get('Cloak')
  const outfit = body ? legendaryOutfits[body.id] : undefined
  const sprite = outfitSprite(player.classId, body?.id) ?? classSprite(player.classId)
  const active = player.ability && player.ability.activeTurns > 0 ? `status-${player.ability.kind.toLowerCase()}` : ''
  const classes = ['hero', className, outfit ? `glow-${outfit.glow}` : '', active].filter(Boolean).join(' ')

  return (
    <span className={classes} role="img" aria-label={player.name}>
      <Sprite sheet={sprite.sheet} index={sprite.index} className="hero-layer" />
      {cloak && <Sprite sheet={sheets.outfits} index={cloakSprites[cloak.element ?? 'Plain']} className="hero-layer" />}
      {head && <Sprite sheet={sheets.outfits} index={helmetSprites[head.rarity]} className="hero-layer" />}
    </span>
  )
}
