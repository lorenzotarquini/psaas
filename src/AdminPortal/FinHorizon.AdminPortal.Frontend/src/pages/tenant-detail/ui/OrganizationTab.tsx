import { zodResolver } from '@hookform/resolvers/zod'
import { useMemo } from 'react'
import { FormProvider, useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useUpdateTenantOrganization, type TenantDetail } from '@/entities/tenant'
import {
  countChanges,
  OrganizationMatrix,
  organizationFromTenant,
  organizationSchema,
  type OrganizationValues,
  removedSavedProfiles,
  toOrganizationRequest,
} from '@/features/organization-editor'
import { submitWithServerErrors } from '@/shared/lib/form'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { UnsavedChangesBar } from '@/shared/ui/unsaved-bar'

const formId = 'tenant-organization'

// UC5: the request replaces the whole organization; whatever is removed is struck through until saved.
export function OrganizationTab({ tenant }: { tenant: TenantDetail }) {
  const { t } = useTranslation()
  const update = useUpdateTenantOrganization(tenant.id)
  const original = useMemo(() => organizationFromTenant(tenant), [tenant])
  const form = useForm({ resolver: zodResolver(organizationSchema), defaultValues: original })
  const values = useWatch({ control: form.control }) as OrganizationValues
  const userCounts = Object.fromEntries(tenant.profiles.map((p) => [p.id, p.userCount]))

  // UC5-R4: the bar says who loses what before saving (mock 1d).
  const removed = removedSavedProfiles(values)
  const affectedUsers = removed.reduce((sum, p) => sum + (userCounts[p.id!] ?? 0), 0)
  const warning =
    removed.length > 0
      ? t('organizationEditor.removalWarning', {
          count: affectedUsers,
          names: removed.map((p) => `"${p.name}"`).join(', '),
        })
      : undefined

  const onSubmit = form.handleSubmit(async (formValues) => {
    const saved = await submitWithServerErrors(() => update.mutateAsync(toOrganizationRequest(formValues)), form.setError)
    if (saved) form.reset(organizationFromTenant(saved))
  })

  return (
    <FormProvider {...form}>
      <form id={formId} onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        {update.error && <ErrorAlert error={update.error} action="save" />}
        <OrganizationMatrix original={original} userCounts={userCounts} />
        <UnsavedChangesBar
          formId={formId}
          count={countChanges(original, values)}
          warning={warning}
          saveLabel={t('organizationEditor.save')}
          saving={form.formState.isSubmitting}
          onDiscard={() => form.reset(original)}
        />
      </form>
    </FormProvider>
  )
}
