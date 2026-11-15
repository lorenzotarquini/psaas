import { useTranslation } from 'react-i18next'

/**
 * Locale-aware formatting through i18next's built-in Intl formatters: the patterns live in the
 * translation files ("format.*"), so each language decides how money, dates and percentages look.
 */
export function useFormat() {
  const { t } = useTranslation()
  return {
    money: (value: number) => t('format.money', { value }),
    /** Sign always shown: +1.800,00 € / -85,40 €. */
    signedMoney: (value: number) => t('format.signedMoney', { value }),
    /** API dates are calendar dates (yyyy-MM-dd): formatted in UTC so no time zone shifts the day. */
    date: (isoDate: string) => t('format.date', { value: new Date(`${isoDate}T00:00:00Z`) }),
    shortDate: (isoDate: string) => t('format.shortDate', { value: new Date(`${isoDate}T00:00:00Z`) }),
    percent: (value: number) => t('format.percent', { value: value / 100 }),
  }
}
