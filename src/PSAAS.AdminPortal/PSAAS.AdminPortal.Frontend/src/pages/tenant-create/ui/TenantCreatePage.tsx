import { zodResolver } from '@hookform/resolvers/zod'
import { FormProvider, useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { useCreateTenant } from '@/entities/tenant'
import {
  emptyOrganization,
  OrganizationMatrix,
  organizationSchema,
  toOrganizationRequest,
} from '@/features/organization-editor'
import { emptyRegistry, RegistryFields, registrySchema, toRegistryRequest } from '@/features/tenant-registry-form'
import { submitWithServerErrors } from '@/shared/lib/form'
import { routes, tenantPath } from '@/shared/routes'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { PageHeader } from '@/shared/ui/page-header'

const schema = registrySchema.extend(organizationSchema.shape)

// UC3: registry plus optional organization, saved all together; then the new tenant's detail (UC2).
export function TenantCreatePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const createTenant = useCreateTenant()
  const form = useForm({ resolver: zodResolver(schema), defaultValues: { ...emptyRegistry, ...emptyOrganization } })

  const onSubmit = form.handleSubmit(async (values) => {
    const { profiles, productiveUnits } = toOrganizationRequest(values)
    const tenant = await submitWithServerErrors(
      () =>
        createTenant.mutateAsync({
          ...toRegistryRequest(values),
          // Everything is new on creation: no ids.
          profiles: profiles.map(({ key, name, description }) => ({ key, name, description })),
          productiveUnits: productiveUnits.map(({ name, description, profileKeys }) => ({ name, description, profileKeys })),
        }),
      form.setError,
    )
    if (tenant) navigate(tenantPath(tenant.id))
  })

  return (
    <FormProvider {...form}>
      <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        <PageHeader title={t('tenantForm.createTitle')} parents={[{ to: routes.tenants, label: t('nav.tenants') }]} />
        {createTenant.error && <ErrorAlert error={createTenant.error} action="save" />}
        <section className="card bg-base-100 shadow-sm">
          <div className="card-body">
            <h2 className="card-title text-base">{t('tenantDetail.registry')}</h2>
            <RegistryFields />
          </div>
        </section>
        <section className="card bg-base-100 shadow-sm">
          <div className="card-body">
            <h2 className="card-title text-base">
              {t('tenantDetail.organization')}
              <span className="badge badge-ghost">{t('tenantForm.optional')}</span>
            </h2>
            <OrganizationMatrix />
          </div>
        </section>
        <div className="flex justify-end">
          <button type="submit" className="btn btn-primary" disabled={form.formState.isSubmitting}>
            {form.formState.isSubmitting && <span className="loading loading-spinner loading-sm" />}
            {t('tenantForm.create')}
          </button>
        </div>
      </form>
    </FormProvider>
  )
}
