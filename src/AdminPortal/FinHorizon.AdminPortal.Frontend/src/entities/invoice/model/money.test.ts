import { describe, expect, it } from 'vitest'
import { formatMoney } from './money'

describe('formatMoney', () => {
  it('always shows the cents', () => {
    // Italian groups thousands only from five digits (CLDR): 1234,50 but 12.345,50.
    expect(formatMoney(1234.5, 'it')).toMatch(/^1234,50\s€$/)
    expect(formatMoney(12345.5, 'it')).toMatch(/^12\.345,50\s€$/)
    expect(formatMoney(122, 'en')).toBe('€122.00')
  })
})
