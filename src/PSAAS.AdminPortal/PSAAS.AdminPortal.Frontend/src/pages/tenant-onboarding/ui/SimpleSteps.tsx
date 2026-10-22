import { zodResolver } from '@hookform/resolvers/zod'
import { useMemo, type ReactNode } from 'react'
import { FormProvider, useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useTenantSubscription } from '@/entities/catalog'
import { useUpdateTenantOrganization, type TenantDetail } from '@/entities/tenant'
import {
  countChanges,
  OrganizationMatrix,
  organizationFromTenant,
  organizationSchema,
  toOrganizationRequest,
} from '@/features/organization-editor'
import { SubscriptionEditor } from '@/features/subscription-editor'
import { submitWithServerErrors } from '@/shared/lib/form'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { StepFooter } from './StepFooter'

interface StepProps {
  draft: TenantDetail
  onNext: (tenant: TenantDetail) => void
  onCancel: () => void
  nextLabel: string
}

export function StepLayout({ title, hint, children }: { title: string; hint: string; children: ReactNode }) {
  return (
    <div className="flex min-h-full flex-col gap-4 px-1 py-2 lg:px-8">
      <header>
        <h1 className="text-[22px] font-semibold">{title}</h1>
        <p className="text-base-content/60">{hint}</p>
      </header>
      {children}
    </div>
  )
}

// UC14 step 2 = UC17 on the draft: plan, trial and add-ons; no plan chosen moves on and leaves the check grey.
export function PlanStep({ draft, onNext, onCancel, nextLabel }: StepProps) {
  const { t } = useTranslation()
  const { data: subscription, isPending } = useTenantSubscription(draft.id)
  return (
    <StepLayout title={t('onboarding.steps.plan.title')} hint={t('onboarding.steps.plan.hint')}>
      {isPending ? (
        <span className="loading loading-spinner" aria-label={t('loading')} />
      ) : (
        <SubscriptionEditor tenantId={draft.id} current={subscription ?? null} formId="onboarding-plan" onSaved={() => onNext(draft)} />
      )}
      <StepFooter formId="onboarding-plan" onCancel={onCancel} cancelLabel={t('onboarding.cancel')} busy={false} primaryLabel={nextLabel} />
    </StepLayout>
  )
}

// UC14 step 3 = UC5 on the draft.
export function OrganizationStep({ draft, onNext, onCancel, nextLabel }: StepProps) {
  const { t } = useTranslation()
  const original = useMemo(() => organizationFromTenant(draft), [draft])
  const form = useForm({ resolver: zodResolver(organizationSchema), defaultValues: original })
  const update = useUpdateTenantOrganization(draft.id)

  const onSubmit = form.handleSubmit(async (values) => {
    if (countChanges(original, values) === 0) return onNext(draft)
    const saved = await submitWithServerErrors(() => update.mutateAsync(toOrganizationRequest(values)), form.setError)
    if (saved) onNext(saved)
  })

  return (
    <StepLayout title={t('onboarding.steps.organization.title')} hint={t('onboarding.steps.organization.hint')}>
      <FormProvider {...form}>
        <form id="onboarding-organization" onSubmit={onSubmit} noValidate className="contents">
          {update.error && <ErrorAlert error={update.error} action="save" />}
          <OrganizationMatrix original={original} />
          <StepFooter
            formId="onboarding-organization"
            onCancel={onCancel}
            cancelLabel={t('onboarding.cancel')}
            busy={form.formState.isSubmitting}
            primaryLabel={nextLabel}
          />
        </form>
      </FormProvider>
    </StepLayout>
  )
}
