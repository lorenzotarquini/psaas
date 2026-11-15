import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, Navigate } from 'react-router'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faArrowDown, faArrowUp, faScaleBalanced, faFileInvoiceDollar } from '@fortawesome/free-solid-svg-icons'
import { useDashboard, useNetWorth } from '@/entities/portfolio'
import type { TransactionKind } from '@/entities/transaction'
import { TransactionDialog } from '@/features/edit-transaction'
import { useFormat } from '@/shared/lib/format'
import { Amount, EmptyState, LoadError, Loading } from '@/shared/ui'
import { NetWorthChart } from '@/widgets/net-worth-chart'
import { AccountsCard, DebtsCard } from '@/widgets/portfolio-lists'
import { TransactionTable } from '@/widgets/transaction-table'
import { KpiCard } from './KpiCard'

/** UC4: net worth today, accounts, debts, the latest movements and the 30-day trend. */
export function DashboardPage() {
  const { t } = useTranslation()
  const format = useFormat()
  const dashboard = useDashboard()
  const trend = useNetWorth('30d')
  const [newKind, setNewKind] = useState<TransactionKind | undefined>()

  if (dashboard.isPending) return <Loading />
  if (dashboard.isError) return <LoadError />
  // UC1: without accounts there is nothing to show yet. Decided on fresh data only: a cached "no accounts"
  // from before the setup would send the user back to it.
  if (dashboard.data.accounts.length === 0) return dashboard.isFetching ? <Loading /> : <Navigate to="/setup" replace />

  const { netWorth, debtsResidual, accounts, debts, recentTransactions, limits } = dashboard.data
  const change = trend.data

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">{t('dashboard.title')}</h1>
          <p className="muted">{t('dashboard.subtitle')}</p>
        </div>
        <div className="flex gap-2">
          <button type="button" className="btn btn-error btn-soft" onClick={() => setNewKind('Expense')}>
            <FontAwesomeIcon icon={faArrowUp} /> {t('transactions.newExpense')}
          </button>
          <button type="button" className="btn btn-primary" onClick={() => setNewKind('Income')}>
            <FontAwesomeIcon icon={faArrowDown} /> {t('transactions.newIncome')}
          </button>
        </div>
      </header>

      <div className="grid gap-4 sm:grid-cols-3">
        <KpiCard icon={faScaleBalanced} label={t('netWorth.netWorth')} highlight>
          <Amount value={netWorth} highlightNegative />
        </KpiCard>
        <KpiCard icon={faFileInvoiceDollar} label={t('netWorth.debts')}>
          <Amount value={debtsResidual} />
        </KpiCard>
        <KpiCard icon={faArrowUp} label={t('dashboard.change30')}>
          {change ? (
            <span className={change.change < 0 ? 'text-error' : 'text-success'}>
              {format.signedMoney(change.change)}
              {change.changePercent !== null && <span className="ml-2 text-sm">({format.percent(change.changePercent)})</span>}
            </span>
          ) : (
            <span className="loading loading-dots loading-sm" />
          )}
        </KpiCard>
      </div>

      <div className="grid gap-6 xl:grid-cols-3">
        <section className="surface flex flex-col gap-3 p-5 xl:col-span-2" aria-labelledby="trend-title">
          <header className="flex items-center justify-between">
            <h2 id="trend-title" className="surface-title">
              {t('dashboard.trendTitle')}
            </h2>
            <Link to="/net-worth" className="link link-primary text-sm">
              {t('dashboard.seeTrend')}
            </Link>
          </header>
          {trend.data && trend.data.points.length > 1 ? (
            <NetWorthChart points={trend.data.points} compact />
          ) : (
            <EmptyState>{t('netWorth.buildingUp')}</EmptyState>
          )}
        </section>
        <div className="flex flex-col gap-6">
          <AccountsCard accounts={accounts} max={limits.maxAccounts} />
          <DebtsCard debts={debts} max={limits.maxDebts} />
        </div>
      </div>

      <section className="surface flex flex-col gap-3 p-5" aria-labelledby="recent-title">
        <header className="flex items-center justify-between">
          <h2 id="recent-title" className="surface-title">
            {t('dashboard.recentTitle')}
          </h2>
          <Link to="/transactions" className="link link-primary text-sm">
            {t('dashboard.seeAll')}
          </Link>
        </header>
        {recentTransactions.length === 0 ? <EmptyState>{t('dashboard.noTransactions')}</EmptyState> : <TransactionTable items={recentTransactions} />}
      </section>

      <TransactionDialog open={newKind !== undefined} defaultKind={newKind} onClose={() => setNewKind(undefined)} />
    </div>
  )
}
