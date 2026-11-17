import { useQuery } from '@tanstack/react-query'
import { apiGet, apiPost, apiPut, ledgerKey } from '@/shared/api'
import type { PortfolioGroup } from '@/shared/lib/portfolio-scope'

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
  /** UC3-R8: what the user pays every month, when given; only the projection reads it (UC14). */
  monthlyPayment?: number | null
  createdOn: string
  paidOff: boolean
  /** Day-to-day or long-term (mortgages): what the UC12 stable switch shows or hides. */
  group: PortfolioGroup
  /** A traditional currency: residuals and installments are in it (UC13). */
  currency: string
  displayResidual: number | null
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
  monthlyPayment?: number
  currency: string
}

export const useDebts = () =>
  useQuery({ queryKey: [ledgerKey, 'debts'], queryFn: ({ signal }) => apiGet<DebtList>('/api/debts', signal) })

export const createDebt = (debt: NewDebt) => apiPost<Debt>('/api/debts', debt)

/** UC10: `residual` is the initial residual; it cannot go below the installments already paid. */
export const updateDebt = (id: string, debt: NewDebt) => apiPut<Debt>(`/api/debts/${id}`, debt)
