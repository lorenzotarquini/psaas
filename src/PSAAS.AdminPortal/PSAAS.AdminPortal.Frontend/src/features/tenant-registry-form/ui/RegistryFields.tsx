import { useTranslation } from 'react-i18next'
import { TextField } from '@/shared/ui/form-field'
import { useUniqueFieldCheck } from '../model/useUniqueFieldCheck'
import { AddressFields } from './AddressFields'

// Registry fields of UC3 and UC4; must be inside a FormProvider whose values include RegistryValues.
// excludeTenantId: the tenant being edited, so its own values are not reported as duplicates.
export function RegistryFields({ excludeTenantId }: { excludeTenantId?: string }) {
  const { t } = useTranslation()
  const checkOnBlur = useUniqueFieldCheck(excludeTenantId)

  return (
    <div className="grid gap-x-4 sm:grid-cols-2">
      <TextField
        name="companyName"
        label={t('tenants.companyName')}
        maxLength={20}
        className="sm:col-span-2"
        registerOptions={checkOnBlur('companyName')}
      />
      <TextField
        name="completeVatNumber"
        label={t('tenants.completeVatNumber')}
        placeholder="IT12345678901"
        autoComplete="off"
        registerOptions={checkOnBlur('completeVatNumber')}
      />
      <TextField
        name="certifiedEmail"
        label={t('tenants.certifiedEmail')}
        type="email"
        autoComplete="off"
        registerOptions={checkOnBlur('certifiedEmail')}
      />
      <AddressFields />
    </div>
  )
}
