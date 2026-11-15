import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { apiGet, ledgerKey } from '@/shared/api'
import { scopeQuery, type PortfolioScope } from '@/shared/lib/portfolio-scope'

const everything: PortfolioScope = { stable: true, investments: true }
import type { Account } from '@/entities/account/@x/portfolio'
import type { Debt } from '@/entities/debt/@x/portfolio'
import type { Transaction } from '@/entities/transaction/@x/portfolio'

// Mirrors DashboardView in FinHorizon.MySync.Application.
export interface Dashboard {
  /** The balances of all accounts; debts are not subtracted. */
  netWorth: number
  debtsResidual: number
  accounts: Account[]
  debts: Debt[]
  recentTransactions: Transaction[]
  limits: { maxAccounts: number; maxDebts: number }
}

export const netWorthPeriods = ['30d', '90d', '12m', 'all'] as const
export type NetWorthPeriod = (typeof netWorthPeriods)[number]

export interface NetWorthPoint {
  date: string
  netWorth: number
  debts: number
}

// Mirrors NetWorthTrendView.
export interface NetWorthTrend {
  period: NetWorthPeriod
  points: NetWorthPoint[]
  change: number
  changePercent: number | null
}

/** UC12: the totals count the groups in the scope; the lists stay complete and carry each item's group. */
export const useDashboard = (scope: PortfolioScope = everything) =>
  useQuery({
    queryKey: [ledgerKey, 'dashboard', scope],
    queryFn: ({ signal }) => apiGet<Dashboard>(`/api/dashboard?${scopeQuery(scope)}`, signal),
    placeholderData: keepPreviousData,
  })

export const useNetWorth = (period: NetWorthPeriod, scope: PortfolioScope = everything) =>
  useQuery({
    queryKey: [ledgerKey, 'net-worth', period, scope],
    queryFn: ({ signal }) => apiGet<NetWorthTrend>(`/api/net-worth?period=${period}&${scopeQuery(scope)}`, signal),
    placeholderData: keepPreviousData,
  })
