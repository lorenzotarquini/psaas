import { describe, expect, it } from 'vitest'
import { highlight, postalCodeRange } from './postalCodes'

describe('postalCodeRange', () => {
  it('shows a range for many codes and the code for one', () => {
    expect(postalCodeRange(['40141', '40121', '40131'])).toBe('40121–40141')
    expect(postalCodeRange(['21020'])).toBe('21020')
    expect(postalCodeRange([])).toBe('')
  })
})

describe('highlight', () => {
  it('splits the name around the typed text, ignoring case', () => {
    expect(highlight('Bologna', 'bolo')).toEqual({ before: '', match: 'Bolo', after: 'gna' })
    expect(highlight('San Lazzaro', 'laz')).toEqual({ before: 'San ', match: 'Laz', after: 'zaro' })
    expect(highlight('Roma', 'xyz')).toEqual({ before: 'Roma', match: '', after: '' })
  })
})
