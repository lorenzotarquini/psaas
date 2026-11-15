import { describe, expect, it } from 'vitest'
import { sanitizePasted } from './sanitizePasted'

describe('sanitizePasted', () => {
  it.each([
    ['1.234.567,89', '1234567,89'],
    ['22.242', '22242'],
    ['-117.500,00 €', '-117500,00 €'],
    ['1 234,5', '1234,5'],
    ['12,5', '12,5'],
  ])('Italian: %s → %s', (pasted, expected) => expect(sanitizePasted(pasted, ',')).toBe(expected))

  it.each([
    ['1,234,567.89', '1234567.89'],
    ['22,242', '22242'],
    ['12.5', '12.5'],
  ])('English: %s → %s', (pasted, expected) => expect(sanitizePasted(pasted, '.')).toBe(expected))
})
