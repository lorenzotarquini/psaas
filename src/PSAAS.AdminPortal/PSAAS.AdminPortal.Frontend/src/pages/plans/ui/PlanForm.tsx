import { zodResolver } from '@hookform/resolvers/zod'
import { FormProvider, useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { ModuleIcon, useModules, useSavePlan, type Plan } from '@/entities/catalog'
import { ApiError } from '@/shared/api'
import { submitWithServerErrors } from '@/shared/lib/form'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { FieldError, TextField } from '@/shared/ui/form-field'
import { emptyPlan, planSchema, planToValues, toPlanRequest } from '../model/planForm'

// UC16: a plan, its limits and the modules it includes (the core always, added by the backend).
export function PlanForm({ plan, onClose }: { plan: Plan | null; onClose: () => void }) {
  const { t } = useTranslation()
  const { data: modules = [] } = useModules()
  const save = useSavePlan()
  const form = useForm({ resolver: zodResolver(planSchema), defaultValues: plan ? planToValues(plan) : emptyPlan })
  const chosen = useWatch({ control: form.control, name: 'moduleIds' }) ?? []

  const toggle = (id: string) =>
    form.setValue('moduleIds', chosen.includes(id) ? chosen.filter((x) => x !== id) : [...chosen, id], { shouldDirty: true })

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
          <TextField name="trialDays" label={t('catalog.trialDaysLabel')} inputMode="numeric" />
          <TextField name="description" label={t('organizationEditor.description')} />
          <TextField name="maxUsers" label={t('catalog.maxUsers')} inputMode="numeric" placeholder={t('catalog.unlimited')} />
          <TextField name="maxProductiveUnits" label={t('catalog.maxProductiveUnits')} inputMode="numeric" placeholder={t('catalog.unlimited')} />
          <label className="flex items-center gap-2 pt-7">
            <input type="checkbox" className="checkbox checkbox-sm" {...form.register('active')} />
            {t('catalog.sellable')}
          </label>
        </div>
        <div>
          <div className="eyebrow pb-1.5 text-base-content/55">{t('catalog.includedModules')}</div>
          <ul className="grid gap-2 md:grid-cols-3">
            {modules.map((module) => (
              <li key={module.id}>
                <label className="flex items-center gap-2.5 rounded-box border border-base-300 px-3 py-2">
                  <input
                    type="checkbox"
                    className="checkbox checkbox-sm"
                    checked={module.isCore || chosen.includes(module.id)}
                    disabled={module.isCore}
                    onChange={() => toggle(module.id)}
                  />
                  <ModuleIcon icon={module.icon} />
                  <span className="flex-1">{module.name}</span>
                </label>
              </li>
            ))}
          </ul>
          <FieldError name="moduleIds" />
        </div>
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
