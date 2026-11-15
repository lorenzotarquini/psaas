import type { SelectHTMLAttributes } from 'react'
import { useCurrencies, type CurrencyType } from '../api/currencies'

interface CurrencySelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  /** Only traditional currencies or only cryptocurrencies (UC13). */
  type: CurrencyType
}

/** A select of the catalog of one type, "EUR · Euro"; works with react-hook-form's register. */
export function CurrencySelect({ type, className = '', ...select }: CurrencySelectProps) {
  const currencies = useCurrencies()
  const options = currencies.data?.filter((currency) => currency.type === type) ?? []
  return (
    <select className={`select w-full ${className}`} disabled={currencies.isPending || select.disabled} {...select}>
      {options.map((currency) => (
        <option key={currency.code} value={currency.code}>
          {currency.code} · {currency.name}
        </option>
      ))}
    </select>
  )
}
