import { zodResolver } from '@hookform/resolvers/zod'
import { FormProvider, useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router'
import { useTenant, useUpdateTenantOrganization, type TenantDetail } from '@/entities/tenant'
import {
  OrganizationEditor,
  organizationFromTenant,
  organizationSchema,
  removedSavedProfiles,
  toOrganizationRequest,
} from '@/features/organization-editor'
import { submitWithServerErrors } from '@/shared/lib/form'
import { tenantPath } from '@/shared/routes'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { PageHeader } from '@/shared/ui/page-header'

// UC5: the request replaces the whole organization; whatever is missing is removed.
export function TenantOrganizationPage() {
  const { t } = useTranslation()
  const { tenantId = '' } = useParams()
  const { data: tenant, error, isPending } = useTenant(tenantId)

  return (
    <div className="flex flex-col gap-4">
      {error ? (
        <ErrorAlert error={error} />
      ) : isPending ? (
        <span className="loading loading-spinner" aria-label={t('loading')} />
      ) : (
        <OrganizationForm tenant={tenant} />
      )}
    </div>
  )
}

function OrganizationForm({ tenant }: { tenant: TenantDetail }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const update = useUpdateTenantOrganization(tenant.id)
  const form = useForm({ resolver: zodResolver(organizationSchema), defaultValues: organizationFromTenant(tenant) })

  const onSubmit = form.handleSubmit(async (values) => {
    // R4: users lose a removed profile with no replacement, so ask first.
    const removed = removedSavedProfiles(tenant, values)
    if (removed.length > 0 && !window.confirm(t('organizationEditor.confirmRemoveProfiles', { names: removed.join(', ') }))) {
      return
    }
    const saved = await submitWithServerErrors(() => update.mutateAsync(toOrganizationRequest(values)), form.setError)
    if (saved) navigate(tenantPath(saved.id))
  })

  return (
    <FormProvider {...form}>
      <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        <PageHeader
          title={t('organizationEditor.title', { name: tenant.companyName })}
          back={{ to: tenantPath(tenant.id), label: t('tenantForm.backToDetail') }}
        />
        {update.error && <ErrorAlert error={update.error} />}
        <section className="card bg-base-100 shadow-sm">
          <div className="card-body">
            <OrganizationEditor />
          </div>
        </section>
        <div className="flex justify-end">
          <button type="submit" className="btn btn-primary" disabled={form.formState.isSubmitting}>
            {form.formState.isSubmitting && <span className="loading loading-spinner loading-sm" />}
            {t('tenantForm.save')}
          </button>
        </div>
      </form>
    </FormProvider>
  )
}
