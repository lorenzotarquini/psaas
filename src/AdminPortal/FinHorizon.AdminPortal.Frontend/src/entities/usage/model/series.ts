import type { UsageSeries } from '../api/usageApi'

/**
 * One bar per day of the period, oldest first: missing days are 0 for a counter and repeat the last value for a gauge
 * (a gauge does not drop to 0 because nothing was reported that day).
 */
export function dailyBars(series: UsageSeries, days: number, today: Date): number[] {
  const byDay = new Map(series.points.map((p) => [p.day, p.value]))
  const bars: number[] = []
  let last = 0
  for (let i = days - 1; i >= 0; i--) {
    const day = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), today.getUTCDate() - i)).toISOString().slice(0, 10)
    const value = byDay.get(day)
    if (value !== undefined) last = value
    bars.push(value ?? (series.kind === 'gauge' ? last : 0))
  }
  return bars
}
