import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { routes } from '@/shared/routes'
import { InvoicesTable } from '@/widgets/invoices-table'

// Mock 1d "Fatture": the invoices of this tenant; a row opens it in the billing page.
export function InvoicesTab({ tenantId }: { tenantId: string }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [query, setQuery] = useState({ tenantId, page: 1, pageSize: 20 })

  return (
    <div className="panel overflow-hidden">
      <InvoicesTable
        query={query}
        onQueryChange={(next) => setQuery({ ...query, ...next })}
        onSelect={(invoice) => navigate(`${routes.billing}?${new URLSearchParams({ invoice })}`)}
        hideTenant
        emptyText={t('billing.noTenantInvoices')}
      />
    </div>
  )
}
