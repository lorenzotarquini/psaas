import { faFilter } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useProfiles } from '@/entities/profile'
import { tenantPath } from '@/shared/routes'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { PageHeader } from '@/shared/ui/page-header'

// UC9: read-only overview of every profile of every tenant, ordered by name (by the backend).
export function ProfilesPage() {
  const { t } = useTranslation()
  const { data: profiles = [], error, isPending } = useProfiles()
  const [filter, setFilter] = useState('')

  // The whole list is already here: filter on the client.
  const needle = filter.trim().toLowerCase()
  const visible = profiles.filter((profile) =>
    [profile.name, profile.description ?? '', profile.tenantCompanyName].some((text) => text.toLowerCase().includes(needle)),
  )

  return (
    <>
      <PageHeader title={t('profiles.title')} count={isPending ? undefined : profiles.length} />
      <label className="input w-full max-w-sm">
        <FontAwesomeIcon icon={faFilter} className="text-base-content/40" />
        <input
          type="search"
          placeholder={t('profiles.filterPlaceholder')}
          aria-label={t('profiles.filterPlaceholder')}
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
        />
      </label>
      <div className="panel overflow-hidden">
        {error ? (
          <div className="p-4">
            <ErrorAlert error={error} />
          </div>
        ) : isPending ? (
          <span className="loading loading-spinner m-4" aria-label={t('loading')} />
        ) : profiles.length === 0 ? (
          <p className="py-10 text-center text-base-content/60">{t('profiles.empty')}</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="table table-dense">
              <thead>
                <tr>
                  <th>{t('tenantDetail.profile')}</th>
                  <th>{t('organizationEditor.description')}</th>
                  <th>{t('profiles.tenant')}</th>
                </tr>
              </thead>
              <tbody>
                {visible.map((profile) => (
                  <tr key={profile.id} className="hover:bg-base-200/60">
                    <td className="font-semibold">{profile.name}</td>
                    <td className="text-base-content/70">{profile.description}</td>
                    <td>
                      <Link to={tenantPath(profile.tenantId)} className="link link-hover">
                        {profile.tenantCompanyName}
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <p className="border-t border-base-300 px-3 py-2 text-[12px] text-base-content/60">
              {t('profiles.count', { count: visible.length, total: profiles.length })}
            </p>
          </div>
        )}
      </div>
    </>
  )
}
