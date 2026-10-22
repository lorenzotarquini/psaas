import { faMagnifyingGlass } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router'
import { describeAuditEvent, useAudit } from '@/entities/audit'
import { useDebouncedSearch } from '@/shared/lib/debounce'
import { formatWhen } from '@/shared/lib/format'
import { tenantPath } from '@/shared/routes'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { PageHeader } from '@/shared/ui/page-header'
import { Pagination } from '@/shared/ui/pagination'

const actionFilters = ['tenant.', 'tenant.created', 'tenant.onboarding', 'tenant.status', 'tenant.registry', 'tenant.organization', 'user.'] as const
const pageSizes = [25, 50, 100, 200] as const

// UC12 (EXP-10): every change made through the portal, newest first; filters live in the URL.
export function AuditPage() {
  const { t, i18n } = useTranslation()
  const [params, setParams] = useSearchParams()
  const page = Number(params.get('page')) || 1
  const pageSize = Number(params.get('pageSize')) || 50
  const query = {
    action: params.get('action') ?? undefined,
    search: params.get('search') ?? undefined,
    from: params.get('from') ? new Date(params.get('from')!).toISOString() : undefined,
    // The "to" day is included: the backend bound is exclusive, so move to the next day.
    to: params.get('to') ? new Date(new Date(params.get('to')!).getTime() + 86_400_000).toISOString() : undefined,
    page,
    pageSize,
  }
  const { data, error, isPending, isPlaceholderData } = useAudit(query)

  const update = (changes: Record<string, string | undefined>) =>
    setParams(
      (current) => {
        const next = new URLSearchParams(current)
        Object.entries(changes).forEach(([key, value]) => (value ? next.set(key, value) : next.delete(key)))
        if (!('page' in changes)) next.delete('page')
        return next
      },
      { replace: true },
    )
  const [searchText, setSearchText] = useDebouncedSearch(params.get('search') ?? '', (value) => update({ search: value || undefined }))
  const filtered = Boolean(query.action || query.search || query.from || query.to)

  return (
    <>
      <PageHeader title={t('audit.title')} count={data?.totalCount} subtitle={t('audit.subtitle')} />
      <div className="flex flex-wrap items-center gap-2">
        <label className="input w-full max-w-sm">
          <FontAwesomeIcon icon={faMagnifyingGlass} className="text-base-content/40" />
          <input
            type="search"
            placeholder={t('audit.searchPlaceholder')}
            aria-label={t('audit.searchPlaceholder')}
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
          />
        </label>
        <select
          className="select w-auto"
          aria-label={t('audit.columns.event')}
          value={query.action ?? ''}
          onChange={(e) => update({ action: e.target.value || undefined })}
        >
          <option value="">{t('audit.allActions')}</option>
          {actionFilters.map((action) => (
            <option key={action} value={action}>
              {t(`audit.filters.${action}`)}
            </option>
          ))}
        </select>
        <label className="input w-auto">
          <span className="text-base-content/60">{t('audit.from')}</span>
          <input type="date" value={params.get('from') ?? ''} onChange={(e) => update({ from: e.target.value || undefined })} />
        </label>
        <label className="input w-auto">
          <span className="text-base-content/60">{t('audit.to')}</span>
          <input type="date" value={params.get('to') ?? ''} onChange={(e) => update({ to: e.target.value || undefined })} />
        </label>
      </div>

      <div className="panel overflow-hidden">
        {error ? (
          <div className="p-4">
            <ErrorAlert error={error} />
          </div>
        ) : isPending ? (
          <span className="loading loading-spinner m-4" aria-label={t('loading')} />
        ) : data.items.length === 0 ? (
          <p className="py-10 text-center text-base-content/60">{filtered ? t('audit.noMatch') : t('audit.empty')}</p>
        ) : (
          <>
            <div className={`overflow-x-auto ${isPlaceholderData ? 'opacity-60' : ''}`}>
              <table className="table table-dense">
                <thead>
                  <tr>
                    <th className="w-40">{t('audit.columns.when')}</th>
                    <th className="w-44">{t('audit.columns.actor')}</th>
                    <th>{t('audit.columns.event')}</th>
                    <th>{t('audit.columns.detail')}</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((event) => {
                    const { what, detail } = describeAuditEvent(event, t)
                    return (
                      <tr key={event.id} className="align-top hover:bg-base-200/50">
                        <td className="data whitespace-nowrap text-base-content/70">
                          {formatWhen(event.occurredAt, i18n.language, t('audit.yesterday'))}
                        </td>
                        <td className="font-medium">{event.actorName}</td>
                        <td>
                          {what}{' '}
                          {event.tenantId && event.targetType === 'tenant' ? (
                            <Link to={tenantPath(event.targetId)} className="font-semibold hover:underline">
                              {event.targetName}
                            </Link>
                          ) : (
                            <b className="font-semibold">{event.targetName}</b>
                          )}
                        </td>
                        <td className="text-[12px] text-base-content/70">{detail}</td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
            <Pagination
              page={page}
              pageSize={pageSize}
              totalCount={data.totalCount}
              pageSizes={pageSizes}
              onPageChange={(next) => update({ page: String(next) })}
              onPageSizeChange={(size) => update({ pageSize: String(size) })}
            />
          </>
        )}
      </div>
    </>
  )
}
