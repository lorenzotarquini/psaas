import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { initialParameters, project, useProjection, type ProjectionData, type ProjectionResult } from '@/entities/projection'
import { useFormat } from '@/shared/lib/format'
import { Amount, LoadError, Loading } from '@/shared/ui'
import { parse, toValues, type ParameterValues } from '../model/form'
import { FixedExpenses } from './FixedExpenses'
import { ParametersForm } from './ParametersForm'
import { ProjectionActions } from './ProjectionActions'
import { ProjectionChart } from './ProjectionChart'

/** UC14: where the net worth will be in a chosen number of months, recomputed in the browser at every change (R10). */
export function ProjectionPage() {
  const { t } = useTranslation()
  const projection = useProjection()
  // "Riparti dai miei dati" remounts the editor on the fresh data.
  const [generation, setGeneration] = useState(0)

  if (projection.isPending) return <Loading />
  if (projection.isError) return <LoadError />

  return (
    <div className="flex flex-col gap-6">
      <header>
        <h1 className="text-2xl font-semibold">{t('projection.title')}</h1>
        <p className="muted">{t('projection.subtitle')}</p>
      </header>
      <ProjectionEditor key={generation} data={projection.data} onReset={() => setGeneration((value) => value + 1)} />
    </div>
  )
}

function ProjectionEditor({ data, onReset }: { data: ProjectionData; onReset: () => void }) {
  const { t } = useTranslation()
  const [values, setValues] = useState<ParameterValues>(() => toValues(initialParameters(data)))
  const parsed = useMemo(() => parse(values), [values])
  // The last valid parameters keep the chart on screen while a field is being corrected.
  const [lastValid, setLastValid] = useState(() => parsed.parameters ?? initialParameters(data))
  if (parsed.parameters && parsed.parameters !== lastValid) setLastValid(parsed.parameters)
  const result = useMemo(() => project({ today: data.today, start: data.start, debts: data.debts, parameters: lastValid }), [data, lastValid])

  return (
    <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_minmax(0,1.4fr)]">
      <div className="flex flex-col gap-6">
        <StartingPoint data={data} />
        <section className="surface flex flex-col gap-3 p-5" aria-labelledby="parameters-title">
          <h2 id="parameters-title" className="surface-title">
            {t('projection.parameters.title')}
          </h2>
          {data.monthsOfData < 3 && (
            <p role="note" className="alert alert-warning alert-soft text-sm">
              {data.monthsOfData === 0 ? t('projection.noData') : t('projection.fewData', { count: data.monthsOfData })}
            </p>
          )}
          <ParametersForm values={values} errors={parsed.errors ?? {}} debts={data.debts} currency={data.currency} onChange={setValues} />
          <ProjectionActions parameters={parsed.parameters} onReset={onReset} />
        </section>
        <details className="surface p-5">
          <summary className="cursor-pointer font-medium">{t('projection.fixed.title')}</summary>
          <div className="mt-3">
            <FixedExpenses />
          </div>
        </details>
      </div>
      <div className="flex flex-col gap-6">
        <Summary result={result} currency={data.currency} horizon={lastValid.horizonMonths} />
        <section className="surface flex flex-col gap-3 p-5" aria-label={t('projection.chartLabel')}>
          <ProjectionChart points={result.points} currency={data.currency} goal={lastValid.goal} />
        </section>
        <DataTable result={result} currency={data.currency} />
      </div>
    </div>
  )
}

function StartingPoint({ data }: { data: ProjectionData }) {
  const { t } = useTranslation()
  const rows = [
    ['liquidity', data.start.liquidity],
    ['investments', data.start.investments],
    ['stable', data.start.stable],
    ['debts', data.debts.reduce((sum, debt) => sum + debt.residual, 0)],
  ] as const
  return (
    <section className="surface flex flex-col gap-2 p-5" aria-labelledby="start-title">
      <h2 id="start-title" className="surface-title">
        {t('projection.start.title')}
      </h2>
      <dl className="grid grid-cols-2 gap-2 text-sm">
        {rows.map(([key, value]) => (
          <div key={key}>
            <dt className="muted">{t(`projection.start.${key}`)}</dt>
            <dd className="font-medium">
              <Amount value={value} currency={data.currency} highlightNegative />
            </dd>
          </div>
        ))}
      </dl>
    </section>
  )
}

