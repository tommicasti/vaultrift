import type { ItemMark } from '../api/types'

interface MarkIconProps {
  mark: Exclude<ItemMark, 'None'>
}

export function MarkIcon({ mark }: MarkIconProps) {
  if (mark === 'Favorite') {
    return (
      <svg viewBox="0 0 16 16" aria-hidden="true" className="mark-icon">
        <path d="M8 1.5l1.9 4.2 4.6.4-3.5 3 1.1 4.5L8 11.2l-4.1 2.4L5 9.1l-3.5-3 4.6-.4z" fill="currentColor" />
      </svg>
    )
  }

  return (
    <svg viewBox="0 0 16 16" aria-hidden="true" className="mark-icon">
      <path d="M6 1.5h4l.5 1.5H14v1.5H2V3h3.5zM3.2 5.5h9.6l-.8 9H4z" fill="currentColor" />
      <path d="M6.2 7.2v5.6M9.8 7.2v5.6" stroke="var(--surface)" strokeWidth="1.1" />
    </svg>
  )
}

interface MarkBadgeProps {
  mark?: ItemMark
}

export function MarkBadge({ mark }: MarkBadgeProps) {
  if (!mark || mark === 'None') return null

  return (
    <span className={`slot-mark mark-${mark.toLowerCase()}`}>
      <MarkIcon mark={mark} />
    </span>
  )
}
