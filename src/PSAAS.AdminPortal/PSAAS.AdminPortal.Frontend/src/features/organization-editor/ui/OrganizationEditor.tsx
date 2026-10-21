import { faPlus, faTrash } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { get, useFieldArray, useFormContext } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { FieldError, TextField } from '@/shared/ui/form-field'
import { newProfile, newUnit, type OrganizationValues } from '../model/organization'

// UC3 step 2 and UC5: profiles, productive units and which units each profile opens.
// Must be inside a FormProvider whose values include OrganizationValues.
export function OrganizationEditor() {
  const { t } = useTranslation()
  const { control, watch, getValues, setValue, formState } = useFormContext<OrganizationValues>()
  const profiles = useFieldArray({ control, name: 'profiles', keyName: 'fieldKey' })
  const units = useFieldArray({ control, name: 'productiveUnits', keyName: 'fieldKey' })
  const profileValues = watch('profiles')

  const removeProfile = (index: number) => {
    const key = getValues(`profiles.${index}.key`)
    // Units must not keep pointing to a profile that is no longer in the request.
    getValues('productiveUnits').forEach((unit, i) => {
      if (unit.profileKeys.includes(key)) {
        setValue(`productiveUnits.${i}.profileKeys`, unit.profileKeys.filter((k) => k !== key), { shouldDirty: true })
      }
    })
    profiles.remove(index)
  }

  const toggleAccess = (unitIndex: number, key: string, open: boolean) => {
    const current = getValues(`productiveUnits.${unitIndex}.profileKeys`)
    setValue(
      `productiveUnits.${unitIndex}.profileKeys`,
      open ? [...current, key] : current.filter((k) => k !== key),
      { shouldDirty: true },
    )
  }

  return (
    <div className="flex flex-col gap-6">
      <section>
        <h3 className="font-medium">{t('tenantDetail.profiles')}</h3>
        {profiles.fields.length === 0 && <p className="text-sm text-base-content/70">{t('tenantDetail.noProfiles')}</p>}
        {profiles.fields.map((field, index) => (
          <div key={field.fieldKey} className="flex flex-wrap items-start gap-x-3 border-b border-base-300 py-2">
            <TextField name={`profiles.${index}.name`} label={t('organizationEditor.name')} className="min-w-48 flex-1" />
            <TextField
              name={`profiles.${index}.description`}
              label={t('organizationEditor.description')}
              className="min-w-48 flex-[2]"
            />
            <button
              type="button"
              className="btn btn-ghost btn-square mt-8 text-error"
              aria-label={t('organizationEditor.removeProfile')}
              onClick={() => removeProfile(index)}
            >
              <FontAwesomeIcon icon={faTrash} />
            </button>
            <FieldError name={`profiles.${index}.key`} />
            <FieldError name={`profiles.${index}.id`} />
          </div>
        ))}
        <button type="button" className="btn btn-sm btn-outline mt-2" onClick={() => profiles.append(newProfile())}>
          <FontAwesomeIcon icon={faPlus} />
          {t('organizationEditor.addProfile')}
        </button>
      </section>

      <section>
        <h3 className="font-medium">{t('tenantDetail.productiveUnits')}</h3>
        {units.fields.length === 0 && (
          <p className="text-sm text-base-content/70">{t('tenantDetail.noProductiveUnits')}</p>
        )}
        {units.fields.map((field, index) => {
          const openedBy = watch(`productiveUnits.${index}.profileKeys`)
          const invalidLink = Boolean(get(formState.errors, `productiveUnits.${index}.profileKeys`))
          return (
            <div key={field.fieldKey} className="border-b border-base-300 py-2">
              <div className="flex flex-wrap items-start gap-x-3">
                <TextField name={`productiveUnits.${index}.name`} label={t('organizationEditor.name')} className="min-w-48 flex-1" />
                <TextField
                  name={`productiveUnits.${index}.description`}
                  label={t('organizationEditor.description')}
                  className="min-w-48 flex-[2]"
                />
                <button
                  type="button"
                  className="btn btn-ghost btn-square mt-8 text-error"
                  aria-label={t('organizationEditor.removeUnit')}
                  onClick={() => units.remove(index)}
                >
                  <FontAwesomeIcon icon={faTrash} />
                </button>
              </div>
              <FieldError name={`productiveUnits.${index}.id`} />
              <fieldset className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm">
                <legend className="mb-1 text-base-content/70">{t('organizationEditor.openedBy')}</legend>
                {profileValues.length === 0 && <span className="text-base-content/70">{t('tenantDetail.noProfiles')}</span>}
                {profileValues.map((profile) => (
                  <label key={profile.key} className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      className="checkbox checkbox-sm"
                      checked={openedBy.includes(profile.key)}
                      onChange={(e) => toggleAccess(index, profile.key, e.target.checked)}
                    />
                    {profile.name || t('organizationEditor.unnamedProfile')}
                  </label>
                ))}
              </fieldset>
              {invalidLink && <p className="text-error text-sm">{t('organizationEditor.invalidProfileLink')}</p>}
            </div>
          )
        })}
        <button type="button" className="btn btn-sm btn-outline mt-2" onClick={() => units.append(newUnit())}>
          <FontAwesomeIcon icon={faPlus} />
          {t('organizationEditor.addUnit')}
        </button>
      </section>
    </div>
  )
}
