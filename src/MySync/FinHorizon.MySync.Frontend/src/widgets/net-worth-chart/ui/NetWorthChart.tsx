import { useTranslation } from 'react-i18next'
import { Area, CartesianGrid, ComposedChart, Legend, Line, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import type { NetWorthPoint } from '@/entities/portfolio'
import { useFormat } from '@/shared/lib/format'
import { hiddenAmount, useAmountsHidden } from '@/shared/ui'

interface NetWorthChartProps {
  points: NetWorthPoint[]
  /** Dashboard card: net worth only, no legend. */
  compact?: boolean
}

/** UC7: net worth as the main area, the debts still to pay as a line. Colours come from theme.css. */
export function NetWorthChart({ points, compact = false }: NetWorthChartProps) {
  const { t } = useTranslation()
  const format = useFormat()
  // UC12 eye: the shape of the trend stays, the figures do not.
  const hidden = useAmountsHidden()
  const money = (value: number) => (hidden ? hiddenAmount : format.money(value))
  const tick = { fill: 'var(--chart-text)', fontSize: 12 }

  return (
    <div className={compact ? 'h-56' : 'h-80'} role="img" aria-label={t('netWorth.chartLabel')}>
      <ResponsiveContainer width="100%" height="100%">
        <ComposedChart data={points} margin={{ top: 8, right: 8, bottom: 0, left: 8 }}>
          <defs>
            <linearGradient id="net-worth-fill" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="var(--chart-net-worth)" stopOpacity={0.35} />
              <stop offset="100%" stopColor="var(--chart-net-worth)" stopOpacity={0} />
            </linearGradient>
          </defs>
          <CartesianGrid stroke="var(--chart-grid)" strokeDasharray="3 3" vertical={false} />
          <XAxis dataKey="date" tick={tick} tickFormatter={format.shortDate} tickLine={false} axisLine={false} minTickGap={24} />
          <YAxis tick={tick} tickFormatter={(value: number) => money(value)} tickLine={false} axisLine={false} width={96} />
          <Tooltip
            labelFormatter={(label: string) => format.date(label)}
            formatter={(value: number, name: string) => [money(value), name]}
            contentStyle={{ background: 'var(--color-base-100)', border: '1px solid var(--color-base-300)', borderRadius: 12 }}
          />
          {!compact && <Legend />}
          <Area
            type="monotone"
            dataKey="netWorth"
            name={t('netWorth.netWorth')}
            stroke="var(--chart-net-worth)"
            strokeWidth={2.5}
            fill="url(#net-worth-fill)"
            isAnimationActive={false}
          />
          {!compact && (
            <Line
              type="monotone"
              dataKey="debts"
              name={t('netWorth.debts')}
              stroke="var(--chart-debts)"
              dot={false}
              strokeWidth={1.5}
              isAnimationActive={false}
            />
          )}
        </ComposedChart>
      </ResponsiveContainer>
    </div>
  )
}
