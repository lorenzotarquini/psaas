import { useFormContext, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import type { UniqueTenantField } from '@/entities/tenant'
import { tenantPath } from '@/shared/routes'
import { TextField } from '@/shared/ui/form-field'
import type { RegistryValues } from '../model/registry'
import { useUniqueFieldCheck } from '../model/useUniqueFieldCheck'
import { AddressFields } from './AddressFields'

// Registry fields of UC3 and UC4; must be inside a FormProvider whose values include RegistryValues.
// excludeTenantId: the tenant being edited, so its own values are not reported as duplicates.
export function RegistryFields({ excludeTenantId }: { excludeTenantId?: string }) {
  const { t } = useTranslation()
  const { control } = useFormContext<RegistryValues>()
  const values = useWatch({ control })
  const { onBlur, statusOf } = useUniqueFieldCheck(excludeTenantId)

  // Mock 1a/1e: green "Disponibile" once the backend confirmed the value; on a duplicate, a link to the tenant using it.
  const uniqueProps = (field: UniqueTenantField) => {
    const { verified, conflictingTenant } = statusOf(field, values[field] ?? '')
    return {
      registerOptions: onBlur(field),
      valid: verified,
      hint: verified ? (
        t('tenantForm.available')
      ) : conflictingTenant ? (
        <Link to={tenantPath(conflictingTenant.id)} className="link link-primary">
          {t('tenantForm.openConflicting', { name: conflictingTenant.companyName })} →
        </Link>
      ) : undefined,
    }
  }

  return (
    <div className="grid gap-x-4 sm:grid-cols-2">
      <TextField
        name="companyName"
        label={t('tenants.companyName')}
        maxLength={20}
        counter
        className="sm:col-span-2"
        {...uniqueProps('companyName')}
      />
      <TextField
        name="completeVatNumber"
        label={t('tenants.completeVatNumber')}
        placeholder="IT12345678901"
        autoComplete="off"
        className="[&_input]:font-mono"
        {...uniqueProps('completeVatNumber')}
      />
      <TextField
        name="certifiedEmail"
        label={t('tenants.certifiedEmail')}
        type="email"
        autoComplete="off"
        {...uniqueProps('certifiedEmail')}
      />
      <AddressFields />
    </div>
  )
}
