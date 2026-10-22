import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate, useNavigate, useParams, useSearchParams } from 'react-router'
import { useDiscardDraft, useTenant, type TenantDetail } from '@/entities/tenant'
import { useBreadcrumb } from '@/shared/lib/breadcrumb'
import { onboardingPath, routes, tenantPath } from '@/shared/routes'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { isDraft, nextStep, readStep, type Step } from '../model/onboarding'
import { RegistryStep } from './RegistryStep'
import { OrganizationStep, PlanStep } from './SimpleSteps'
import { Stepper } from './Stepper'
import { SummaryStep } from './SummaryStep'
import { UsersStep } from './UsersStep'

// UC14 (mock 1e): /tenants/new starts a new tenant; /tenants/:id/onboarding?step= resumes a draft.
export function OnboardingPage() {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const { tenantId } = useParams()
  const [params] = useSearchParams()
  const { data: draft, error, isPending } = useTenant(tenantId ?? '', Boolean(tenantId))
  const discard = useDiscardDraft()
  const [savedAt, setSavedAt] = useState<Date>()
  const [confirmCancel, setConfirmCancel] = useState(false)
  const step = readStep(params.get('step'), Boolean(tenantId))

  useBreadcrumb(
    [{ label: t('nav.tenants'), to: routes.tenants }, { label: draft?.companyName ?? t('onboarding.title') }],
    savedAt ? t('onboarding.savedAt', { time: savedAt.toLocaleTimeString(i18n.language, { hour: '2-digit', minute: '2-digit' }) }) : undefined,
  )

  if (tenantId && error) return <ErrorAlert error={error} />
  if (tenantId && isPending) return <span className="loading loading-spinner" aria-label={t('loading')} />
  // An activated tenant is not a draft any more: its page is the tenant page.
  if (draft && !isDraft(draft)) return <Navigate to={tenantPath(draft.id)} replace />

  const saved = (tenant: TenantDetail) => {
    setSavedAt(new Date())
    if (!tenantId) navigate(onboardingPath(tenant.id, 'registry'), { replace: true })
  }
  const goTo = (tenant: TenantDetail, next: Step | undefined) => {
    setSavedAt(new Date())
    navigate(next ? onboardingPath(tenant.id, next) : tenantPath(tenant.id))
  }
  const activated = (tenant: TenantDetail) => navigate(tenantPath(tenant.id))
  const cancel = () => (draft ? setConfirmCancel(true) : navigate(routes.tenants))
  const discardDraft = async () => {
    await discard.mutateAsync(draft!.id)
    navigate(routes.tenants)
  }
  const nextLabel = (current: Step) => {
    const next = nextStep(current)
    return next ? t('onboarding.nextTo', { step: t(`onboarding.steps.${next}.title`) }) : t('onboarding.activate')
  }
  const common = { onCancel: cancel, nextLabel: nextLabel(step) }

  return (
    <div className="-mx-4 -my-6 grid min-h-[calc(100vh-52px)] md:-mx-7 lg:grid-cols-[230px_minmax(0,1fr)]">
      <div className="border-base-300 px-4 py-6 lg:border-r">
        <Stepper current={step} linkTo={draft ? (s) => onboardingPath(draft.id, s) : undefined} />
      </div>
      <div className="px-4 py-6">
        {step === 'registry' || !draft ? (
          <RegistryStep
            key={draft?.id ?? 'new'}
            draft={draft}
            onSaved={saved}
            onNext={(tenant) => goTo(tenant, 'plan')}
            onActivated={activated}
            onCancel={cancel}
          />
        ) : step === 'plan' ? (
          <PlanStep draft={draft} {...common} onNext={(tenant) => goTo(tenant, 'organization')} />
        ) : step === 'organization' ? (
          <OrganizationStep draft={draft} {...common} onNext={(tenant) => goTo(tenant, 'users')} />
        ) : step === 'users' ? (
          <UsersStep draft={draft} {...common} onNext={(tenant) => goTo(tenant, 'summary')} />
        ) : (
          <SummaryStep draft={draft} onActivated={activated} onCancel={cancel} stepLink={(s) => onboardingPath(draft.id, s)} />
        )}
      </div>

      <dialog className={`modal ${confirmCancel ? 'modal-open' : ''}`} aria-labelledby="discard-title">
        <div className="modal-box">
          <h2 id="discard-title" className="text-[15px] font-semibold">
            {t('onboarding.discardTitle')}
          </h2>
          <p className="py-3 text-base-content/70">{t('onboarding.discardText', { name: draft?.companyName })}</p>
          {discard.error && <ErrorAlert error={discard.error} action="save" />}
          <div className="modal-action">
            <button type="button" className="btn btn-sm" onClick={() => setConfirmCancel(false)}>
              {t('onboarding.keepDraft')}
            </button>
            <button type="button" className="btn btn-error btn-sm" disabled={discard.isPending} onClick={() => void discardDraft()}>
              {t('onboarding.discard')}
            </button>
          </div>
        </div>
      </dialog>
    </div>
  )
}
