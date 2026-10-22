import { faIdBadge, faIndustry, faPlus, faRotateLeft, faTrash } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { get, useFieldArray, useFormContext } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { FieldError } from '@/shared/ui/form-field'
import { accessCell, newProfile, newUnit, type OrganizationValues } from '../model/organization'

interface OrganizationMatrixProps {
  /** The saved organization, to highlight what changed; empty when creating a tenant. */
  original?: OrganizationValues
  /** Users per saved profile id (UC2-R2); null when Keycloak could not count them. */
  userCounts?: Record<string, number | null>
}

// Mock 1d: profiles × productive units, a click on a cell opens or closes the access (UC3 step 3, UC5).
// Must be inside a FormProvider whose values include OrganizationValues.
export function OrganizationMatrix({ original = { profiles: [], productiveUnits: [] }, userCounts }: OrganizationMatrixProps) {
  const { t } = useTranslation()
  const { control, register, watch, getValues, setValue, formState } = useFormContext<OrganizationValues>()
  const profiles = useFieldArray({ control, name: 'profiles', keyName: 'fieldKey' })
  const units = useFieldArray({ control, name: 'productiveUnits', keyName: 'fieldKey' })
  const values = watch()

  // A new item disappears; a saved one is struck through until saved or restored.
  const toggleProfileRemoved = (index: number) => {
    if (!getValues(`profiles.${index}.id`)) return profiles.remove(index)
    setValue(`profiles.${index}.removed`, !getValues(`profiles.${index}.removed`), { shouldDirty: true })
  }
  const toggleUnitRemoved = (index: number) => {
    if (!getValues(`productiveUnits.${index}.id`)) return units.remove(index)
    setValue(`productiveUnits.${index}.removed`, !getValues(`productiveUnits.${index}.removed`), { shouldDirty: true })
  }
  const toggleAccess = (unitIndex: number, key: string) => {
    const current = getValues(`productiveUnits.${unitIndex}.profileKeys`)
    setValue(
      `productiveUnits.${unitIndex}.profileKeys`,
      current.includes(key) ? current.filter((k) => k !== key) : [...current, key],
      { shouldDirty: true },
    )
  }
  const invalid = (name: string) => Boolean(get(formState.errors, name))

  return (
    <section className="panel overflow-hidden">
      <header className="flex flex-wrap items-start justify-between gap-3 border-b border-base-300 px-4 py-3">
        <div>
          <h2 className="panel-title">{t('organizationEditor.matrixTitle')}</h2>
          <p className="text-base-content/60">{t('organizationEditor.matrixHint')}</p>
        </div>
        <div className="flex gap-2">
          <button type="button" className="btn btn-sm" onClick={() => profiles.append(newProfile())}>
            <FontAwesomeIcon icon={faIdBadge} />
            {t('organizationEditor.addProfile')}
          </button>
          <button type="button" className="btn btn-sm" onClick={() => units.append(newUnit())}>
            <FontAwesomeIcon icon={faIndustry} />
            {t('organizationEditor.addUnit')}
          </button>
        </div>
      </header>

      {profiles.fields.length === 0 && units.fields.length === 0 ? (
        <p className="px-4 py-10 text-center text-base-content/60">{t('tenantDetail.noOrganization')}</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="table table-dense">
            <thead>
              <tr className="align-bottom">
                <th className="min-w-44">{t('tenantDetail.profile')}</th>
                {userCounts && <th className="w-16">{t('organizationEditor.users')}</th>}
                {units.fields.map((unit, u) => {
                  const removed = values.productiveUnits[u]?.removed
                  return (
                    <th key={unit.fieldKey} className={`min-w-24 text-center ${removed ? 'bg-error/5' : ''}`}>
                      <div className="flex flex-col items-center gap-0.5">
                        <div className="flex items-center gap-1">
                          <FontAwesomeIcon icon={faIndustry} className="text-base-content/50" />
                          <button
                            type="button"
                            className="btn btn-ghost btn-xs h-5 min-h-5 px-1 text-base-content/40"
                            aria-label={removed ? t('organizationEditor.restore') : t('organizationEditor.removeUnit')}
                            onClick={() => toggleUnitRemoved(u)}
                          >
                            <FontAwesomeIcon icon={removed ? faRotateLeft : faTrash} className="text-[10px]" />
                          </button>
                        </div>
                        <input
                          {...register(`productiveUnits.${u}.name`)}
                          aria-label={t('organizationEditor.unitName')}
                          placeholder={t('organizationEditor.unitName')}
                          aria-invalid={invalid(`productiveUnits.${u}.name`)}
                          className={`input input-ghost input-xs h-6 w-full text-center font-semibold text-base-content ${removed ? 'line-through' : ''} ${invalid(`productiveUnits.${u}.name`) ? 'input-error' : ''}`}
                        />
                        <input
                          {...register(`productiveUnits.${u}.description`)}
                          aria-label={t('organizationEditor.description')}
                          placeholder={t('organizationEditor.description')}
                          className="input input-ghost input-xs h-5 w-full text-center text-[11.5px] font-normal"
                        />
                        <FieldError name={`productiveUnits.${u}.name`} />
                        <FieldError name={`productiveUnits.${u}.id`} />
                      </div>
                    </th>
                  )
                })}
                <th className="w-8" />
              </tr>
            </thead>
            <tbody>
              {profiles.fields.map((profile, p) => {
                const value = values.profiles[p]
                const removed = value?.removed
                return (
                  <tr key={profile.fieldKey} className={removed ? 'bg-error/5' : 'hover:bg-base-200/50'}>
                    <td>
                      <input
                        {...register(`profiles.${p}.name`)}
                        aria-label={t('organizationEditor.profileName')}
                        placeholder={t('organizationEditor.profileName')}
                        aria-invalid={invalid(`profiles.${p}.name`)}
                        className={`input input-ghost input-xs h-6 w-full px-1 text-[13px] font-semibold ${removed ? 'text-error line-through' : ''} ${invalid(`profiles.${p}.name`) ? 'input-error' : ''}`}
                      />
                      <input
                        {...register(`profiles.${p}.description`)}
                        aria-label={t('organizationEditor.description')}
                        placeholder={t('organizationEditor.description')}
                        className="input input-ghost input-xs h-5 w-full px-1 text-[12px] text-base-content/60"
                      />
                      <FieldError name={`profiles.${p}.name`} />
                      <FieldError name={`profiles.${p}.id`} />
                    </td>
                    {userCounts && (
                      <td className="data">{value?.id ? (userCounts[value.id] ?? '–') : '–'}</td>
                    )}
                    {units.fields.map((unit, u) => {
                      const unitValue = values.productiveUnits[u]
                      if (!unitValue || !value) return <td key={unit.fieldKey} />
                      const { open, changed } = accessCell(original, unitValue, value)
                      return (
                        <td key={unit.fieldKey} className="text-center">
                          <input
                            type="checkbox"
                            className={`checkbox checkbox-sm ${removed || unitValue.removed ? 'checkbox-error' : 'checkbox-primary'} ${changed ? 'ring-3 ring-warning/50' : ''}`}
                            checked={open}
                            disabled={removed || unitValue.removed}
                            aria-label={t('organizationEditor.access', { profile: value.name, unit: unitValue.name })}
                            onChange={() => toggleAccess(u, value.key)}
                          />
                        </td>
                      )
                    })}
                    <td>
                      <button
                        type="button"
                        className="btn btn-ghost btn-xs text-base-content/50"
                        aria-label={removed ? t('organizationEditor.restore') : t('organizationEditor.removeProfile')}
                        onClick={() => toggleProfileRemoved(p)}
                      >
                        <FontAwesomeIcon icon={removed ? faRotateLeft : faTrash} />
                      </button>
                    </td>
                  </tr>
                )
              })}
              {profiles.fields.length === 0 && (
                <tr>
                  <td colSpan={units.fields.length + 3} className="py-6 text-center text-base-content/60">
                    {t('tenantDetail.noProfiles')}
                    <button type="button" className="btn btn-link btn-sm" onClick={() => profiles.append(newProfile())}>
                      <FontAwesomeIcon icon={faPlus} />
                      {t('organizationEditor.addProfile')}
                    </button>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
