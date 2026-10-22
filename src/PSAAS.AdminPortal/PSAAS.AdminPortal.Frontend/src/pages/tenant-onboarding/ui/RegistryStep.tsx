import { zodResolver } from '@hookform/resolvers/zod'
import { FormProvider, useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import {
  useChangeTenantStatus,
  useCreateTenant,
  useUpdateTenantRegistry,
  type TenantDetail,
} from '@/entities/tenant'
import {
  emptyRegistry,
  RegistryFields,
  registryFromTenant,
  registrySchema,
  toRegistryRequest,
  useUniqueFieldCheck,
} from '@/features/tenant-registry-form'
import { keycloak } from '@/shared/auth'
import { submitWithServerErrors } from '@/shared/lib/form'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { buildChecks } from '../model/onboarding'
import { ChecksPanel } from './ChecksPanel'
import { StepFooter } from './StepFooter'

const formId = 'onboarding-registry'

interface RegistryStepProps {
  draft?: TenantDetail
  onSaved: (tenant: TenantDetail) => void
  onNext: (tenant: TenantDetail) => void
  onActivated: (tenant: TenantDetail) => void
  onCancel: () => void
}

// UC14 step 1 (mock 1e): saving creates the draft (Onboarding), or updates it when it already exists.
export function RegistryStep({ draft, onSaved, onNext, onActivated, onCancel }: RegistryStepProps) {
  const { t } = useTranslation()
  const token = keycloak.tokenParsed as { given_name?: string; family_name?: string } | undefined
  const me = [token?.given_name, token?.family_name].filter(Boolean).join(' ')
  const form = useForm({
    resolver: zodResolver(registrySchema),
    defaultValues: draft ? registryFromTenant(draft) : { ...emptyRegistry, accountManager: me },
  })
  const check = useUniqueFieldCheck(draft?.id, form)
  const create = useCreateTenant()
  const update = useUpdateTenantRegistry(draft?.id ?? '')
  const activate = useChangeTenantStatus()
  const values = useWatch({ control: form.control })
  const errors = form.formState.errors

  const checks = buildChecks({
    values: {
      companyName: values.companyName ?? '',
      completeVatNumber: values.completeVatNumber ?? '',
      certifiedEmail: values.certifiedEmail ?? '',
      city: values.city ?? '',
      postalCode: values.postalCode ?? '',
    },
    errors: Object.fromEntries(
      (['companyName', 'completeVatNumber', 'certifiedEmail', 'city', 'postalCode'] as const)
        .filter((field) => errors[field]?.message)
        .map((field) => [field, errors[field]!.message!]),
    ),
    unique: (field) => check.statusOf(field, values[field] ?? ''),
    planChosen: false,
  })

  // Creates or updates the draft; returns it, or undefined when the server rejected some field.
  const save = async () => {
    let saved: TenantDetail | undefined
    await form.handleSubmit(async (formValues) => {
      const request = toRegistryRequest(formValues)
      saved = draft
        ? form.formState.isDirty
          ? await submitWithServerErrors(() => update.mutateAsync(request), form.setError)
          : draft
        : await submitWithServerErrors(
            () => create.mutateAsync({ ...request, profiles: [], productiveUnits: [], draft: true }),
            form.setError,
          )
    })()
    if (saved) {
      form.reset(registryFromTenant(saved))
      onSaved(saved)
    }
    return saved
  }

  const onSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    const saved = await save()
    if (saved) onNext(saved)
  }

  // "Salta organizzazione e attiva": save, then activate straight away.
  const skipAndActivate = async () => {
    const saved = await save()
    if (!saved) return
    const active = await activate.mutateAsync({ id: saved.id, status: 'Active' }).catch(() => undefined)
    if (active) onActivated(active)
  }

  const busy = form.formState.isSubmitting || create.isPending || update.isPending || activate.isPending
  const error = create.error ?? update.error ?? activate.error

  return (
    <FormProvider {...form}>
      <div className="grid min-h-full gap-0 lg:grid-cols-[minmax(0,1fr)_280px]">
        <form id={formId} onSubmit={onSubmit} noValidate className="flex flex-col gap-4 px-1 py-2 lg:px-8">
          <header>
            <h1 className="text-[22px] font-semibold">{t('onboarding.steps.registry.title')}</h1>
            <p className="text-base-content/60">{t('tenantForm.registryHint')}</p>
          </header>
          {error && <ErrorAlert error={error} action="save" />}
          <section className="panel px-5 py-3">
            <RegistryFields excludeTenantId={draft?.id} check={check} />
          </section>
          <StepFooter
            formId={formId}
            onCancel={onCancel}
            cancelLabel={t('onboarding.cancel')}
            busy={busy}
            primaryLabel={t('onboarding.nextTo', { step: t('onboarding.steps.plan.title') })}
            secondary={
              <button type="button" className="btn btn-sm" disabled={busy} onClick={() => void skipAndActivate()}>
                {t('onboarding.skipAndActivate')}
              </button>
            }
          />
        </form>
        <div className="border-base-300 px-1 py-2 lg:border-l lg:px-5">
          <ChecksPanel checks={checks} />
        </div>
      </div>
    </FormProvider>
  )
}
