import { useState } from 'react'
import { useFormContext, type RegisterOptions } from 'react-hook-form'
import { validateTenantField, type TenantReference, type UniqueTenantField } from '@/entities/tenant'
import { getLogger } from '@/shared/lib/logger'
import type { RegistryValues } from './registry'

const logger = getLogger('tenant-registry-form')

interface CheckedValue {
  value: string
  conflictingTenant: TenantReference | null
}

/**
 * UC11 on blur: client format rules first, then the backend (uniqueness, same codes as saving).
 * excludeTenantId: the tenant being edited (UC4), whose own values are not duplicates.
 * statusOf tells whether the current value was confirmed free, or which tenant already uses it (UC11-R4).
 */
export function useUniqueFieldCheck(excludeTenantId?: string) {
  const { getValues, trigger, setError } = useFormContext<RegistryValues>()
  const [checked, setChecked] = useState<Partial<Record<UniqueTenantField, CheckedValue>>>({})

  const onBlur = (field: UniqueTenantField): RegisterOptions => ({
    onBlur: async () => {
      const value = getValues(field).trim()
      // Empty: nothing to check yet. Format errors: already shown by the client, no request.
      if (!value || !(await trigger(field))) return
      try {
        const { errorCode, conflictingTenant } = await validateTenantField(field, value, excludeTenantId)
        // The field may have changed while the answer was coming.
        if (getValues(field).trim() !== value) return
        if (errorCode) setError(field, { type: 'server', message: errorCode })
        setChecked((current) => ({ ...current, [field]: { value, conflictingTenant } }))
      } catch (error) {
        // Not blocking: saving checks everything again.
        logger.warn({ err: error, field }, 'Field validation unavailable')
      }
    },
  })

  const statusOf = (field: UniqueTenantField, currentValue: string) => {
    const result = checked[field]
    const current = result && result.value === currentValue.trim()
    return { verified: Boolean(current && !result.conflictingTenant), conflictingTenant: current ? result.conflictingTenant : null }
  }

  return { onBlur, statusOf }
}
