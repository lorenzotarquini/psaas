import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { apiDelete, apiGet, apiPost, apiPut, ledgerKey, toQuery } from '@/shared/api'

export const transactionKinds = ['Expense', 'Income'] as const
export type TransactionKind = (typeof transactionKinds)[number]

// Mirrors TransactionView in FinHorizon.MySync.Application.
export interface Transaction {
  id: string
  kind: TransactionKind
  date: string
  amount: number
  accountId: string
  accountName: string
  categoryId: string
  categoryName: string
  debtId: string | null
  debtName: string | null
  note: string | null
  recordedAt: string
}

export interface TransactionPage {
  items: Transaction[]
  page: number
  pageSize: number
  totalCount: number
  expenseTotal: number
  incomeTotal: number
}

export interface TransactionInput {
  kind: TransactionKind
  date: string
  amount: number
  accountId: string
  categoryId: string
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

/** Effect on the balance: an expense is negative. */
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
