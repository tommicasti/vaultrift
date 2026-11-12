interface EventLogProps {
  entries: string[]
}

export function EventLog({ entries }: EventLogProps) {
  return (
    <section className="event-log" aria-live="polite">
      {entries.map((entry, index) => (
        <p key={index}>{entry}</p>
      ))}
    </section>
  )
}