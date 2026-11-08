import { faCircleCheck, faFileCsv, faMagnifyingGlass, faUserPlus } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation, useSearchParams } from 'react-router'
import { useProfiles } from '@/entities/profile'
import { userStatuses, useUsers } from '@/entities/user'
import { UserImportDialog } from '@/features/user-import'
import { UserPanel } from '@/features/user-panel'
import { useDebouncedSearch } from '@/shared/lib/debounce'
import { routes, type UserCreatedState } from '@/shared/routes'
import { PageHeader } from '@/shared/ui/page-header'
import { UsersTable } from '@/widgets/users-table'
import { readUsersQuery, writeUsersQuery } from '../model/searchParams'

// UC7 + UC19 + UC20 (mock 1f): global search with filters, side panel for the selected user, CSV import.
export function UsersPage() {
  const { t } = useTranslation()
  const [searchParams, setSearchParams] = useSearchParams()
  const query = readUsersQuery(searchParams)
  const selected = searchParams.get('user')
  const createdEmail = (useLocation().state as UserCreatedState | null)?.createdEmail
  const { data: profiles = [] } = useProfiles()
  const { data } = useUsers(query)
  const [importing, setImporting] = useState(false)
  const tenants = [...new Map(profiles.map((p) => [p.tenantId, p.tenantCompanyName]))].sort((a, b) => a[1].localeCompare(b[1]))
  const tenantProfiles = profiles.filter((p) => !query.tenantId || p.tenantId === query.tenantId)

  const navigate = (next: Partial<typeof query> & { user?: string | null }, replace = false) =>
    setSearchParams(writeUsersQuery({ ...query, ...next }, next.user === undefined ? selected : next.user), { replace })
  // A new search starts from page 1; replace keeps typing out of the browser history.
  const [searchText, setSearchText] = useDebouncedSearch(query.search, (search) => navigate({ search, page: 1 }, true))

  return (
    <div className={`grid gap-4 ${selected ? 'xl:-mr-7 xl:grid-cols-[minmax(0,1fr)_400px]' : ''}`}>
      <div className="flex min-w-0 flex-col gap-4">
        <PageHeader
          title={t('users.title')}
          count={data?.totalCount}
          actions={
            <>
              <button type="button" className="btn btn-ghost btn-sm" onClick={() => setImporting(true)}>
                <FontAwesomeIcon icon={faFileCsv} />
                {t('users.import.button')}
              </button>
              <Link to={routes.userNew} className="btn btn-primary btn-sm">
                <FontAwesomeIcon icon={faUserPlus} />
                {t('users.create')}
              </Link>
            </>
          }
        />
        {createdEmail && (
          <div role="status" className="alert alert-success alert-soft">
            <FontAwesomeIcon icon={faCircleCheck} />
            <span>{t('users.created', { email: createdEmail })}</span>
          </div>
        )}
        <div className="flex flex-wrap items-center gap-2">
          <label className="input w-full max-w-xs">
            <FontAwesomeIcon icon={faMagnifyingGlass} className="text-base-content/40" />
            <input
              type="search"
              placeholder={t('users.searchPlaceholder')}
              aria-label={t('users.searchPlaceholder')}
              value={searchText}
              onChange={(e) => setSearchText(e.target.value)}
            />
          </label>
          <FilterSelect
            label={t('users.filters.tenant')}
            value={query.tenantId}
            options={tenants}
            onChange={(tenantId) => navigate({ tenantId, profileId: undefined, page: 1 })}
          />
          <FilterSelect
            label={t('users.filters.profile')}
            value={query.profileId}
            options={tenantProfiles.map((p) => [p.id, query.tenantId ? p.name : `${p.name} · ${p.tenantCompanyName}`])}
            onChange={(profileId) => navigate({ profileId, page: 1 })}
          />
          <FilterSelect
            label={t('users.filters.status')}
            value={query.status}
            options={userStatuses.map((s) => [s, t(`users.status.${s}`)])}
            onChange={(status) => navigate({ status: status as typeof query.status, page: 1 })}
          />
          {data && (
            <span className="text-[12px] text-base-content/60">{t('users.resultCount', { count: data.totalCount })}</span>
          )}
        </div>
        <div className="panel overflow-hidden">
          <UsersTable
            query={query}
            onQueryChange={(next) => navigate(next)}
            selectedId={selected}
            onSelect={(user) => navigate({ user })}
            emptyText={query.search ? t('users.noResults', { search: query.search }) : t('users.empty')}
          />
        </div>
      </div>
      {selected && (
        <div className="relative xl:sticky xl:top-0 xl:-my-6 xl:h-[calc(100vh-56px)]">
          <UserPanel userId={selected} onClose={() => navigate({ user: null })} />
        </div>
      )}
      <UserImportDialog open={importing} onClose={() => setImporting(false)} />
    </div>
  )
}

// Filter chip of mock 1f: dashed when empty, solid when set.
function FilterSelect({
  label,
  value,
  options,
  onChange,
}: {
  label: string
  value: string | undefined
  options: [string, string][]
  onChange: (value: string | undefined) => void
}) {
  return (
    <select
      className={`select select-sm w-auto max-w-56 ${value ? 'border-primary text-primary' : 'border-dashed'}`}
      aria-label={label}
      value={value ?? ''}
      onChange={(e) => onChange(e.target.value || undefined)}
    >
      <option value="">{label}</option>
      {options.map(([id, name]) => (
        <option key={id} value={id}>
          {name}
        </option>
      ))}
    </select>
  )
}
