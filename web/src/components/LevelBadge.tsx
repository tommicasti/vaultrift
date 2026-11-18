interface LevelBadgeProps {
  level: number
  highlight?: boolean
}

export function LevelBadge({ level, highlight = false }: LevelBadgeProps) {
  return (
    <span className={highlight ? 'level-badge has-points' : 'level-badge'} aria-label={`Level ${level}`}>
      {level}
    </span>
  )
}