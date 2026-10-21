import { useEffect, useId } from 'react'
import { useFormContext } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { minCitySearchLength, useCities } from '@/entities/city'
import { useDebouncedValue } from '@/shared/lib/debounce'
import { TextField } from '@/shared/ui/form-field'
import type { RegistryValues } from '../model/registry'

// UC10 as native datalists: suggestions while typing, free text always allowed (UC3 3c: city list down).
export function AddressFields() {
  const { t } = useTranslation()
  const cityListId = useId()
  const postalCodeListId = useId()
  const { watch, getValues, setValue } = useFormContext<RegistryValues>()

  const city = watch('city').trim()
  const term = useDebouncedValue(city)
  const { data: cities = [], isError } = useCities(term)

  // Homonymous cities: the postal code decides (UC3 R5), so offer the codes of all of them.
  const matching = cities.filter((candidate) => candidate.name.toLowerCase() === city.toLowerCase())
  const postalCodes = [...new Set(matching.flatMap((candidate) => candidate.postalCodes))]

  const singlePostalCode = postalCodes.length === 1 ? postalCodes[0] : undefined
  useEffect(() => {
    if (singlePostalCode && !getValues('postalCode')) setValue('postalCode', singlePostalCode)
  }, [singlePostalCode, getValues, setValue])

  return (
    <>
      <TextField name="street" label={t('tenantForm.street')} autoComplete="street-address" className="sm:col-span-2" />
      <div>
        <TextField name="city" label={t('tenants.city')} list={cityListId} autoComplete="off" />
        <datalist id={cityListId}>
          {cities.map((candidate) => (
            <option key={`${candidate.name}-${candidate.provinceCode}`} value={candidate.name}>
              {`${candidate.name} (${candidate.provinceCode})`}
            </option>
          ))}
        </datalist>
        {term.length >= minCitySearchLength && isError && (
          <p className="text-warning text-sm">{t('tenantForm.cityListUnavailable')}</p>
        )}
      </div>
      <div>
        <TextField name="postalCode" label={t('tenantForm.postalCode')} list={postalCodeListId} inputMode="numeric" autoComplete="off" />
        <datalist id={postalCodeListId}>
          {postalCodes.map((code) => (
            <option key={code} value={code} />
          ))}
        </datalist>
      </div>
    </>
  )
}
