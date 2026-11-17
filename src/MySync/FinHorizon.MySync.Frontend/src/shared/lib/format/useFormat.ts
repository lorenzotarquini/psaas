import { useTranslation } from 'react-i18next'

/** Up to 8 decimals for cryptocurrencies (UC13-R2); traditional amounts never have more than 2, so they keep their cents. */
const moneyParams = (currency: string) => ({ formatParams: { value: { currency, minimumFractionDigits: 2, maximumFractionDigits: 8 } } })

/** Intl knows only 3-letter codes: tickers such as AAVE are shown as a number followed by the code. */
const hasIntlCode = (currency: string) => /^[A-Z]{3}$/.test(currency)

/**
 * Locale-aware formatting through i18next's built-in Intl formatters: the patterns live in the
 * translation files ("format.*"), so each language decides how money, dates and percentages look.
 * Money is in euro unless a currency is given (UC13).
 */
export function useFormat() {
  const { t } = useTranslation()
  const withCode = (key: 'number' | 'signedNumber', value: number, currency: string) =>
    `${t(`format.${key}`, { value, formatParams: { value: { minimumFractionDigits: 2, maximumFractionDigits: 8 } } })} ${currency}`
  return {
    money: (value: number, currency = 'EUR') =>
      hasIntlCode(currency) ? t('format.money', { value, ...moneyParams(currency) }) : withCode('number', value, currency),
    /** Sign always shown: +1.800,00 € / -85,40 €. */
    signedMoney: (value: number, currency = 'EUR') =>
      hasIntlCode(currency) ? t('format.signedMoney', { value, ...moneyParams(currency) }) : withCode('signedNumber', value, currency),
    /** API dates are calendar dates (yyyy-MM-dd): formatted in UTC so no time zone shifts the day. */
    date: (isoDate: string) => t('format.date', { value: new Date(`${isoDate}T00:00:00Z`) }),
    /** A calendar month, yyyy-MM: "dicembre 2026". */
    month: (isoMonth: string) => t('format.month', { value: new Date(`${isoMonth}-01T00:00:00Z`) }),
    shortMonth: (isoMonth: string) => t('format.shortMonth', { value: new Date(`${isoMonth}-01T00:00:00Z`) }),
    shortDate: (isoDate: string) => t('format.shortDate', { value: new Date(`${isoDate}T00:00:00Z`) }),
    percent: (value: number) => t('format.percent', { value: value / 100 }),
  }
}
