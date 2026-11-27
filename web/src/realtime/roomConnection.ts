import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import type { EquipmentSlot, ItemMark, StatPoint, StatsPreview } from '../api/types'
import type { MoveInput } from './input'
import type { LiveActionResponse, LiveMonster, LiveSnapshot, LiveWelcome } from './types'

export interface RoomHandlers {
  onSnapshot: (snapshot: LiveSnapshot, receivedAt: number) => void
  onClosed: (reason: string) => void
  onReconnecting: () => void
  onResumed: (welcome: LiveWelcome) => void
}

export const liveProtocol = 3
export const outdatedMessage = 'The game and the server are different versions: restart the server, then reload this page.'

export const reconnectDelays = [0, 1000, 2000, 3000, 4000, 5000, 5000, 5000]
export const lostMessage = 'The connection to the vault was lost.'

export function expandMonsters(known: Map<number, LiveMonster>, snapshot: LiveSnapshot): LiveMonster[] {
  if (snapshot.full) known.clear()
  for (const id of snapshot.gone ?? []) known.delete(id)
  for (const monster of snapshot.monsters) known.set(monster.id, monster)
  return [...known.values()]
}

export class RoomConnection {
  private readonly connection: HubConnection
  private readonly monsters = new Map<number, LiveMonster>()
  private closing = false
  private resumeToken: string | null = null

  constructor(handlers: RoomHandlers) {
    this.connection = new HubConnectionBuilder()
      .withUrl('/hubs/room')
      .withAutomaticReconnect(reconnectDelays)
      .configureLogging(LogLevel.Warning)
      .build()
    this.connection.serverTimeoutInMilliseconds = 15000
    this.connection.keepAliveIntervalInMilliseconds = 5000
    this.connection.on('Snapshot', (snapshot: LiveSnapshot) => handlers.onSnapshot({ ...snapshot, monsters: expandMonsters(this.monsters, snapshot) }, performance.now()))
    this.connection.on('Closed', (reason: string) => {
      this.resumeToken = null
      handlers.onClosed(reason)
    })
    this.connection.onreconnecting(() => {
      if (!this.closing && this.resumeToken) handlers.onReconnecting()
    })
    this.connection.onreconnected(() => {
      if (this.closing) return
      if (!this.resumeToken) {
        handlers.onClosed(lostMessage)
        void this.leave()
        return
      }
      this.connection
        .invoke<LiveWelcome>('Resume', this.resumeToken)
        .then(welcome => this.remember(welcome))
        .then(welcome => handlers.onResumed(welcome))
        .catch((error: unknown) => {
          this.resumeToken = null
          handlers.onClosed(error instanceof Error ? cleanHubMessage(error.message) : lostMessage)
          void this.leave()
        })
    })
    this.connection.onclose(error => {
      if (!this.closing) handlers.onClosed(error ? lostMessage : 'The room closed.')
    })
  }

  async create(heroId: string, worldId: string, shared: boolean): Promise<LiveWelcome> {
    await this.connection.start()
    return this.remember(await this.connection.invoke<LiveWelcome>('Create', heroId, worldId, shared))
  }

  async join(code: string, heroId: string): Promise<LiveWelcome> {
    await this.connection.start()
    return this.remember(await this.connection.invoke<LiveWelcome>('Join', code, heroId))
  }

  private remember(welcome: LiveWelcome): LiveWelcome {
    if (welcome.protocol !== liveProtocol) {
      void this.leave()
      throw new Error(outdatedMessage)
    }
    this.resumeToken = welcome.resumeToken
    return welcome
  }

  send(input: MoveInput) {
    if (this.connection.state !== HubConnectionState.Connected) return
    void this.connection.send('Input', input.x, input.y, input.attack, input.aimX, input.aimY, input.sprint).catch(() => undefined)
  }

  equip(entryId: number, hand?: EquipmentSlot) {
    return this.act('Equip', entryId, hand ?? null)
  }

  unequip(slot: EquipmentSlot) {
    return this.act('Unequip', slot)
  }

  use(entryId: number) {
    return this.act('Use', entryId)
  }

  useQuickSlot(slot: number) {
    return this.act('UseQuickSlot', slot)
  }

  assignQuickSlot(entryId: number, slot: number) {
    return this.act('AssignQuickSlot', entryId, slot)
  }

  spendPoint(stat: StatPoint) {
    return this.act('SpendPoint', stat)
  }

  discard(entryId: number) {
    return this.act('Discard', entryId)
  }

  discardJunk() {
    return this.act('DiscardJunk')
  }

  mark(entryId: number, mark: ItemMark) {
    return this.act('Mark', entryId, mark)
  }

  learn(entryId: number, key: number) {
    return this.act('Learn', entryId, key)
  }

  ability() {
    return this.act('Ability')
  }

  selectSpell(index: number) {
    return this.act('SelectSpell', index)
  }

  buy(offerId: number) {
    return this.act('Buy', offerId)
  }

  buyUpgrade() {
    return this.act('BuyUpgrade')
  }

  sell(entryId: number) {
    return this.act('Sell', entryId)
  }

  sellJunk() {
    return this.act('SellJunk')
  }

  travel(floor: number): Promise<void> {
    return this.connection.invoke<void>('Travel', floor)
  }

  preview(entryId: number, hand?: EquipmentSlot): Promise<StatsPreview> {
    return this.connection.invoke<StatsPreview>('Preview', entryId, hand ?? null)
  }

  previewOffer(offerId: number): Promise<StatsPreview> {
    return this.connection.invoke<StatsPreview>('PreviewOffer', offerId, null)
  }

  async ping(): Promise<number> {
    const sentAt = performance.now()
    await this.connection.invoke<number>('Ping', Math.round(sentAt))
    return performance.now() - sentAt
  }

  private act(method: string, ...args: unknown[]): Promise<LiveActionResponse> {
    return this.connection.invoke<LiveActionResponse>(method, ...args)
  }

  async leave() {
    this.closing = true
    if (this.connection.state === HubConnectionState.Connected) await this.connection.invoke('Leave').catch(() => undefined)
    await this.connection.stop()
  }
}

export function cleanHubMessage(message: string): string {
  const marker = 'HubException: '
  const at = message.lastIndexOf(marker)
  return at >= 0 ? message.slice(at + marker.length) : message
}
