import { useEffect } from 'react'
import type { TrophyInfo } from '../api/types'

const showMs = 4200

interface TrophyToastProps {
  trophy: TrophyInfo
  onDone: () => void
}

export function TrophyToast({ trophy, onDone }: TrophyToastProps) {
  useEffect(() => {
    const timer = window.setTimeout(onDone, showMs)
    return () => window.clearTimeout(timer)
  }, [trophy, onDone])

  return (
    <div className="trophy-toast" role="status">
      <span className="trophy-toast-icon" aria-hidden="true">
        🏆
      </span>
      <span className="trophy-toast-text">
        <span className="trophy-toast-title">Trophy unlocked</span>
        <strong>{trophy.name}</strong>
        <span className="trophy-toast-description">{trophy.description}</span>
      </span>
    </div>
  )
}
