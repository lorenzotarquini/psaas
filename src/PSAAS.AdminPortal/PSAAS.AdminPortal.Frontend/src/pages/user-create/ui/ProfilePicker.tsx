import { get, useFormContext } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useProfiles } from '@/entities/profile'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { groupProfilesByTenant, type UserValues } from '../model/user'

export function ProfilePicker() {
  const { t } = useTranslation()
  const { data: profiles = [], error, isPending } = useProfiles()
  const { watch, setValue, formState } = useFormContext<UserValues>()
  const selected = watch('profileIds')
  // The backend reports an unknown profile as profileIds[i] = InvalidValue.
  const invalid = Boolean(get(formState.errors, 'profileIds'))

  // Checkboxes managed by hand: a single registered checkbox would yield a boolean, not an array.
  const toggle = (id: string, checked: boolean) =>
    setValue('profileIds', checked ? [...selected, id] : selected.filter((s) => s !== id), { shouldDirty: true })

  if (error) return <ErrorAlert error={error} />
  if (isPending) return <span className="loading loading-spinner" aria-label={t('loading')} />
  if (profiles.length === 0) return <p className="text-sm text-base-content/70">{t('profiles.empty')}</p>

  return (
    <div className="flex flex-col gap-3">
      {groupProfilesByTenant(profiles).map((group) => (
        <fieldset key={group.tenantId}>
          <legend className="font-medium">{group.tenantName}</legend>
          <div className="flex flex-wrap gap-x-4 gap-y-1">
            {group.profiles.map((profile) => (
              <label key={profile.id} className="flex items-center gap-2 text-sm" title={profile.description ?? undefined}>
                <input
                  type="checkbox"
                  className="checkbox checkbox-sm"
                  checked={selected.includes(profile.id)}
                  onChange={(e) => toggle(profile.id, e.target.checked)}
                />
                {profile.name}
              </label>
            ))}
          </div>
        </fieldset>
      ))}
      {invalid && <p className="text-error text-sm">{t('userForm.invalidProfile')}</p>}
    </div>
  )
}
