import { useFormContext, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import type { UniqueTenantField } from '@/entities/tenant'
import { tenantPath } from '@/shared/routes'
import { TextField } from '@/shared/ui/form-field'
import type { RegistryValues } from '../model/registry'
import { useUniqueFieldCheck } from '../model/useUniqueFieldCheck'
import { AddressFields } from './AddressFields'

export type UniqueFieldCheck = ReturnType<typeof useUniqueFieldCheck>

interface RegistryFieldsProps {
  /** The tenant being edited (UC4), whose own values are not duplicates. */
  excludeTenantId?: string
  /** A check owned by the page, when something else shows its results (the wizard "Controlli" panel). */
  check?: UniqueFieldCheck
}

// Registry fields of UC3, UC4 and UC14; must be inside a FormProvider whose values include RegistryValues.
export function RegistryFields({ excludeTenantId, check }: RegistryFieldsProps) {
  const { t } = useTranslation()
  const { control } = useFormContext<RegistryValues>()
  const values = useWatch({ control })
  const ownCheck = useUniqueFieldCheck(excludeTenantId)
  const { onBlur, statusOf } = check ?? ownCheck

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
      <TextField
        name="sdiCode"
        label={t('tenantForm.sdiCode')}
        placeholder="M5UXCR1"
        maxLength={7}
        autoComplete="off"
        className="[&_input]:font-mono [&_input]:uppercase"
        hint={t('tenantForm.optional')}
      />
      <TextField name="accountManager" label={t('tenantForm.accountManager')} autoComplete="off" />
    </div>
  )
}
