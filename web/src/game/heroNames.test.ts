import { describe, expect, it } from 'vitest'
import { classLabel, randomHeroName } from './heroNames'

describe('hero names', () => {
  it('joins an opening and an ending', () => {
    expect(randomHeroName(() => 0)).toBe('Aldric')
    expect(randomHeroName(() => 0.999)).toBe('Wynis')
  })

  it('always fits the name field', () => {
    for (let index = 0; index < 50; index++) expect(randomHeroName().length).toBeLessThanOrEqual(16)
  })

  it('turns a class id into its name', () => {
    expect(classLabel('berserker')).toBe('Berserker')
    expect(classLabel(null)).toBe('Hero')
  })
})
