import { zodResolver } from '@hookform/resolvers/zod'
import { FormProvider, useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router'
import { useTenant, useUpdateTenantRegistry, type TenantDetail } from '@/entities/tenant'
import { RegistryFields, registryFromTenant, registrySchema, toRegistryRequest } from '@/features/tenant-registry-form'
import { submitWithServerErrors } from '@/shared/lib/form'
import { routes, tenantPath } from '@/shared/routes'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { PageHeader } from '@/shared/ui/page-header'

// UC4: every registry field is editable, VAT number included; uniqueness excludes the tenant itself.
export function TenantEditPage() {
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
        <RegistryForm tenant={tenant} />
      )}
    </div>
  )
}

function RegistryForm({ tenant }: { tenant: TenantDetail }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const update = useUpdateTenantRegistry(tenant.id)
  const form = useForm({ resolver: zodResolver(registrySchema), defaultValues: registryFromTenant(tenant) })

  const onSubmit = form.handleSubmit(async (values) => {
    const saved = await submitWithServerErrors(() => update.mutateAsync(toRegistryRequest(values)), form.setError)
    if (saved) navigate(tenantPath(saved.id))
  })

  return (
    <FormProvider {...form}>
      <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        <PageHeader
          title={t('tenantForm.editTitle', { name: tenant.companyName })}
          parents={[{ to: routes.tenants, label: t('nav.tenants') }, { to: tenantPath(tenant.id), label: tenant.companyName }]}
        />
        {update.error && <ErrorAlert error={update.error} action="save" />}
        <section className="card bg-base-100 shadow-sm">
          <div className="card-body">
            <RegistryFields excludeTenantId={tenant.id} />
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
