import { z } from 'zod'

// At most 2 decimals (8 for cryptocurrencies, UC13-R2), comma or dot as separator: what people type in Italy and elsewhere.
const amountPattern = (decimals: number) => new RegExp(String.raw`^-?\d+(?:[.,]\d{1,${decimals}})?$`)

/** Whether a typed amount has no more decimals than its currency allows. */
export const fitsDecimals = (text: string, decimals: number) => amountPattern(decimals).test(text.trim())

export const parseAmount = (text: string) => Number(text.trim().replace(',', '.'))

/**
 * An amount typed as text and checked with the backend's codes: Required, InvalidFormat (more than cents),
 * InvalidValue (not positive, when asked).
 */
export const amountField = ({ positive, decimals = 2 }: { positive: boolean; decimals?: number }) =>
  z
    .string()
    .trim()
    .min(1, 'Required')
    .refine((text) => fitsDecimals(text, decimals), 'InvalidFormat')
    .refine((text) => !positive || parseAmount(text) > 0, 'InvalidValue')

/** For the form's default value: 1234.5 → "1234.50", 0.12345678 stays as it is (edited as text). */
export const amountText = (value: number | undefined) =>
  value === undefined ? '' : Number.isInteger(Math.round(value * 100 * 1e6) / 1e6) ? value.toFixed(2) : String(value)
