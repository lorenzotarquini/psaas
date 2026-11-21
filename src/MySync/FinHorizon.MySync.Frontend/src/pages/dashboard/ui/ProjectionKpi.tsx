import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { faWandMagicSparkles } from '@fortawesome/free-solid-svg-icons'
import { initialParameters, project, useProjection } from '@/entities/projection'
import { isFeatureLocked, TierLocked } from '@/features/tier-lock'
import type { PortfolioScope } from '@/shared/lib/portfolio-scope'
import { Amount } from '@/shared/ui'
import { KpiCard } from './KpiCard'

/** UC4-R6: the net worth in 12 months, with the saved parameters or the suggested ones; a click opens the projection (UC14). */
export function ProjectionKpi({ scope }: { scope: PortfolioScope }) {
  const { t } = useTranslation()
  const projection = useProjection()
  const data = projection.data
  // The projection counts everything (D15): with a switch off, the label says so.
  const label = t(scope.stable && scope.investments ? 'dashboard.projectionKpi' : 'dashboard.projectionKpiTotal')
  // Contract C5: without the feature the card says so and leads to the plans.
  if (projection.isError && isFeatureLocked(projection.error)) {
    return (
      <Link to="/settings?tab=billing" className="block rounded-box transition hover:opacity-90" aria-label={label}>
        <KpiCard icon={faWandMagicSparkles} label={label}>
          <TierLocked compact />
        </KpiCard>
      </Link>
    )
  }

  const result = data ? project({ today: data.today, start: data.start, debts: data.debts, subscriptions: data.subscriptions, parameters: { ...initialParameters(data), horizonMonths: 12 } }) : undefined

  return (
    <Link to="/projection" className="block rounded-box transition hover:opacity-90" aria-label={label}>
      <KpiCard icon={faWandMagicSparkles} label={label}>
        {result && data ? <Amount value={Math.round(result.last.netWorth * 100) / 100} currency={data.currency} highlightNegative /> : <span className="loading loading-dots loading-sm" />}
      </KpiCard>
    </Link>
  )
}
