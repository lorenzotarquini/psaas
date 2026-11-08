import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { dailyBars, useTenantUsage, type UsageSeries } from '@/entities/usage'
import { ErrorAlert } from '@/shared/ui/error-alert'

const periods = [7, 30, 90] as const

// UC25 (mock 1d "Uso"): consumption against the plan limits, active users, and each module metric day by day.
export function UsageTab({ tenantId }: { tenantId: string }) {
  const { t } = useTranslation()
  const [days, setDays] = useState<number>(30)
  // The day the tab opened: the bars do not shift while it stays open.
  const [today] = useState(() => new Date())
  const { data: usage, error, isPending } = useTenantUsage(tenantId, days)

  if (error) return <ErrorAlert error={error} />
  if (isPending) return <span className="loading loading-spinner" aria-label={t('loading')} />

  return (
    <div className="flex flex-col gap-4">
      <div className="grid gap-3 md:grid-cols-3">
        <Gauge label={t('usage.users')} value={usage.users} max={usage.maxUsers} />
        <Gauge label={t('usage.activeUsers')} value={usage.activeUsers} max={usage.users} hint={t('usage.activeHint')} />
        <Gauge label={t('usage.units')} value={usage.productiveUnits} max={usage.maxProductiveUnits} />
      </div>
      <section className="panel">
        <div className="flex items-center justify-between border-b border-base-300 px-4 py-3">
          <h2 className="panel-title">{t('usage.metrics')}</h2>
          <div role="tablist" className="tabs tabs-box tabs-xs">
            {periods.map((p) => (
              <button key={p} type="button" role="tab" className={`tab ${p === days ? 'tab-active' : ''}`} onClick={() => setDays(p)}>
                {t('usage.days', { count: p })}
              </button>
            ))}
          </div>
        </div>
        {usage.metrics.length === 0 ? (
          <p className="px-4 py-8 text-center text-base-content/60">{t('usage.noMetrics')}</p>
        ) : (
          <ul className="divide-y divide-base-300">
            {usage.metrics.map((series) => (
              <MetricRow key={`${series.moduleId}/${series.metric}`} series={series} days={days} today={today} />
            ))}
          </ul>
        )}
      </section>
    </div>
  )
}

function Gauge({ label, value, max, hint }: { label: string; value: number | null; max: number | null; hint?: string }) {
  const { t } = useTranslation()
  const ratio = value !== null && max ? value / max : null
  return (
    <div className="panel px-4 py-3">
      <div className="text-[12.5px] text-base-content/60">{label}</div>
      <div className="data mt-1 text-[22px] font-semibold">
        {value ?? '–'}
        <span className="text-[14px] font-normal text-base-content/55"> / {max ?? t('usage.unlimited')}</span>
      </div>
      {ratio !== null && (
        <progress className={`progress h-1.5 w-full ${ratio >= 0.9 ? 'progress-warning' : 'progress-primary'}`} value={Math.min(ratio, 1) * 100} max={100} />
      )}
      {hint && <div className="mt-1 text-[12px] text-base-content/55">{hint}</div>}
    </div>
  )
}

// A bar per day, scaled to the highest day; the total reads as sum (counter) or latest (gauge).
function MetricRow({ series, days, today }: { series: UsageSeries; days: number; today: Date }) {
  const { t, i18n } = useTranslation()
  const bars = dailyBars(series, days, today)
  const peak = Math.max(...bars, 1)
  const number = (n: number) => n.toLocaleString(i18n.language, { maximumFractionDigits: 2 })
  return (
    <li className="grid grid-cols-[minmax(0,14rem)_1fr_auto] items-center gap-4 px-4 py-3">
      <div className="min-w-0">
        <div className="data truncate font-medium">{series.metric}</div>
        <div className="text-[12px] text-base-content/60">
          {series.moduleName} · {t(`usage.kind.${series.kind}`)}
        </div>
      </div>
      <svg viewBox={`0 0 ${bars.length * 4} 32`} preserveAspectRatio="none" className="h-8 w-full text-primary" role="img" aria-label={t('usage.chart', { metric: series.metric, days })}>
        {bars.map((value, i) => (
          <rect key={i} x={i * 4 + 0.5} width={3} y={32 - (value / peak) * 32} height={(value / peak) * 32} className="fill-current" opacity={value ? 0.85 : 0.15} />
        ))}
      </svg>
      <div className="data text-right text-[15px] font-semibold">{number(series.total)}</div>
    </li>
  )
}
