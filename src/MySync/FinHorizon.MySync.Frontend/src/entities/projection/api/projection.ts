import { useQuery } from '@tanstack/react-query'
import { apiDelete, apiGet, apiPut, ledgerKey } from '@/shared/api'

export interface ProjectionPayment {
  debtId: string
  amount: number
}

export interface ProjectionEvent {
  /** 1 is the month after today (UC14-R2). */
  month: number
  /** Positive: an income; negative: an expense. */
  amount: number
  note: string
}

/** UC14: what the user sets; percentages as numbers (2 = 2%). Mirrors the body of PUT /api/projection. */
export interface ProjectionParameters {
  horizonMonths: number
  income: number
  fixedExpenses: number
  variableExpenses: number
  saving: number
  annualReturn: number
  inflation: number
  inflationOn: boolean
  payments: ProjectionPayment[]
  events: ProjectionEvent[]
  goal: number | null
}

export interface ProjectionStart {
  liquidity: number
  investments: number
  stable: number
}

export interface ProjectionDebt {
  id: string
  name: string
  residual: number
  monthlyPayment: number | null
}

// Mirrors ProjectionView in FinHorizon.MySync.Application.
export interface ProjectionData {
  /** The backend's today (yyyy-MM-dd): month 1 is the calendar month after it. */
  today: string
  start: ProjectionStart
  debts: ProjectionDebt[]
  suggested: Omit<ProjectionParameters, 'horizonMonths' | 'inflationOn' | 'events' | 'goal'>
  /** Whole months the suggestions average over, 0 to 6 (UC14-R3). */
  monthsOfData: number
  saved: ProjectionParameters | null
  currency: string
}

/** Under the ledger key: a new movement changes the starting point and the suggestions. */
export const projectionQueryKey = [ledgerKey, 'projection'] as const

export const useProjection = () => useQuery({ queryKey: projectionQueryKey, queryFn: ({ signal }) => apiGet<ProjectionData>('/api/projection', signal) })

export const saveProjection = (parameters: ProjectionParameters) => apiPut<ProjectionParameters>('/api/projection', parameters)

/** UC14-10b "Riparti dai miei dati". */
export const resetProjection = () => apiDelete('/api/projection')

/** UC14 step 3 and R11: the saved parameters, or the suggested ones; every active debt gets its saved payment or the suggested one. */
export function initialParameters(data: ProjectionData): ProjectionParameters {
  const { suggested, saved, debts } = data
  const payment = (debtId: string) =>
    saved?.payments.find((item) => item.debtId === debtId)?.amount ?? suggested.payments.find((item) => item.debtId === debtId)?.amount ?? 0
  return {
    horizonMonths: saved?.horizonMonths ?? 12,
    income: saved?.income ?? suggested.income,
    fixedExpenses: saved?.fixedExpenses ?? suggested.fixedExpenses,
    variableExpenses: saved?.variableExpenses ?? suggested.variableExpenses,
    saving: saved?.saving ?? suggested.saving,
    annualReturn: saved?.annualReturn ?? suggested.annualReturn,
    inflation: saved?.inflation ?? suggested.inflation,
    inflationOn: saved?.inflationOn ?? false,
    payments: debts.map((debt) => ({ debtId: debt.id, amount: payment(debt.id) })),
    events: saved?.events ?? [],
    goal: saved?.goal ?? null,
  }
}
