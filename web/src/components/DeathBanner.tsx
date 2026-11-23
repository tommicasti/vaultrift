interface DeathBannerProps {
  onDone: () => void
}

export function DeathBanner({ onDone }: DeathBannerProps) {
  return (
    <div className="death-banner" role="alert" onAnimationEnd={event => event.target === event.currentTarget && onDone()}>
      <div className="death-banner-band">
        <span className="death-banner-text">You have fallen</span>
      </div>
    </div>
  )
}
