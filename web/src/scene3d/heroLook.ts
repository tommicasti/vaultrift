import type { EquippedItem, ItemInfo } from '../api/types'
import type { Outfit } from './Actor'
import { offHandModel, weaponModel } from './models'

const elementCloth: Record<string, string> = {
  Fire: '#d8452a',
  Ice: '#5ab0e8',
  Lightning: '#e8c83a',
  Wind: '#5ab05a',
}

export interface HeroGear {
  mainHand?: ItemInfo
  offHand?: ItemInfo
  mainHandModel?: string
  offHandModel?: string
  outfit: Outfit
}

export function heroGear(equipment: EquippedItem[]): HeroGear {
  const worn = (slot: string) => equipment.find(equipped => equipped.slot === slot)?.item
  const mainHand = worn('MainHand')
  const offHand = worn('OffHand')
  const cloak = worn('Cloak')

  return {
    mainHand,
    offHand,
    mainHandModel: weaponModel(mainHand),
    offHandModel: mainHand?.isTwin ? 'axe' : mainHand?.twoHanded ? undefined : offHandModel(offHand),
    outfit: {
      head: worn('Head') !== undefined,
      cloak: cloak ? (cloak.element ? elementCloth[cloak.element] : '') : null,
      body: worn('Body') !== undefined,
      feet: worn('Feet') !== undefined,
    },
  }
}
