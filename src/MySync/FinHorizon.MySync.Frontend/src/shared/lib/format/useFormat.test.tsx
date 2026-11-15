import { renderHook } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { useFormat } from './useFormat'

// RNF3, RNF4, RNF6: euro amounts and calendar dates formatted by the locale files through i18next (Intl).
describe('useFormat (it)', () => {
  const { result } = renderHook(() => useFormat())
  const normalize = (text: string) => text.replace(/\s/g, ' ')

  it('formats euro with comma decimals', () => expect(normalize(result.current.money(-117500))).toBe('-117.500,00 €'))

  it('shows the sign of a movement', () => {
    expect(normalize(result.current.signedMoney(1800))).toBe('+1800,00 €')
    expect(normalize(result.current.signedMoney(-85.4))).toBe('-85,40 €')
  })

  it('keeps the calendar day whatever the time zone', () => expect(result.current.date('2026-11-15')).toBe('15 nov 2026'))

  it('formats a percentage change', () => expect(result.current.percent(5)).toBe('+5%'))
})
