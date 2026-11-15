import { describe, expect, it } from 'vitest'
import { amountField, parseAmount } from './amount'

// UC2-R4, UC5-R2: the same codes as the backend.
describe('amountField', () => {
  const positive = amountField({ positive: true })
  const anySign = amountField({ positive: false })
  const code = (schema: typeof positive, text: string) => schema.safeParse(text).error?.issues[0]?.message

  it.each([
    ['', 'Required'],
    ['10,005', 'InvalidFormat'],
    ['abc', 'InvalidFormat'],
    ['0', 'InvalidValue'],
    ['-10', 'InvalidValue'],
  ])('refuses %j with %s', (text, expected) => expect(code(positive, text)).toBe(expected))

  it.each(['85,40', '85.4', '1800'])('accepts %j', (text) => expect(positive.safeParse(text).success).toBe(true))

  it('accepts a negative balance where allowed (D7)', () => expect(anySign.safeParse('-300').success).toBe(true))

  it('reads the comma as decimal separator', () => expect(parseAmount(' 85,40 ')).toBe(85.4))
})
