import i18next from 'i18next'
import { beforeAll, describe, expect, it } from 'vitest'
import italian from '../../../../public/locales/it/common.json'
import { ageLabel, areas } from './queue'

describe('overview queue', () => {
  beforeAll(() => i18next.init({ lng: 'it', resources: { it: { translation: italian } }, interpolation: { escapeValue: false } }))
  const now = new Date('2026-10-22T10:00:00Z')

  it('labels the age as in mock 1b', () => {
    expect(ageLabel('2026-10-22T09:48:00Z', now, i18next.t)).toBe('12 min')
    expect(ageLabel('2026-10-22T07:00:00Z', now, i18next.t)).toBe('3 h')
    expect(ageLabel('2026-10-20T10:00:00Z', now, i18next.t)).toBe('2 g')
  })

  it('puts every kind in "all" and each in at least one area', () => {
    const others = Object.entries(areas).filter(([key]) => key !== 'all').flatMap(([, kinds]) => kinds)
    expect([...areas.all].sort()).toEqual([...new Set(others)].sort())
  })
})
