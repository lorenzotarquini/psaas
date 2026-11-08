import { useFormContext, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import type { TenantKind, UniqueTenantField } from '@/entities/tenant'
import { tenantPath } from '@/shared/routes'
import { TextField } from '@/shared/ui/form-field'
import { nameMaxLength, type RegistryValues } from '../model/registry'
import { useUniqueFieldCheck } from '../model/useUniqueFieldCheck'
import { AddressFields } from './AddressFields'

export type UniqueFieldCheck = ReturnType<typeof useUniqueFieldCheck>

interface RegistryFieldsProps {
  /** The tenant being edited (UC4), whose own values are not duplicates; its kind can no longer change. */
  excludeTenantId?: string
  /** A check owned by the page, when something else shows its results (the wizard "Controlli" panel). */
  check?: UniqueFieldCheck
}

const kinds: TenantKind[] = ['Business', 'Individual']

// Registry fields of UC3, UC4 and UC14; must be inside a FormProvider whose values include RegistryValues.
export function RegistryFields({ excludeTenantId, check }: RegistryFieldsProps) {
  const { t } = useTranslation()
  const { control, register, setValue } = useFormContext<RegistryValues>()
  const values = useWatch({ control })
  const ownCheck = useUniqueFieldCheck(excludeTenantId)
  const { onBlur, statusOf } = check ?? ownCheck
  const kind = values.kind ?? 'Business'
  const individual = kind === 'Individual'

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
      {!excludeTenantId && (
        // UC3-R9: chosen once, at creation.
        <fieldset className="mb-2 flex flex-wrap gap-4 sm:col-span-2">
          <legend className="mb-1 text-[12px] font-medium">{t('tenants.kind')}</legend>
          {kinds.map((option) => (
            <label key={option} className="flex items-center gap-2">
              <input
                type="radio"
                value={option}
                className="radio radio-sm"
                {...register('kind', {
                  // A private person has no SDI code: clear it rather than leave a hidden error.
                  onChange: () => option === 'Individual' && setValue('sdiCode', ''),
                })}
              />
              {t(`tenants.kinds.${option}`)}
            </label>
          ))}
        </fieldset>
      )}
      <TextField
        name="companyName"
        label={t(individual ? 'tenants.fullName' : 'tenants.companyName')}
        maxLength={nameMaxLength(kind)}
        counter
        hintScope={individual ? 'fullName' : undefined}
        className="sm:col-span-2"
        {...uniqueProps('companyName')}
      />
      <TextField
        name="completeVatNumber"
        label={t(individual ? 'tenants.taxCode' : 'tenants.completeVatNumber')}
        placeholder={individual ? 'RSSMRA85T10A562S' : 'IT12345678901'}
        hintScope={individual ? 'taxCode' : undefined}
        autoComplete="off"
        className="[&_input]:font-mono [&_input]:uppercase"
        {...uniqueProps('completeVatNumber')}
      />
      <TextField
        name="certifiedEmail"
        label={t(individual ? 'tenants.email' : 'tenants.certifiedEmail')}
        type="email"
        autoComplete="off"
        {...uniqueProps('certifiedEmail')}
      />
      <AddressFields />
      {!individual && (
        <TextField
          name="sdiCode"
          label={t('tenantForm.sdiCode')}
          placeholder="M5UXCR1"
          maxLength={7}
          autoComplete="off"
          className="[&_input]:font-mono [&_input]:uppercase"
          hint={t('tenantForm.optional')}
        />
      )}
      <TextField name="accountManager" label={t('tenantForm.accountManager')} autoComplete="off" />
    </div>
  )
}
