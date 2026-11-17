import { useTranslation } from 'react-i18next'
import { CartesianGrid, ComposedChart, Legend, Line, ReferenceLine, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import type { ProjectionPoint } from '@/entities/projection'
import { useFormat } from '@/shared/lib/format'

interface ProjectionChartProps {
  points: ProjectionPoint[]
  currency: string
  goal: number | null
}

/**
 * UC14 step 5: net worth, the invested part and the debts left, month by month. Dashed lines say it is an estimate,
 * not the real trend of UC7; the goal is a horizontal line (step 8).
 */
export function ProjectionChart({ points, currency, goal }: ProjectionChartProps) {
  const { t } = useTranslation()
  const format = useFormat()
  const money = (value: number) => format.money(Math.round(value * 100) / 100, currency)
  const label = (date: string) => (date === points[0]?.date ? t('projection.today') : format.month(date))
  const tick = { fill: 'var(--chart-text)', fontSize: 12 }

  return (
    <div className="h-80" role="img" aria-label={t('projection.chartLabel')}>
      <ResponsiveContainer width="100%" height="100%">
        <ComposedChart data={points} margin={{ top: 8, right: 8, bottom: 0, left: 8 }}>
          <CartesianGrid stroke="var(--chart-grid)" strokeDasharray="3 3" vertical={false} />
          <XAxis dataKey="date" tick={tick} tickFormatter={format.shortMonth} tickLine={false} axisLine={false} minTickGap={24} />
          <YAxis tick={tick} tickFormatter={(value: number) => money(value)} tickLine={false} axisLine={false} width={96} />
          <Tooltip
            labelFormatter={(value: string) => label(value)}
            formatter={(value: number, name: string) => [money(value), name]}
            contentStyle={{ background: 'var(--color-base-100)', border: '1px solid var(--color-base-300)', borderRadius: 12 }}
          />
          <Legend />
          <Line type="monotone" dataKey="netWorth" name={t('projection.netWorth')} stroke="var(--chart-net-worth)" strokeWidth={2.5} strokeDasharray="6 4" dot={false} isAnimationActive={false} />
          <Line type="monotone" dataKey="investments" name={t('projection.investments')} stroke="var(--color-info)" strokeWidth={1.5} strokeDasharray="4 4" dot={false} isAnimationActive={false} />
          <Line type="monotone" dataKey="debts" name={t('projection.debts')} stroke="var(--chart-debts)" strokeWidth={1.5} strokeDasharray="2 4" dot={false} isAnimationActive={false} />
          {goal !== null && <ReferenceLine y={goal} stroke="var(--color-success)" strokeDasharray="8 4" label={{ value: t('projection.goal'), fill: 'var(--chart-text)', fontSize: 12, position: 'insideTopLeft' }} />}
        </ComposedChart>
      </ResponsiveContainer>
    </div>
  )
}
