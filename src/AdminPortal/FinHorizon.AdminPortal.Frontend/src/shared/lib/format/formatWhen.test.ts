import { describe, expect, it } from 'vitest'
import { formatWhen } from './formatWhen'

describe('formatWhen', () => {
  const now = new Date(2026, 9, 22, 12, 0)

  it('shows only the time for today, "ieri" for yesterday and the date before', () => {
    expect(formatWhen(new Date(2026, 9, 22, 10, 31).toISOString(), 'it', 'ieri', now)).toBe('10:31')
    expect(formatWhen(new Date(2026, 9, 21, 23, 0).toISOString(), 'it', 'ieri', now)).toBe('ieri 23:00')
    expect(formatWhen(new Date(2026, 9, 20, 15, 2).toISOString(), 'it', 'ieri', now)).toBe('20/10/2026 15:02')
  })
})
