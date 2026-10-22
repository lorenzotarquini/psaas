import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { routes } from '@/shared/routes'
import { UsersTable } from '@/widgets/users-table'

// Mock 1d "Utenti": the users of this tenant; a row opens the user in the users page (UC19 panel).
export function UsersTab({ tenantId }: { tenantId: string }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [query, setQuery] = useState({ search: '', tenantId, page: 1, pageSize: 20 })

  return (
    <div className="panel overflow-hidden">
      <UsersTable
        query={query}
        onQueryChange={(next) => setQuery({ ...query, ...next })}
        onSelect={(user) => navigate(`${routes.users}?${new URLSearchParams({ tenantId, user })}`)}
        emptyText={t('users.noTenantUsers')}
      />
    </div>
  )
}
