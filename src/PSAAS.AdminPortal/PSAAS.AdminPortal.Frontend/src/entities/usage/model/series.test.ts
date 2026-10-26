import { describe, expect, it } from 'vitest'
import type { UsageSeries } from '../api/usageApi'
import { dailyBars } from './series'

const series = (kind: UsageSeries['kind'], points: [string, number][]): UsageSeries => ({
  moduleId: 'warehouse',
  moduleName: 'Magazzino',
  metric: 'm',
  kind,
  total: 0,
  points: points.map(([day, value]) => ({ day, value })),
})
const today = new Date('2026-10-20T15:00:00Z')

describe('dailyBars', () => {
  it('fills the missing days of a counter with 0', () => {
    expect(dailyBars(series('counter', [['2026-10-18', 3], ['2026-10-20', 2]]), 4, today)).toEqual([0, 3, 0, 2])
  })

  it('carries the last value of a gauge over the missing days', () => {
    expect(dailyBars(series('gauge', [['2026-10-18', 120]]), 4, today)).toEqual([0, 120, 120, 120])
  })
})
