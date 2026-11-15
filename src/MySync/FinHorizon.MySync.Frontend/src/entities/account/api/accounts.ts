import { useQuery } from '@tanstack/react-query'
import { apiGet, apiPost, apiPut, ledgerKey } from '@/shared/api'
import type { PortfolioGroup } from '@/shared/lib/portfolio-scope'

// Order of the account type list in the forms.
export const accountKinds = ['CurrentAccount', 'VirtualBank', 'DepositAccount', 'Investments', 'SavingsPlan', 'VirtualCurrency', 'Cash', 'Valuables', 'Other'] as const
export type AccountKind = (typeof accountKinds)[number]

// Mirrors AccountView in FinHorizon.MySync.Application.
export interface Account {
  id: string
  kind: AccountKind
  name: string
  description: string | null
  tag: string | null
  initialBalance: number
  openedOn: string
  balance: number
  /** Day-to-day, stable (valuables) or investments and deposits: what the UC12 switches show or hide. */
  group: PortfolioGroup
  /** ISO code or crypto ticker: initial balance and balance are in it (UC13). */
  currency: string
  /** The balance in the display currency; null when the rate was not at hand. */
  displayBalance: number | null
}

/** UC13: "Moneta virtuale" holds cryptocurrencies, every other kind a traditional currency. */
export const currencyTypeOf = (kind: AccountKind) => (kind === 'VirtualCurrency' ? 'Crypto' : 'Fiat')

export interface AccountList {
  items: Account[]
  max: number
}

export interface NewAccount {
  kind: AccountKind
  name: string
  description?: string
  tag?: string
  initialBalance: number
  currency: string
}

export const accountsQueryKey = [ledgerKey, 'accounts'] as const

export const useAccounts = () =>
  useQuery({ queryKey: accountsQueryKey, queryFn: ({ signal }) => apiGet<AccountList>('/api/accounts', signal) })

export const createAccount = (account: NewAccount) => apiPost<Account>('/api/accounts', account)

/** UC10: the opening day does not change; balances and the trend are recomputed from the new values. */
export const updateAccount = (id: string, account: NewAccount) => apiPut<Account>(`/api/accounts/${id}`, account)

/** Tags already used on accounts and debts, suggested while typing. */
export const useTags = () => useQuery({ queryKey: [ledgerKey, 'tags'], queryFn: ({ signal }) => apiGet<string[]>('/api/tags', signal) })
