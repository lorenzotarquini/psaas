import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { AuditEventItem, useAudit } from '@/entities/audit'
import { tenantPath } from '@/shared/routes'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { Pagination } from '@/shared/ui/pagination'

// UC12: the history of one tenant. Side card (latest events, mock 1d) or the full History tab.
export function HistoryCard({ tenantId, full = false }: { tenantId: string; full?: boolean }) {
  const { t } = useTranslation()
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(full ? 25 : 4)
  const { data, error, isPending } = useAudit({ tenantId, page, pageSize })

  return (
    <section className={`panel ${full ? 'overflow-hidden' : 'px-4 py-3.5'}`}>
      <header className={`flex items-baseline justify-between ${full ? 'border-b border-base-300 px-4 py-3' : 'mb-1.5'}`}>
        <h2 className="panel-title">{t('audit.history')}</h2>
        {!full && (
          <Link to={tenantPath(tenantId, 'history')} className="text-[12px] text-primary hover:underline">
            {t('audit.historyAll')} →
          </Link>
        )}
      </header>
      {error ? (
        <ErrorAlert error={error} />
      ) : isPending ? (
        <span className="loading loading-spinner loading-sm" aria-label={t('loading')} />
      ) : data.items.length === 0 ? (
        <p className="py-3 text-base-content/60">{t('audit.empty')}</p>
      ) : (
        <>
          <ul className={full ? 'divide-y divide-base-200 px-4' : ''}>
            {data.items.map((event) => (
              <AuditEventItem key={event.id} event={event} showDetail={full} />
            ))}
          </ul>
          {full && (
            <Pagination
              page={page}
              pageSize={pageSize}
              totalCount={data.totalCount}
              pageSizes={[25, 50, 100]}
              onPageChange={setPage}
              onPageSizeChange={(size) => {
                setPageSize(size)
                setPage(1)
              }}
            />
          )}
        </>
      )}
    </section>
  )
}
