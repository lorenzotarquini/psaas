import { useTranslation } from 'react-i18next'
import { UserAvatar, UserStatusBadge, useUsers, type UsersQuery } from '@/entities/user'
import { formatWhen } from '@/shared/lib/format'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { Pagination } from '@/shared/ui/pagination'
import { userPageSizes } from '../model/pageSizes'

interface UsersTableProps {
  query: UsersQuery
  onQueryChange: (next: Partial<UsersQuery>) => void
  selectedId?: string | null
  onSelect: (id: string) => void
  /** Shown when nothing matches. */
  emptyText: string
}

// Table of mock 1f: avatar, profiles · tenant, status, last access; the selected row opens the side panel.
export function UsersTable({ query, onQueryChange, selectedId, onSelect, emptyText }: UsersTableProps) {
  const { t, i18n } = useTranslation()
  const { data, error, isPending, isPlaceholderData } = useUsers(query)

  if (error) return <ErrorAlert error={error} />
  if (isPending) return <span className="loading loading-spinner m-4" aria-label={t('loading')} />
  if (data.items.length === 0) return <p className="py-10 text-center text-base-content/60">{emptyText}</p>

  return (
    <>
      <div className={`overflow-x-auto ${isPlaceholderData ? 'opacity-60' : ''}`}>
        <table className="table table-dense">
          <thead>
            <tr>
              <th>{t('users.user')}</th>
              <th>{t('users.profilesTenant')}</th>
              <th>{t('users.statusLabel')}</th>
              <th>{t('users.lastLogin')}</th>
            </tr>
          </thead>
          <tbody>
            {data.items.map((user) => {
              const selected = user.id === selectedId
              return (
                <tr
                  key={user.id}
                  className={`cursor-pointer hover:bg-base-200/60 ${selected ? 'bg-primary/6 shadow-[inset_3px_0_0_var(--color-primary)]' : ''}`}
                  onClick={() => onSelect(user.id)}
                  aria-selected={selected}
                >
                  <td>
                    <button type="button" className="flex items-center gap-2.5 text-left" onClick={() => onSelect(user.id)}>
                      <UserAvatar firstName={user.firstName} lastName={user.lastName} email={user.email} />
                      <span>
                        <span className="block font-semibold">
                          {user.firstName} {user.lastName}
                        </span>
                        <span className="block text-[12px] break-all text-base-content/60">{user.email}</span>
                      </span>
                    </button>
                  </td>
                  <td>
                    <div className="flex flex-col items-start gap-1">
                      {user.profiles.length === 0 ? (
                        <span className="badge badge-outline badge-sm font-semibold">{t('users.noProfiles')}</span>
                      ) : (
                        user.profiles.map((p) => (
                          <span key={p.id} className="badge badge-outline badge-sm gap-1.5 rounded-md border-base-300">
                            <span className="font-semibold">{p.name}</span>
                            <span className="text-base-content/60">{p.tenantCompanyName}</span>
                          </span>
                        ))
                      )}
                    </div>
                  </td>
                  <td>
                    <UserStatusBadge status={user.status} />
                  </td>
                  <td className="data text-[12.5px]">
                    {user.lastLoginAt ? formatWhen(user.lastLoginAt, i18n.language, t('audit.yesterday')) : t('users.never')}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
      <Pagination
        page={query.page}
        pageSize={query.pageSize}
        totalCount={data.totalCount}
        pageSizes={userPageSizes}
        onPageChange={(page) => onQueryChange({ page })}
        onPageSizeChange={(pageSize) => onQueryChange({ pageSize, page: 1 })}
      />
    </>
  )
}
