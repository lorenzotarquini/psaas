import { faCircleCheck, faMagnifyingGlass, faPlus } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { Link, useLocation, useSearchParams } from 'react-router'
import { useUsers } from '@/entities/user'
import { useDebouncedSearch } from '@/shared/lib/debounce'
import { formatDate } from '@/shared/lib/format'
import { routes, tenantPath, type UserCreatedState } from '@/shared/routes'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { PageHeader } from '@/shared/ui/page-header'

// UC7: global search on first name, last name and email, with profiles and their tenants.
export function UsersPage() {
  const { t, i18n } = useTranslation()
  const [searchParams, setSearchParams] = useSearchParams()
  const search = searchParams.get('search')?.trim() ?? ''
  const createdEmail = (useLocation().state as UserCreatedState | null)?.createdEmail
  const { data: users, error, isPending, isPlaceholderData } = useUsers(search)

  // replace keeps typing out of the browser history.
  const [searchText, setSearchText] = useDebouncedSearch(search, (value) =>
    setSearchParams(value ? { search: value } : {}, { replace: true }),
  )

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        title={t('users.title')}
        actions={
          <Link to={routes.userNew} className="btn btn-primary btn-sm">
            <FontAwesomeIcon icon={faPlus} />
            {t('users.create')}
          </Link>
        }
      />
      {createdEmail && (
        <div role="status" className="alert alert-success">
          <FontAwesomeIcon icon={faCircleCheck} />
          <span>{t('users.created', { email: createdEmail })}</span>
        </div>
      )}
      <div className="card bg-base-100 shadow-sm">
        <div className="card-body gap-4">
          <label className="input w-full max-w-md">
            <FontAwesomeIcon icon={faMagnifyingGlass} className="opacity-50" />
            <input
              type="search"
              placeholder={t('users.searchPlaceholder')}
              aria-label={t('users.searchPlaceholder')}
              value={searchText}
              onChange={(e) => setSearchText(e.target.value)}
            />
          </label>

          {error ? (
            <ErrorAlert error={error} />
          ) : isPending ? (
            <span className="loading loading-spinner" aria-label={t('loading')} />
          ) : users.length === 0 ? (
            <p className="py-8 text-center text-base-content/70">
              {search ? t('users.noResults', { search }) : t('users.empty')}
            </p>
          ) : (
            <div className={`overflow-x-auto ${isPlaceholderData ? 'opacity-60' : ''}`}>
              <table className="table table-zebra">
                <thead>
                  <tr>
                    <th>{t('users.name')}</th>
                    <th>{t('users.email')}</th>
                    <th>{t('users.birthDate')}</th>
                    <th>{t('tenantDetail.profiles')}</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((user) => (
                    <tr key={user.id}>
                      <td className="font-medium">
                        {user.firstName} {user.lastName}
                      </td>
                      <td className="break-all">{user.email}</td>
                      <td>{user.birthDate && formatDate(user.birthDate, i18n.language)}</td>
                      <td>
                        {user.profiles.length === 0 ? (
                          <span className="text-base-content/50">{t('users.noProfiles')}</span>
                        ) : (
                          <div className="flex flex-wrap gap-1">
                            {user.profiles.map((profile) => (
                              <Link key={profile.id} to={tenantPath(profile.tenantId)} className="badge badge-outline hover:badge-primary">
                                {profile.name} · {profile.tenantCompanyName}
                              </Link>
                            ))}
                          </div>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
