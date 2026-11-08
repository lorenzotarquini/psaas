import type { TFunction } from 'i18next'
import type { BillingPeriod } from '../api/catalogApi'

/** "99 €/mese" or "990 €/anno", in the language of the page. */
export function formatPrice(price: number, period: BillingPeriod, language: string, perLabel: Record<BillingPeriod, string>) {
  const amount = new Intl.NumberFormat(language, { style: 'currency', currency: 'EUR', maximumFractionDigits: price % 1 ? 2 : 0 }).format(price)
  return `${amount}/${perLabel[period]}`
}

/** "100 utenti · 10 unità", "utenti illimitati" when the plan has no limit. */
export const formatLimits = (t: TFunction, maxUsers: number | null, maxUnits: number | null) =>
  t('catalog.limits', {
    users: maxUsers === null ? t('catalog.usersUnlimited') : t('catalog.users', { count: maxUsers }),
    units: maxUnits === null ? t('catalog.unitsUnlimited') : t('catalog.units', { count: maxUnits }),
  })
