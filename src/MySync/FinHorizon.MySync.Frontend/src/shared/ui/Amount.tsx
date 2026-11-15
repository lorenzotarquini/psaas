import { useTranslation } from 'react-i18next'
import { useFormat } from '@/shared/lib/format'
import { hiddenAmount, useAmountsHidden } from './AmountVisibility'

interface AmountProps {
  value: number
  /** ISO code or crypto ticker of the amount; euro when missing (UC13). */
  currency?: string
  /** Show + for positive values and colour by sign (movements). */
  signed?: boolean
  /** Colour negative values in red (balances, net worth). */
  highlightNegative?: boolean
  className?: string
}

export function Amount({ value, currency, signed = false, highlightNegative = false, className = '' }: AmountProps) {
  const { t } = useTranslation()
  const format = useFormat()
  if (useAmountsHidden()) {
    return (
      <span className={`amount ${className}`} aria-label={t('amounts.hidden')}>
        {hiddenAmount}
      </span>
    )
  }
  const tone = signed ? (value < 0 ? 'text-error' : 'text-success') : highlightNegative && value < 0 ? 'text-error' : ''
  return <span className={`amount ${tone} ${className}`}>{signed ? format.signedMoney(value, currency) : format.money(value, currency)}</span>
}
