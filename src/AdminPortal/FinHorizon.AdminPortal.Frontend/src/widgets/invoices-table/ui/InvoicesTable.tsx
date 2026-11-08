import { useTranslation } from 'react-i18next'
import { formatMoney, InvoiceStatusBadge, useInvoices, type InvoicesQuery } from '@/entities/invoice'
import { formatDate } from '@/shared/lib/format'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { Pagination } from '@/shared/ui/pagination'

const pageSizes = [20, 50, 100] as const

interface InvoicesTableProps {
  query: InvoicesQuery
  onQueryChange: (next: Partial<InvoicesQuery>) => void
  selectedId?: string | null
  onSelect: (id: string) => void
  /** The tenant tab already knows the customer. */
  hideTenant?: boolean
  emptyText: string
}

// UC24: invoices and credit notes, most recent first; a row opens the invoice.
export function InvoicesTable({ query, onQueryChange, selectedId, onSelect, hideTenant, emptyText }: InvoicesTableProps) {
  const { t, i18n } = useTranslation()
  const { data, error, isPending, isPlaceholderData } = useInvoices(query)

  if (error) return <ErrorAlert error={error} />
  if (isPending) return <span className="loading loading-spinner m-4" aria-label={t('loading')} />
  if (data.items.length === 0) return <p className="py-10 text-center text-base-content/60">{emptyText}</p>

  return (
    <div className={isPlaceholderData ? 'opacity-60' : ''}>
      <div className="overflow-x-auto">
        <table className="table table-dense">
          <thead>
            <tr>
              <th>{t('billing.number')}</th>
              {!hideTenant && <th>{t('billing.customer')}</th>}
              <th>{t('billing.issuedAt')}</th>
              <th>{t('billing.dueAt')}</th>
              <th className="text-right">{t('billing.total')}</th>
              <th>{t('billing.statusLabel')}</th>
            </tr>
          </thead>
          <tbody>
            {data.items.map((invoice) => (
              <tr
                key={invoice.id}
                className={`cursor-pointer hover:bg-base-200/60 ${invoice.id === selectedId ? 'bg-primary/6 shadow-[inset_3px_0_0_var(--color-primary)]' : ''}`}
                onClick={() => onSelect(invoice.id)}
              >
                <td>
                  <button type="button" className="data font-semibold" onClick={() => onSelect(invoice.id)}>
                    {invoice.number}
                  </button>
                  {invoice.kind === 'CreditNote' && <span className="badge badge-ghost badge-sm ml-2">{t('billing.kind.CreditNote')}</span>}
                </td>
                {!hideTenant && <td>{invoice.tenantName}</td>}
                <td className="data">{formatDate(invoice.issuedAt, i18n.language)}</td>
                <td className={`data ${invoice.status === 'Overdue' ? 'text-error' : ''}`}>{formatDate(invoice.dueAt, i18n.language)}</td>
                <td className="data text-right">
                  {invoice.kind === 'CreditNote' && '−'}
                  {formatMoney(invoice.total, i18n.language)}
                </td>
                <td>
                  <span className="flex items-center gap-1.5">
                    <InvoiceStatusBadge status={invoice.status} />
                    {invoice.remindersSent > 0 && <span className="text-[11px] text-base-content/55">{t('billing.reminders', { count: invoice.remindersSent })}</span>}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Pagination
        page={query.page}
        pageSize={query.pageSize}
        totalCount={data.totalCount}
        pageSizes={pageSizes}
        onPageChange={(page) => onQueryChange({ page })}
        onPageSizeChange={(pageSize) => onQueryChange({ pageSize, page: 1 })}
      />
    </div>
  )
}
