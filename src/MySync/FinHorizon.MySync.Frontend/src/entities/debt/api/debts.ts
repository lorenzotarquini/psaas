import { useQuery } from '@tanstack/react-query'
import { apiGet, apiPost, apiPut, ledgerKey } from '@/shared/api'

export const debtKinds = ['Loan', 'Mortgage', 'Overdraft', 'Other'] as const
export type DebtKind = (typeof debtKinds)[number]

// Mirrors DebtView in FinHorizon.MySync.Application.
export interface Debt {
  id: string
  kind: DebtKind
  name: string
  description: string | null
  tag: string | null
  accountId: string
  initialResidual: number
  residual: number
  createdOn: string
  paidOff: boolean
}

export interface DebtList {
  items: Debt[]
  max: number
}

export interface NewDebt {
  kind: DebtKind
  name: string
  description?: string
  tag?: string
  accountId: string
  residual: number
}

export const useDebts = () =>
  useQuery({ queryKey: [ledgerKey, 'debts'], queryFn: ({ signal }) => apiGet<DebtList>('/api/debts', signal) })

export const createDebt = (debt: NewDebt) => apiPost<Debt>('/api/debts', debt)

/** UC10: `residual` is the initial residual; it cannot go below the installments already paid. */
export const updateDebt = (id: string, debt: NewDebt) => apiPut<Debt>(`/api/debts/${id}`, debt)
