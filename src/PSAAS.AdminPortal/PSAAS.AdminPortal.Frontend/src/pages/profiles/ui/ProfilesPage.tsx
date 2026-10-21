import { faFilter } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useProfiles } from '@/entities/profile'
import { useTenantNames } from '@/entities/tenant'
import { tenantPath } from '@/shared/routes'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { PageHeader } from '@/shared/ui/page-header'

// UC9: read-only overview of every profile of every tenant, ordered by name (by the backend).
export function ProfilesPage() {
  const { t } = useTranslation()
  const { data: profiles = [], error, isPending } = useProfiles()
  const tenantNames = useTenantNames(profiles.map((p) => p.tenantId))
  const [filter, setFilter] = useState('')

  // The whole list is already here: filter on the client.
  const needle = filter.trim().toLowerCase()
  const visible = profiles.filter((profile) =>
    [profile.name, profile.description ?? '', tenantNames.get(profile.tenantId) ?? ''].some((text) =>
      text.toLowerCase().includes(needle),
    ),
  )

  return (
    <div className="flex flex-col gap-4">
      <PageHeader title={t('profiles.title')} />
      <div className="card bg-base-100 shadow-sm">
        <div className="card-body gap-4">
          {error ? (
            <ErrorAlert error={error} />
          ) : isPending ? (
            <span className="loading loading-spinner" aria-label={t('loading')} />
          ) : profiles.length === 0 ? (
            <p className="py-8 text-center text-base-content/70">{t('profiles.empty')}</p>
          ) : (
            <>
              <label className="input w-full max-w-md">
                <FontAwesomeIcon icon={faFilter} className="opacity-50" />
                <input
                  type="search"
                  placeholder={t('profiles.filterPlaceholder')}
                  aria-label={t('profiles.filterPlaceholder')}
                  value={filter}
                  onChange={(e) => setFilter(e.target.value)}
                />
              </label>
              <p className="text-sm text-base-content/70">
                {t('profiles.count', { count: visible.length, total: profiles.length })}
              </p>
              <div className="overflow-x-auto">
                <table className="table table-zebra">
                  <thead>
                    <tr>
                      <th>{t('tenantDetail.profile')}</th>
                      <th>{t('organizationEditor.description')}</th>
                      <th>{t('profiles.tenant')}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {visible.map((profile) => (
                      <tr key={profile.id}>
                        <td className="font-medium">{profile.name}</td>
                        <td>{profile.description}</td>
                        <td>
                          <Link to={tenantPath(profile.tenantId)} className="link link-hover">
                            {tenantNames.get(profile.tenantId) ?? <span className="loading loading-dots loading-xs" />}
                          </Link>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
