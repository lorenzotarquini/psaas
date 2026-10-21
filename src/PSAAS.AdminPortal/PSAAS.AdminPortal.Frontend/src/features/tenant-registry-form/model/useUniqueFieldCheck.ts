import { useFormContext, type RegisterOptions } from 'react-hook-form'
import { validateTenantField, type UniqueTenantField } from '@/entities/tenant'
import { getLogger } from '@/shared/lib/logger'
import type { RegistryValues } from './registry'

const logger = getLogger('tenant-registry-form')

/**
 * UC11 on blur: client format rules first, then the backend (uniqueness, same codes as saving).
 * excludeTenantId: the tenant being edited (UC4), whose own values are not duplicates.
 */
export function useUniqueFieldCheck(excludeTenantId?: string) {
  const { getValues, trigger, setError } = useFormContext<RegistryValues>()

  return (field: UniqueTenantField): RegisterOptions => ({
    onBlur: async () => {
      const value = getValues(field).trim()
      // Empty: nothing to check yet. Format errors: already shown by the client, no request.
      if (!value || !(await trigger(field))) return
      try {
        const errorCode = await validateTenantField(field, value, excludeTenantId)
        // The field may have changed while the answer was coming.
        if (errorCode && getValues(field).trim() === value) setError(field, { type: 'server', message: errorCode })
      } catch (error) {
        // Not blocking: saving checks everything again.
        logger.warn({ err: error, field }, 'Field validation unavailable')
      }
    },
  })
}
