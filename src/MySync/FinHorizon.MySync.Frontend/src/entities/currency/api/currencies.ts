import { useQuery } from '@tanstack/react-query'
import { apiGet, apiPut } from '@/shared/api'

export type CurrencyType = 'Fiat' | 'Crypto'

// Mirrors CurrencyInfo in FinHorizon.MySync.Application (UC13).
export interface Currency {
  code: string
  name: string
  symbol: string | null
  type: CurrencyType
}

// Mirrors PreferencesView: the display currency, euro while not chosen.
export interface Preferences {
  displayCurrency: string
  chosen: boolean
}

/** UC13-R2: what an amount in a currency of this type can have after the comma. */
export const decimalsOf = (type: CurrencyType) => (type === 'Crypto' ? 8 : 2)

/** The catalog changes rarely: one read per session is enough. */
export const useCurrencies = () =>
  useQuery({ queryKey: ['currencies'], queryFn: ({ signal }) => apiGet<Currency[]>('/api/currencies', signal), staleTime: Infinity })

export const preferencesQueryKey = ['preferences'] as const

export const usePreferences = () =>
  useQuery({ queryKey: preferencesQueryKey, queryFn: ({ signal }) => apiGet<Preferences>('/api/preferences', signal) })

/** UC1, UC11: totals, trend and statistics are recomputed in the new currency. */
export const updatePreferences = (preferences: { displayCurrency: string }) => apiPut<Preferences>('/api/preferences', preferences)
