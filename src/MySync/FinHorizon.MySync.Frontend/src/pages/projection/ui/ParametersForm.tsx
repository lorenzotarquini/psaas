import { useTranslation } from 'react-i18next'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlus, faTrash } from '@fortawesome/free-solid-svg-icons'
import type { ProjectionDebt, ProjectionSubscription } from '@/entities/projection'
import { useErrorText } from '@/shared/lib/form'
import { Field } from '@/shared/ui'
import { limits, type ParameterErrors, type ParameterValues } from '../model/form'

interface ParametersFormProps {
  values: ParameterValues
  errors: ParameterErrors
  debts: ProjectionDebt[]
  subscriptions: ProjectionSubscription[]
  currency: string
  onChange: (values: ParameterValues) => void
}

const horizons = [6, 12, 60, 120, 360] as const

/** UC14 steps 3–9: every parameter of the projection; each change is passed up at once, so the chart follows (R10). */
export function ParametersForm({ values, errors, debts, subscriptions, currency, onChange }: ParametersFormProps) {
  const { t } = useTranslation()
  const errorText = useErrorText()
  const update = (patch: Partial<ParameterValues>) => onChange({ ...values, ...patch })

  const money = (field: 'income' | 'fixedExpenses' | 'variableExpenses' | 'saving' | 'goal', hint?: string) => (
    <Field label={t(`projection.parameters.${field}`)} htmlFor={`projection-${field}`} hint={hint} error={errorText(errors[field])}>
      <label className="input w-full">
        <span className="muted">{currency}</span>
        <input
          id={`projection-${field}`}
          inputMode="decimal"
          autoComplete="off"
          aria-invalid={!!errors[field]}
          value={values[field]}
          onChange={(event) => update({ [field]: event.target.value })}
        />
      </label>
    </Field>
  )

  const percent = (field: 'annualReturn' | 'inflation') => (
    <Field label={t(`projection.parameters.${field}`)} htmlFor={`projection-${field}`} error={errorText(errors[field])}>
      <input
        id={`projection-${field}`}
        className="input w-full"
        inputMode="decimal"
        autoComplete="off"
        aria-invalid={!!errors[field]}
        value={values[field]}
        onChange={(event) => update({ [field]: event.target.value })}
      />
    </Field>
  )

  const setEvent = (index: number, patch: Partial<ParameterValues['events'][number]>) =>
    update({ events: values.events.map((item, position) => (position === index ? { ...item, ...patch } : item)) })

  return (
    <div className="flex flex-col gap-4">
      <div>
        <Field label={t('projection.parameters.horizon')} htmlFor="projection-horizon" error={errorText(errors.horizonMonths)}>
          <input
            id="projection-horizon"
            className="input w-full"
            inputMode="numeric"
            autoComplete="off"
            aria-invalid={!!errors.horizonMonths}
            value={values.horizonMonths}
            onChange={(event) => update({ horizonMonths: event.target.value })}
          />
        </Field>
        <div className="flex flex-wrap gap-1" role="group" aria-label={t('projection.parameters.horizonShortcuts')}>
          {horizons.map((months) => (
            <button
              key={months}
              type="button"
              className={`btn btn-xs ${values.horizonMonths === String(months) ? 'btn-primary' : 'btn-ghost'}`}
              aria-pressed={values.horizonMonths === String(months)}
              onClick={() => update({ horizonMonths: String(months) })}
            >
              {t(`projection.horizons.${months}`)}
            </button>
          ))}
        </div>
      </div>

      <div className="grid gap-x-4 sm:grid-cols-2">
        {money('income')}
        {money('fixedExpenses')}
        {money('variableExpenses')}
        {money('saving', t('projection.parameters.savingHint'))}
        {percent('annualReturn')}
        {percent('inflation')}
      </div>

      <label className="label cursor-pointer gap-2">
        <input type="checkbox" className="toggle toggle-sm toggle-primary" checked={values.inflationOn} onChange={(event) => update({ inflationOn: event.target.checked })} />
        {t('projection.parameters.inflationOn')}
      </label>

      {debts.length > 0 && (
        <div className="grid gap-x-4 sm:grid-cols-2">
          {debts.map((debt) => {
            const error = errors[`payments.${debt.id}`]
            return (
              <Field key={debt.id} label={t('projection.parameters.payment', { name: debt.name })} htmlFor={`projection-payment-${debt.id}`} error={errorText(error)}>
                <label className="input w-full">
                  <span className="muted">{currency}</span>
                  <input
                    id={`projection-payment-${debt.id}`}
                    inputMode="decimal"
                    autoComplete="off"
                    aria-invalid={!!error}
                    value={values.payments[debt.id] ?? ''}
                    onChange={(event) => update({ payments: { ...values.payments, [debt.id]: event.target.value } })}
                  />
                </label>
              </Field>
            )
          })}
        </div>
      )}

      {subscriptions.length > 0 && (
        <fieldset className="flex flex-col gap-2">
          <legend className="font-medium">{t('projection.subscriptions.title')}</legend>
          <p className="muted text-sm">{t('projection.subscriptions.hint')}</p>
          {subscriptions.map((subscription) => {
            const item = values.subscriptions[subscription.id] ?? { amount: '', included: true }
            const error = errors[`subscriptions.${subscription.id}`]
            const set = (patch: Partial<typeof item>) => update({ subscriptions: { ...values.subscriptions, [subscription.id]: { ...item, ...patch } } })
            return (
              <div key={subscription.id} className="grid grid-cols-[auto_1fr_8rem] items-center gap-2">
                <input
                  type="checkbox"
                  className="toggle toggle-sm toggle-primary"
                  aria-label={t('projection.subscriptions.keep', { name: subscription.name })}
                  checked={item.included}
                  onChange={(event) => set({ included: event.target.checked })}
                />
                <span className={item.included ? '' : 'muted line-through'}>
                  {subscription.name}
                  {subscription.endsOn && <span className="muted text-xs"> · {t('projection.subscriptions.endsOn', { date: subscription.endsOn })}</span>}
                </span>
                <div>
                  <label className="input input-sm w-full">
                    <span className="muted">{currency}</span>
                    <input
                      aria-label={t('projection.subscriptions.amount', { name: subscription.name })}
                      inputMode="decimal"
                      autoComplete="off"
                      aria-invalid={!!error}
                      disabled={!item.included}
                      value={item.amount}
                      onChange={(event) => set({ amount: event.target.value })}
                    />
                  </label>
                  {error && <p className="label text-xs text-error">{errorText(error)}</p>}
                </div>
              </div>
            )
          })}
        </fieldset>
      )}

      {money('goal', t('projection.parameters.goalHint'))}

      <fieldset className="flex flex-col gap-2">
        <legend className="font-medium">{t('projection.events.title')}</legend>
        <p className="muted text-sm">{t('projection.events.hint')}</p>
        {values.events.map((item, index) => (
          <div key={index} className="grid grid-cols-[5rem_1fr_1.5fr_auto] items-start gap-2">
            {(['month', 'amount', 'note'] as const).map((field) => {
              const error = errors[`events.${index}.${field}`]
              return (
                <div key={field}>
                  <input
                    className="input input-sm w-full"
                    aria-label={`${t(`projection.events.${field}`)} ${index + 1}`}
                    aria-invalid={!!error}
                    inputMode={field === 'note' ? 'text' : field === 'month' ? 'numeric' : 'decimal'}
                    placeholder={t(`projection.events.${field}`)}
                    value={item[field]}
                    onChange={(event) => setEvent(index, { [field]: event.target.value })}
                  />
                  {error && <p className="label text-xs text-error">{errorText(error)}</p>}
                </div>
              )
            })}
            <button
              type="button"
              className="btn btn-ghost btn-sm btn-square"
              aria-label={`${t('projection.events.remove')} ${index + 1}`}
              onClick={() => update({ events: values.events.filter((_, position) => position !== index) })}
            >
              <FontAwesomeIcon icon={faTrash} />
            </button>
          </div>
        ))}
        {values.events.length < limits.maxEvents ? (
          <button
            type="button"
            className="btn btn-ghost btn-sm self-start"
            onClick={() => update({ events: [...values.events, { month: '1', amount: '', note: '' }] })}
          >
            <FontAwesomeIcon icon={faPlus} /> {t('projection.events.add')}
          </button>
        ) : (
          <p className="muted text-sm">{t('projection.events.max')}</p>
        )}
      </fieldset>
    </div>
  )
}
