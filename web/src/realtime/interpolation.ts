export interface Frame<T> {
  time: number
  items: Map<number, T>
}

export interface Placed {
  x: number
  y: number
  moving: boolean
}

export const renderDelaySeconds = 0.12
export const maxExtrapolationSeconds = 0.15
const keptFrames = 12
const keptGaps = 40
const offsetEase = 0.05
const offsetSnapSeconds = 0.25

export class ClockOffset {
  private gaps: number[] = []
  private current: number | null = null

  observe(serverTime: number, receivedAt: number) {
    this.gaps.push(receivedAt / 1000 - serverTime)
    if (this.gaps.length > keptGaps) this.gaps.shift()

    const target = Math.min(...this.gaps)
    if (this.current === null || Math.abs(target - this.current) > offsetSnapSeconds) this.current = target
    else this.current += (target - this.current) * offsetEase
  }

  get value(): number | null {
    return this.current
  }
}

export class FrameBuffer<T extends Placed> {
  private frames: Frame<T>[] = []
  private readonly clock = new ClockOffset()

  push(serverTime: number, items: T[], receivedAt: number) {
    this.clock.observe(serverTime, receivedAt)
    this.frames.push({ time: serverTime, items: new Map(items.map(item => [(item as unknown as { id: number }).id, item])) })
    if (this.frames.length > keptFrames) this.frames.shift()
  }

  latest(): Frame<T> | undefined {
    return this.frames[this.frames.length - 1]
  }

  renderTime(now: number): number {
    const offset = this.clock.value
    return offset === null ? 0 : now / 1000 - offset - renderDelaySeconds
  }

  sample(id: number, now: number, leadSeconds = 0): Placed | undefined {
    return sampleFrames(this.frames, id, this.renderTime(now) + leadSeconds)
  }
}

export function sampleFrames<T extends Placed>(frames: Frame<T>[], id: number, time: number): Placed | undefined {
  if (frames.length === 0) return undefined

  const after = frames.findIndex(frame => frame.time >= time)
  if (after === -1) return extrapolate(frames, id, time)

  const next = frames[after].items.get(id)
  const previousFrame = after > 0 ? frames[after - 1] : undefined
  const previous = previousFrame?.items.get(id)

  if (!next) return previous ? { x: previous.x, y: previous.y, moving: false } : undefined
  if (!previous || !previousFrame || frames[after].time <= previousFrame.time) return { x: next.x, y: next.y, moving: next.moving }

  const span = frames[after].time - previousFrame.time
  const alpha = Math.min(1, Math.max(0, (time - previousFrame.time) / span))
  return {
    x: previous.x + (next.x - previous.x) * alpha,
    y: previous.y + (next.y - previous.y) * alpha,
    moving: next.moving || previous.moving,
  }
}

function extrapolate<T extends Placed>(frames: Frame<T>[], id: number, time: number): Placed | undefined {
  const lastFrame = frames[frames.length - 1]
  const last = lastFrame.items.get(id)
  if (!last) return undefined

  const previousFrame = frames.length > 1 ? frames[frames.length - 2] : undefined
  const previous = previousFrame?.items.get(id)
  if (!previous || !previousFrame || !last.moving || lastFrame.time <= previousFrame.time) return { x: last.x, y: last.y, moving: last.moving }

  const span = lastFrame.time - previousFrame.time
  const ahead = Math.min(time - lastFrame.time, maxExtrapolationSeconds)
  return {
    x: last.x + ((last.x - previous.x) / span) * ahead,
    y: last.y + ((last.y - previous.y) / span) * ahead,
    moving: true,
  }
}