function Summary({ result, currency, horizon }: { result: ProjectionResult; currency: string; horizon: number }) {
  const { t } = useTranslation()
  const format = useFormat()
  const cents = (value: number) => Math.round(value * 100) / 100
  const notes: { key: string; text: string; tone: string }[] = []
  if (result.goal.status === 'reached') notes.push({ key: 'goal', text: t('projection.summary.goalReached', { month: format.month(result.goal.date) }), tone: 'text-success' })
  if (result.goal.status === 'already') notes.push({ key: 'goal', text: t('projection.summary.goalAlready'), tone: 'text-success' })
  if (result.goal.status === 'notReached') notes.push({ key: 'goal', text: t('projection.summary.goalNotReached', { count: horizon }), tone: '' })
  if (result.negativeLiquidity) notes.push({ key: 'negative', text: t('projection.summary.negativeLiquidity', { month: format.month(result.negativeLiquidity) }), tone: 'text-error' })
  for (const payoff of result.payoffs) {
    const text = payoff.withoutPayment
      ? t('projection.summary.withoutPayment', { name: payoff.name })
      : payoff.date
        ? t('projection.summary.payoff', { name: payoff.name, month: format.month(payoff.date) })
        : t('projection.summary.notPaidOff', { name: payoff.name })
    notes.push({ key: payoff.debtId, text, tone: payoff.withoutPayment ? 'text-warning' : '' })
  }

  return (
    <section className="surface flex flex-col gap-3 p-5" aria-labelledby="summary-title">
      <h2 id="summary-title" className="surface-title">
        {t('projection.summary.title')}
      </h2>
      <div className="grid gap-4 sm:grid-cols-2">
        <div>
          <div className="muted text-xs font-medium uppercase tracking-wide">{t('projection.summary.final', { month: format.month(result.last.date) })}</div>
          <div className="text-2xl font-semibold" data-testid="projection-final">
            <Amount value={cents(result.last.netWorth)} currency={currency} highlightNegative />
          </div>
        </div>
        <div>
          <div className="muted text-xs font-medium uppercase tracking-wide">{t('projection.summary.change')}</div>
          <div className="text-2xl font-semibold">
            <Amount value={cents(result.change)} currency={currency} signed />
          </div>
        </div>
      </div>
      {notes.length > 0 && (
        <ul className="flex flex-col gap-1 text-sm">
          {notes.map((note) => (
            <li key={note.key} className={note.tone}>
              {note.text}
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

function DataTable({ result, currency }: { result: ProjectionResult; currency: string }) {
  const { t } = useTranslation()
  const format = useFormat()
  const cents = (value: number) => Math.round(value * 100) / 100
  return (
    <details className="surface p-5">
      <summary className="cursor-pointer font-medium">{t('projection.tableTitle')}</summary>
      <div className="mt-3 max-h-96 overflow-auto">
        <table className="table table-sm">
          <thead>
            <tr>
              <th>{t('projection.month')}</th>
              <th className="text-right">{t('projection.netWorth')}</th>
              <th className="text-right">{t('projection.investments')}</th>
              <th className="text-right">{t('projection.debts')}</th>
            </tr>
          </thead>
          <tbody>
            {result.points.map((point) => (
              <tr key={point.date}>
                <td>{point.month === 0 ? t('projection.today') : format.month(point.date)}</td>
                <td className="text-right font-medium">
                  <Amount value={cents(point.netWorth)} currency={currency} highlightNegative />
                </td>
                <td className="text-right">
                  <Amount value={cents(point.investments)} currency={currency} />
                </td>
                <td className="text-right">
                  <Amount value={cents(point.debts)} currency={currency} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </details>
  )
}
