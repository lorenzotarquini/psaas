import { useFormat } from '@/shared/lib/format'

interface AmountProps {
  value: number
  /** Show + for positive values and colour by sign (movements). */
  signed?: boolean
  /** Colour negative values in red (balances, net worth). */
  highlightNegative?: boolean
  className?: string
}

export function Amount({ value, signed = false, highlightNegative = false, className = '' }: AmountProps) {
  const format = useFormat()
  const tone = signed ? (value < 0 ? 'text-error' : 'text-success') : highlightNegative && value < 0 ? 'text-error' : ''
  return <span className={`amount ${tone} ${className}`}>{signed ? format.signedMoney(value) : format.money(value)}</span>
}
