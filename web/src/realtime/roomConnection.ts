import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import type { EquipmentSlot, ItemMark, StatPoint, StatsPreview } from '../api/types'
import type { MoveInput } from './input'
import type { LiveActionResponse, LiveSnapshot, LiveWelcome } from './types'

export interface RoomHandlers {
  onSnapshot: (snapshot: LiveSnapshot, receivedAt: number) => void
  onClosed: (reason: string) => void
}

export class RoomConnection {
  private readonly connection: HubConnection
  private closing = false

  constructor(handlers: RoomHandlers) {
    this.connection = new HubConnectionBuilder().withUrl('/hubs/room').configureLogging(LogLevel.Warning).build()
    this.connection.on('Snapshot', (snapshot: LiveSnapshot) => handlers.onSnapshot(snapshot, performance.now()))
    this.connection.on('Closed', (reason: string) => handlers.onClosed(reason))
    this.connection.onclose(error => {
      if (!this.closing) handlers.onClosed(error ? 'The connection to the vault was lost.' : 'The room closed.')
    })
  }

  async create(classId: string, heroName: string): Promise<LiveWelcome> {
    await this.connection.start()
    return this.connection.invoke<LiveWelcome>('Create', classId, heroName)
  }

  async join(code: string, classId: string, heroName: string): Promise<LiveWelcome> {
    await this.connection.start()
    return this.connection.invoke<LiveWelcome>('Join', code, classId, heroName)
  }

  send(input: MoveInput) {
    if (this.connection.state !== HubConnectionState.Connected) return
    void this.connection.send('Input', input.x, input.y, input.attack, input.aimX, input.aimY).catch(() => undefined)
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

  preview(entryId: number, hand?: EquipmentSlot): Promise<StatsPreview> {
    return this.connection.invoke<StatsPreview>('Preview', entryId, hand ?? null)
  }

  private act(method: string, ...args: unknown[]): Promise<LiveActionResponse> {
    return this.connection.invoke<LiveActionResponse>(method, ...args)
  }

  async leave() {
    this.closing = true
    await this.connection.stop()
  }
}
