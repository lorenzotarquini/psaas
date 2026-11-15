import { z } from 'zod'

// Euro with at most 2 decimals, comma or dot as separator: what people type in Italy and elsewhere.
const amountPattern = /^-?\d+(?:[.,]\d{1,2})?$/

export const parseAmount = (text: string) => Number(text.trim().replace(',', '.'))

/**
 * An amount typed as text and checked with the backend's codes: Required, InvalidFormat (more than cents),
 * InvalidValue (not positive, when asked).
 */
export const amountField = ({ positive }: { positive: boolean }) =>
  z
    .string()
    .trim()
    .min(1, 'Required')
    .refine((text) => amountPattern.test(text), 'InvalidFormat')
    .refine((text) => !positive || parseAmount(text) > 0, 'InvalidValue')

/** For the form's default value: 1234.5 → "1234.50" (edited as text). */
export const amountText = (value: number | undefined) => (value === undefined ? '' : value.toFixed(2))
