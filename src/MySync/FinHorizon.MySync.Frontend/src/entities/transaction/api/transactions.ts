import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { apiDelete, apiGet, apiPost, apiPut, ledgerKey, toQuery } from '@/shared/api'

export const transactionKinds = ['Expense', 'Income', 'Transfer'] as const
export type TransactionKind = (typeof transactionKinds)[number]

/** Kinds that carry a category (UC5, UC6); a transfer moves money between two accounts instead (UC9). */
export const categorizedKinds = ['Expense', 'Income'] as const
export type CategorizedKind = (typeof categorizedKinds)[number]

// Mirrors TransactionView in FinHorizon.MySync.Application.
export interface Transaction {
  id: string
  kind: TransactionKind
  date: string
  amount: number
  /** The account of an expense or income; the source of a transfer. */
  accountId: string
  accountName: string
  /** The destination of a transfer. */
  counterAccountId: string | null
  counterAccountName: string | null
  categoryId: string | null
  categoryName: string | null
  debtId: string | null
  debtName: string | null
  note: string | null
  recordedAt: string
  /** The currency of the account: `amount` is in it (UC13). */
  currency: string
  /** What reached the destination of a transfer between two currencies, in `counterCurrency`. */
  counterAmount: number | null
  counterCurrency: string | null
}

export interface TransactionPage {
  items: Transaction[]
  page: number
  pageSize: number
  totalCount: number
  expenseTotal: number
  incomeTotal: number
  /** The display currency of the totals (UC13). */
  currency: string
}

export interface TransactionInput {
  kind: TransactionKind
  date: string
  amount: number
  accountId: string
  counterAccountId?: string | null
  /** Only for a transfer between two currencies (UC13-R7). */
  counterAmount?: number | null
  categoryId?: string | null
  debtId?: string | null
  note?: string
}

export interface TransactionFilter {
  from?: string
  to?: string
  kind?: TransactionKind
  accountId?: string
  categoryId?: string
  debtId?: string
  page?: number
  pageSize?: number
}

export const isTransfer = (transaction: Pick<Transaction, 'kind'>) => transaction.kind === 'Transfer'

/** Effect on the user's money: an expense is negative, a transfer moves money without changing it. */
export const signedAmount = (transaction: Pick<Transaction, 'kind' | 'amount'>) =>
  transaction.kind === 'Expense' ? -transaction.amount : transaction.amount

export const useTransactions = (filter: TransactionFilter) =>
  useQuery({
    queryKey: [ledgerKey, 'transactions', filter],
    queryFn: ({ signal }) => apiGet<TransactionPage>(`/api/transactions${toQuery({ ...filter })}`, signal),
    placeholderData: keepPreviousData,
  })

export const recordTransaction = (input: TransactionInput) => apiPost<Transaction>('/api/transactions', input)

export const amendTransaction = (id: string, input: TransactionInput) => apiPut<Transaction>(`/api/transactions/${id}`, input)

export const deleteTransaction = (id: string) => apiDelete(`/api/transactions/${id}`)
