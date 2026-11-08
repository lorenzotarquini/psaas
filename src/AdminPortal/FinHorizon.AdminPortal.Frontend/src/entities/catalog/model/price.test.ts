import { describe, expect, it } from 'vitest'
import { formatPrice } from './price'

const per = { Monthly: 'mese', Yearly: 'anno' } as const

describe('formatPrice', () => {
  it('drops the cents of a round price and keeps them otherwise', () => {
    expect(formatPrice(99, 'Monthly', 'it', per)).toMatch(/^99\s€\/mese$/)
    expect(formatPrice(12.5, 'Yearly', 'it', per)).toMatch(/^12,50\s€\/anno$/)
  })
})
