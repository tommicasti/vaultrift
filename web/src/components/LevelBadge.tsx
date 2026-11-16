interface LevelBadgeProps {
  level: number
}

export function LevelBadge({ level }: LevelBadgeProps) {
  return (
    <span className="level-badge" aria-label={`Level ${level}`}>
      {level}
    </span>
  )
}