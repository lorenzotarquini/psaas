import { zodResolver } from '@hookform/resolvers/zod'
import { FormProvider, useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useModules, useSavePlan, vatRates, type Plan } from '@/entities/catalog'
import { ApiError } from '@/shared/api'
import { submitWithServerErrors } from '@/shared/lib/form'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { FieldError, TextField } from '@/shared/ui/form-field'
import { emptyPlan, planSchema, planToValues, toPlanRequest } from '../model/planForm'

// UC16: a plan for one module, with its price and VAT, published on Stripe when saved.
export function PlanForm({ plan, onClose }: { plan: Plan | null; onClose: () => void }) {
  const { t } = useTranslation()
  const { data: modules = [] } = useModules()
  const save = useSavePlan()
  const form = useForm({ resolver: zodResolver(planSchema), defaultValues: plan ? planToValues(plan) : emptyPlan })
  const chosen = useWatch({ control: form.control, name: 'moduleIds' }) ?? []
  const vatRate = useWatch({ control: form.control, name: 'vatRate' })
  // Contract C2: the levels the chosen module declares; a module without tiers is sold as a whole.
  const tiers = modules.find((module) => module.id === chosen[0])?.tiers ?? []

  const onSubmit = form.handleSubmit(async (values) => {
    const saved = await submitWithServerErrors(() => save.mutateAsync({ id: plan?.id ?? null, plan: toPlanRequest(values) }), form.setError)
    if (saved) onClose()
  })

  return (
    <FormProvider {...form}>
      <form onSubmit={onSubmit} noValidate className="panel flex flex-col gap-2 px-5 py-4">
        <h2 className="panel-title">{plan ? t('catalog.editPlan', { name: plan.name }) : t('catalog.newPlan')}</h2>
        {save.error && !(save.error instanceof ApiError && save.error.status === 400) && <ErrorAlert error={save.error} action="save" />}
        <div className="grid gap-x-4 md:grid-cols-3">
          <TextField name="code" label={t('catalog.code')} maxLength={30} disabled={Boolean(plan)} />
          <TextField name="name" label={t('catalog.name')} maxLength={100} />
          <TextField name="price" label={t('catalog.priceEur')} inputMode="decimal" />
          <div className="fieldset py-1.5">
            <label htmlFor="plan-period" className="fieldset-legend py-1 text-[12px] font-medium text-base-content/80">
              {t('catalog.billingPeriod')}
            </label>
            <select id="plan-period" className="select w-full" {...form.register('billingPeriod')}>
              <option value="Monthly">{t('catalog.period.Monthly')}</option>
              <option value="Yearly">{t('catalog.period.Yearly')}</option>
            </select>
          </div>
          <div className="fieldset py-1.5">
            <label htmlFor="plan-module" className="fieldset-legend py-1 text-[12px] font-medium text-base-content/80">
              {t('catalog.module')}
            </label>
            <select
              id="plan-module"
              className="select w-full"
              value={chosen[0] ?? ''}
              onChange={(event) => {
                form.setValue('moduleIds', event.target.value ? [event.target.value] : [], { shouldDirty: true, shouldValidate: true })
                form.setValue('tier', '', { shouldDirty: true })
              }}
            >
              <option value="">{t('catalog.chooseModule')}</option>
              {modules.map((module) => (
                <option key={module.id} value={module.id}>
                  {module.name}
                </option>
              ))}
            </select>
            <FieldError name="moduleIds" />
            <FieldError name="moduleIds[0]" />
          </div>
          {tiers.length > 0 && (
            <div className="fieldset py-1.5">
              <label htmlFor="plan-tier" className="fieldset-legend py-1 text-[12px] font-medium text-base-content/80">
                {t('catalog.tier')}
              </label>
              <select id="plan-tier" className="select w-full" {...form.register('tier')}>
                <option value="">{t('catalog.chooseTier')}</option>
                {tiers.map((tier) => (
                  <option key={tier.level} value={String(tier.level)}>
                    {t('catalog.tierOption', { level: tier.level, name: tier.name })}
                  </option>
                ))}
              </select>
              <FieldError name="tier" />
            </div>
          )}
          <div className="fieldset py-1.5">
            <label htmlFor="plan-vat" className="fieldset-legend py-1 text-[12px] font-medium text-base-content/80">
              {t('catalog.vatRate')}
            </label>
            <select id="plan-vat" className="select w-full" {...form.register('vatRate')}>
              {vatRates.map((rate) => (
                <option key={rate} value={String(rate)}>
                  {t('catalog.vatRateOption', { rate })}
                </option>
              ))}
            </select>
            <FieldError name="vatRate" />
          </div>
          {vatRate === '0' ? (
            <TextField name="vatNature" label={t('catalog.vatNature')} maxLength={5} placeholder="N2.1" />
          ) : (
            <label className="flex items-center gap-2 pt-7">
              <input type="checkbox" className="checkbox checkbox-sm" {...form.register('priceIncludesVat')} />
              {t('catalog.priceIncludesVat')}
            </label>
          )}
          <TextField name="trialDays" label={t('catalog.trialDaysLabel')} inputMode="numeric" />
          <TextField name="description" label={t('organizationEditor.description')} />
          <TextField name="maxUsers" label={t('catalog.maxUsers')} inputMode="numeric" placeholder={t('catalog.unlimited')} />
          <TextField name="maxProductiveUnits" label={t('catalog.maxProductiveUnits')} inputMode="numeric" placeholder={t('catalog.unlimited')} />
          <label className="flex items-center gap-2 pt-7">
            <input type="checkbox" className="checkbox checkbox-sm" {...form.register('active')} />
            {t('catalog.sellable')}
          </label>
        </div>
        {plan && !plan.published && <p className="text-[12px] text-warning">{t('catalog.notPublished')}</p>}
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" className="btn btn-ghost btn-sm" onClick={onClose}>
            {t('unsaved.discard')}
          </button>
          <button type="submit" className="btn btn-primary btn-sm" disabled={form.formState.isSubmitting}>
            {form.formState.isSubmitting && <span className="loading loading-spinner loading-xs" />}
            {t('catalog.savePlan')}
          </button>
        </div>
      </form>
    </FormProvider>
  )
}
