import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { netWorthPeriods, useNetWorth, type NetWorthPeriod } from '@/entities/portfolio'
import { useFormat } from '@/shared/lib/format'
import { usePortfolioScope } from '@/shared/lib/portfolio-scope'
import { Amount, EmptyState, LoadError, Loading, ScopeSwitches } from '@/shared/ui'
import { NetWorthChart } from '@/widgets/net-worth-chart'

/** UC7: the trend over a chosen period, with the change and an accessible table of the same data (R5); the switches of UC12, off by default. */
export function NetWorthPage() {
  const { t } = useTranslation()
  const format = useFormat()
  const [params, setParams] = useSearchParams()
  const requested = params.get('period')
  const period: NetWorthPeriod = netWorthPeriods.includes(requested as NetWorthPeriod) ? (requested as NetWorthPeriod) : '30d'
  // The same choices as the dashboard, for the whole working session (UC12-R2).
  const [scope, setScope] = usePortfolioScope()
  const trend = useNetWorth(period, scope)

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">{t('netWorth.title')}</h1>
          <p className="muted">{t('netWorth.subtitle')}</p>
          <div className="mt-1 flex flex-wrap items-center gap-3">
            <ScopeSwitches scope={scope} onChange={setScope} />
          </div>
        </div>
        <div role="tablist" aria-label={t('netWorth.period')} className="tabs tabs-box">
          {netWorthPeriods.map((option) => (
            <button
              key={option}
              type="button"
              role="tab"
              aria-selected={option === period}
              className={`tab ${option === period ? 'tab-active' : ''}`}
              onClick={() => setParams({ period: option })}
            >
              {t(`netWorth.periods.${option}`)}
            </button>
          ))}
        </div>
      </header>

      {trend.isPending ? (
        <Loading />
      ) : trend.isError ? (
        <LoadError />
      ) : (
        <>
          <section className="surface flex flex-col gap-4 p-5" aria-labelledby="change-title">
            <div className="flex flex-wrap items-baseline gap-3">
              <h2 id="change-title" className="surface-title">
                {t('netWorth.change')}
              </h2>
              <span className={`text-2xl font-semibold ${trend.data.change < 0 ? 'text-error' : 'text-success'}`}>
                {format.signedMoney(trend.data.change, trend.data.currency)}
              </span>
              {trend.data.changePercent !== null && <span className="muted">{format.percent(trend.data.changePercent)}</span>}
            </div>
            {trend.data.points.length > 1 ? <NetWorthChart points={trend.data.points} currency={trend.data.currency} /> : <EmptyState>{t('netWorth.buildingUp')}</EmptyState>}
          </section>

          <details className="surface p-5">
            <summary className="cursor-pointer font-medium">{t('netWorth.tableTitle')}</summary>
            <div className="mt-3 max-h-96 overflow-auto">
              <table className="table table-sm">
                <thead>
                  <tr>
                    <th>{t('transactions.fields.date')}</th>
                    <th className="text-right">{t('netWorth.netWorth')}</th>
                    <th className="text-right">{t('netWorth.debts')}</th>
                  </tr>
                </thead>
                <tbody>
                  {[...trend.data.points].reverse().map((point) => (
                    <tr key={point.date}>
                      <td>{format.date(point.date)}</td>
                      <td className="text-right font-medium">
                        <Amount value={point.netWorth} currency={trend.data.currency} highlightNegative />
                      </td>
                      <td className="text-right">
                        <Amount value={point.debts} currency={trend.data.currency} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </details>
        </>
      )}
    </div>
  )
}
