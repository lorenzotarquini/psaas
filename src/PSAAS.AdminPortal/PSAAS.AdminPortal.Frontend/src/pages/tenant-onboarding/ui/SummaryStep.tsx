import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useChangeTenantStatus, type TenantDetail } from '@/entities/tenant'
import { tenantPath } from '@/shared/routes'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { StepLayout } from './SimpleSteps'
import { StepFooter } from './StepFooter'

interface SummaryStepProps {
  draft: TenantDetail
  onActivated: (tenant: TenantDetail) => void
  onCancel: () => void
  stepLink: (step: 'registry' | 'organization' | 'users') => string
}

// UC14 step 5: check and activate (Onboarding → Active).
export function SummaryStep({ draft, onActivated, onCancel, stepLink }: SummaryStepProps) {
  const { t } = useTranslation()
  const activate = useChangeTenantStatus()
  const users = draft.profiles.reduce((sum, profile) => sum + (profile.userCount ?? 0), 0)
  const { street, postalCode, city } = draft.address

  const onSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    const active = await activate.mutateAsync({ id: draft.id, status: 'Active' }).catch(() => undefined)
    if (active) onActivated(active)
  }

  const row = (label: string, value: string, step: 'registry' | 'organization' | 'users') => (
    <div className="flex items-start justify-between gap-4 px-4 py-3">
      <div>
        <div className="text-[12px] text-base-content/60">{label}</div>
        <div className="font-medium">{value}</div>
      </div>
      <Link to={stepLink(step)} className="text-[12px] text-primary hover:underline">
        {t('onboarding.edit')}
      </Link>
    </div>
  )

  return (
    <StepLayout title={t('onboarding.steps.summary.title')} hint={t('onboarding.summaryHint')}>
      <form id="onboarding-summary" onSubmit={onSubmit} className="contents">
        {activate.error && <ErrorAlert error={activate.error} action="save" />}
        <section className="panel divide-y divide-base-300">
          {row(t('tenantDetail.registry'), `${draft.companyName} · ${draft.completeVatNumber} · ${street}, ${postalCode} ${city}`, 'registry')}
          {row(
            t('tenantDetail.organization'),
            t('onboarding.organizationSummary', { profiles: draft.profiles.length, units: draft.productiveUnits.length }),
            'organization',
          )}
          {row(t('onboarding.steps.users.title'), t('onboarding.usersSummary', { count: users }), 'users')}
        </section>
        <p className="text-base-content/60">
          {t('onboarding.activationNote')}{' '}
          <Link to={tenantPath(draft.id)} className="link">
            {t('onboarding.openDraft')}
          </Link>
        </p>
        <StepFooter
          formId="onboarding-summary"
          onCancel={onCancel}
          cancelLabel={t('onboarding.cancel')}
          busy={activate.isPending}
          primaryLabel={t('onboarding.activate')}
        />
      </form>
    </StepLayout>
  )
}
