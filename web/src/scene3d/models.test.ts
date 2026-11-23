import { describe, expect, it } from 'vitest'
import { monsterLook } from './models'

const bestiary = [
  'rat', 'bat', 'spider', 'slime', 'ghost', 'cyclops', 'mimic', 'salamander', 'fire-imp', 'crab', 'eel', 'gem-beetle',
  'shardling', 'myconid', 'puffball', 'sporeling', 'drakeling', 'harpy', 'gargoyle', 'rat-king', 'spider-queen', 'troll',
  'dune-scorpion', 'frost-yeti', 'black-knight', 'magma-golem', 'kraken', 'crystal-golem', 'mushroom-king', 'storm-griffin',
  'custode',
]

describe('monsterLook', () => {
  it.each(bestiary)('gives %s a real model instead of a placeholder skeleton', kind => {
    const look = monsterLook(kind)

    expect(look.url.includes('skeleton')).toBe(false)
    expect(look.url !== '' || look.build !== undefined).toBe(true)
    expect(look.height).toBeGreaterThan(0)
  })

  it.each(bestiary)('maps every animation %s needs', kind => {
    const clips = monsterLook(kind).clips

    for (const clip of ['Idle', 'Sleep', 'Walk', 'Attack', 'Hit', 'Death', 'Wake', 'Spawn'] as const) expect(clips[clip]).toBeTruthy()
  })

  it('builds the procedural creatures with legs to animate', () => {
    const spider = monsterLook('spider').build!()
    let legs = 0
    spider.traverse(child => {
      if (child.userData.leg !== undefined) legs++
    })

    expect(legs).toBe(8)
  })
})
