import { useTranslation } from 'react-i18next'
import { CurrencySelect, usePreferences } from '@/entities/currency'
import { useSaveDisplayCurrency } from '../model/useSaveDisplayCurrency'

/** UC11, UC13: the currency of net worth, debts, trend and totals; saved as soon as it changes. */
export function DisplayCurrencySetting() {
  const { t } = useTranslation()
  const preferences = usePreferences()
  const save = useSaveDisplayCurrency()
  return (
    <div className="flex flex-col gap-1">
      <label htmlFor="display-currency" className="text-sm font-medium">
        {t('currency.display')}
      </label>
      <CurrencySelect
        id="display-currency"
        type="Fiat"
        className="max-w-xs"
        aria-describedby="display-currency-hint"
        value={save.variables ?? preferences.data?.displayCurrency ?? 'EUR'}
        disabled={preferences.isPending || save.isPending}
        onChange={(event) => save.mutate(event.target.value)}
      />
      <span id="display-currency-hint" className="muted text-xs">
        {save.isError ? t('errors.saveFailed') : t('currency.displayHint')}
      </span>
    </div>
  )
}
