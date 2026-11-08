import { zodResolver } from '@hookform/resolvers/zod'
import { FormProvider, useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useUpdateTenantRegistry, type TenantDetail } from '@/entities/tenant'
import { RegistryFields, registryFromTenant, registrySchema, toRegistryRequest } from '@/features/tenant-registry-form'
import { submitWithServerErrors } from '@/shared/lib/form'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { UnsavedChangesBar } from '@/shared/ui/unsaved-bar'

const formId = 'tenant-registry'

// UC4: every registry field is editable, VAT number included; uniqueness excludes the tenant itself.
export function RegistryTab({ tenant }: { tenant: TenantDetail }) {
  const { t } = useTranslation()
  const update = useUpdateTenantRegistry(tenant.id)
  const form = useForm({ resolver: zodResolver(registrySchema), defaultValues: registryFromTenant(tenant) })
  const dirty = Object.keys(form.formState.dirtyFields).length

  const onSubmit = form.handleSubmit(async (values) => {
    const saved = await submitWithServerErrors(() => update.mutateAsync(toRegistryRequest(values)), form.setError)
    if (saved) form.reset(registryFromTenant(saved))
  })

  return (
    <FormProvider {...form}>
      <form id={formId} onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        {update.error && <ErrorAlert error={update.error} action="save" />}
        <section className="panel px-5 py-4">
          <h2 className="panel-title">{t('tenantDetail.registry')}</h2>
          <p className="mb-2 text-base-content/60">{t('tenantForm.registryHint')}</p>
          <RegistryFields excludeTenantId={tenant.id} />
        </section>
        <UnsavedChangesBar
          formId={formId}
          count={dirty}
          saveLabel={t('tenantForm.save')}
          saving={form.formState.isSubmitting}
          onDiscard={() => form.reset()}
        />
      </form>
    </FormProvider>
  )
}
