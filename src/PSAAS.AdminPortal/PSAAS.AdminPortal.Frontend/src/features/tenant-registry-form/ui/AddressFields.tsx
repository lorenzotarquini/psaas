import { faLocationDot } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useEffect, useId, useState } from 'react'
import { get, useFormContext, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { minCitySearchLength, useCities, type City } from '@/entities/city'
import { useDebouncedValue } from '@/shared/lib/debounce'
import { FieldError, TextField } from '@/shared/ui/form-field'
import { highlight, postalCodeRange } from '../model/postalCodes'
import type { RegistryValues } from '../model/registry'

// UC10 as the combobox of mock 1e: suggestions with province and postal codes, keyboard navigation,
// the postal code filled from the chosen city. Free text stays possible (UC3 3c: city list down).
export function AddressFields() {
  const { t } = useTranslation()
  const listId = useId()
  const { control, register, setValue, formState } = useFormContext<RegistryValues>()
  const city = useWatch({ control, name: 'city' }) ?? ''
  const postalCode = useWatch({ control, name: 'postalCode' }) ?? ''
  const term = useDebouncedValue(city.trim())
  const { data: cities = [], isError, isFetching } = useCities(term)
  const [open, setOpen] = useState(false)
  const [active, setActive] = useState(0)

  // Homonymous cities: the postal code decides (UC3-R5), so offer the codes of all of them.
  const chosen = cities.filter((candidate) => candidate.name.toLowerCase() === city.trim().toLowerCase())
  const codes = [...new Set(chosen.flatMap((candidate) => candidate.postalCodes))].sort()
  const singleCode = codes.length === 1 ? codes[0] : undefined
  useEffect(() => {
    if (singleCode && postalCode !== singleCode) setValue('postalCode', singleCode, { shouldDirty: true, shouldValidate: true })
  }, [singleCode, postalCode, setValue])

  const choose = (candidate: City) => {
    setValue('city', candidate.name, { shouldDirty: true, shouldValidate: true })
    const only = candidate.postalCodes.length === 1 ? candidate.postalCodes[0] : ''
    setValue('postalCode', only, { shouldDirty: true, shouldValidate: Boolean(only) })
    setOpen(false)
  }
  const showList = open && term.length >= minCitySearchLength && cities.length > 0
  const onKeyDown = (event: React.KeyboardEvent) => {
    if (!showList) return
    if (event.key === 'ArrowDown') setActive((i) => Math.min(i + 1, cities.length - 1))
    else if (event.key === 'ArrowUp') setActive((i) => Math.max(i - 1, 0))
    else if (event.key === 'Enter') choose(cities[active])
    else if (event.key === 'Escape') setOpen(false)
    else return
    event.preventDefault()
  }
  const cityInvalid = Boolean(get(formState.errors, 'city'))
  const cityField = register('city')

  return (
    <>
      <TextField name="street" label={t('tenantForm.street')} autoComplete="street-address" className="sm:col-span-2" />
      <div className="fieldset relative py-1.5">
        <label htmlFor={`${listId}-input`} className="fieldset-legend py-1 text-[12px] font-medium text-base-content/80">
          {t('tenantForm.city')}
        </label>
        <label className={`input w-full ${cityInvalid ? 'input-error' : ''}`}>
          <FontAwesomeIcon icon={faLocationDot} className="text-base-content/40" />
          <input
            id={`${listId}-input`}
            role="combobox"
            aria-expanded={showList}
            aria-controls={listId}
            aria-autocomplete="list"
            autoComplete="off"
            {...cityField}
            onChange={(event) => {
              void cityField.onChange(event)
              setOpen(true)
              setActive(0)
            }}
            onBlur={(event) => {
              void cityField.onBlur(event)
              // Let a click on a suggestion land before closing.
              setTimeout(() => setOpen(false), 150)
            }}
            onKeyDown={onKeyDown}
          />
          {isFetching && <span className="loading loading-spinner loading-xs" />}
        </label>
        {showList && (
          <ul id={listId} role="listbox" className="absolute top-full z-20 mt-1 max-h-72 w-full overflow-y-auto rounded-box border border-base-300 bg-base-100 shadow-lg">
            {cities.map((candidate, i) => {
              const { before, match, after } = highlight(candidate.name, term)
              return (
                <li
                  key={`${candidate.name}-${candidate.provinceCode}`}
                  role="option"
                  aria-selected={i === active}
                  className={`flex cursor-pointer items-center justify-between px-3 py-2 ${i === active ? 'bg-primary/8' : 'hover:bg-base-200'}`}
                  onMouseDown={(event) => event.preventDefault()}
                  onClick={() => choose(candidate)}
                >
                  <span>
                    {before}
                    <b className="font-semibold">{match}</b>
                    {after} <span className="text-base-content/50">({candidate.provinceCode})</span>
                  </span>
                  <span className="data text-[11.5px] text-base-content/60">{postalCodeRange(candidate.postalCodes)}</span>
                </li>
              )
            })}
            <li className="flex justify-between border-t border-base-300 px-3 py-1.5 text-[11px] text-base-content/50">
              <span>{t('tenantForm.cityKeys')}</span>
              <span>{t('tenantForm.citySource')}</span>
            </li>
          </ul>
        )}
        <FieldError name="city" />
        {term.length >= minCitySearchLength && isError && <p className="text-[12px] text-warning">{t('tenantForm.cityListUnavailable')}</p>}
      </div>
      {codes.length > 1 ? (
        <div className="fieldset py-1.5">
          <label htmlFor={`${listId}-cap`} className="fieldset-legend py-1 text-[12px] font-medium text-base-content/80">
            {t('tenantForm.postalCode')}
          </label>
          <select id={`${listId}-cap`} className={`select w-full font-mono ${get(formState.errors, 'postalCode') ? 'select-error' : ''}`} {...register('postalCode')}>
            <option value="">{t('tenantForm.choosePostalCode')}</option>
            {codes.map((code) => (
              <option key={code} value={code}>
                {code}
              </option>
            ))}
          </select>
          <FieldError name="postalCode" />
        </div>
      ) : (
        <TextField
          name="postalCode"
          label={t('tenantForm.postalCode')}
          inputMode="numeric"
          autoComplete="off"
          className="[&_input]:font-mono [&_input]:placeholder:font-sans"
          placeholder={t('tenantForm.postalCodeFromCity')}
          // Filled from the city; editable only when the city list cannot tell (free text, list down).
          readOnly={Boolean(singleCode)}
        />
      )}
    </>
  )
}
