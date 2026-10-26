import { faMagnifyingGlass, faRotate } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { formatMoney, useBillingSummary, useRunBilling, type InvoiceStatus } from '@/entities/invoice'
import { InvoicePanel } from '@/features/invoice-panel'
import { useDebouncedSearch } from '@/shared/lib/debounce'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { PageHeader } from '@/shared/ui/page-header'
import { InvoicesTable } from '@/widgets/invoices-table'

const statuses: (InvoiceStatus | undefined)[] = [undefined, 'Issued', 'Overdue', 'Paid', 'Cancelled']

const positiveInt = (value: string | null, fallback: number) => {
  const number = Number(value)
  return Number.isInteger(number) && number >= 1 ? number : fallback
}

// UC23/UC24 (EXP-08, no mock: drawn with system 1a): KPIs, invoices by status, side panel of the open invoice.
export function BillingPage() {
  const { t, i18n } = useTranslation()
  const [params, setParams] = useSearchParams()
  const status = statuses.find((s) => s === params.get('status'))
  const query = { status, search: params.get('search') ?? '', page: positiveInt(params.get('page'), 1), pageSize: positiveInt(params.get('pageSize'), 50) }
  const selected = params.get('invoice')
  const { data: summary } = useBillingSummary()
  const run = useRunBilling()
  const money = (amount: number | undefined) => (amount === undefined ? '…' : formatMoney(amount, i18n.language))

  const navigate = (next: Record<string, string | number | undefined | null>, replace = false) => {
    const merged = new URLSearchParams(params)
    for (const [key, value] of Object.entries(next)) {
      if (value === undefined || value === null || value === '') merged.delete(key)
      else merged.set(key, String(value))
    }
    if (merged.get('page') === '1') merged.delete('page')
    setParams(merged, { replace })
  }
  const [searchText, setSearchText] = useDebouncedSearch(query.search, (search) => navigate({ search, page: 1 }, true))

  const kpis = [
    { key: 'toCollect', value: money(summary?.toCollect), tone: '' },
    { key: 'overdue', value: money(summary?.overdue), tone: summary?.overdueCount ? 'text-error' : '', note: summary && t('billing.kpi.overdueCount', { count: summary.overdueCount }) },
    { key: 'paidThisMonth', value: money(summary?.paidThisMonth), tone: 'text-success' },
    { key: 'issuedThisMonth', value: money(summary?.issuedThisMonth), tone: '' },
  ]

  return (
    <div className={`grid gap-4 ${selected ? 'xl:-mr-7 xl:grid-cols-[minmax(0,1fr)_420px]' : ''}`}>
      <div className="flex min-w-0 flex-col gap-4">
        <PageHeader
          title={t('nav.billing')}
          subtitle={t('billing.subtitle')}
          actions={
            <button type="button" className="btn btn-ghost btn-sm" disabled={run.isPending} onClick={() => run.mutate()} title={t('billing.runHint')}>
              <FontAwesomeIcon icon={faRotate} spin={run.isPending} />
              {t('billing.run')}
            </button>
          }
        />
        {run.data && (
          <div role="status" className="alert alert-info alert-soft">
            {t('billing.runResult', { ...run.data })}
          </div>
        )}
        {run.error && <ErrorAlert error={run.error} action="save" />}
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          {kpis.map((kpi) => (
            <div key={kpi.key} className="panel px-4 py-3">
              <div className="eyebrow text-base-content/55">{t(`billing.kpi.${kpi.key}`)}</div>
              <div className={`data mt-1 text-[22px] font-semibold ${kpi.tone}`}>{kpi.value}</div>
              {kpi.note && <div className="text-[12px] text-base-content/60">{kpi.note}</div>}
            </div>
          ))}
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <div role="tablist" className="tabs tabs-box tabs-sm">
            {statuses.map((s) => (
              <button key={s ?? 'all'} type="button" role="tab" className={`tab ${s === status ? 'tab-active' : ''}`} onClick={() => navigate({ status: s, page: 1 })}>
                {t(s ? `billing.status.${s}` : 'billing.all')}
              </button>
            ))}
          </div>
          <label className="input input-sm w-full max-w-xs">
            <FontAwesomeIcon icon={faMagnifyingGlass} className="text-base-content/40" />
            <input
              type="search"
              placeholder={t('billing.searchPlaceholder')}
              aria-label={t('billing.searchPlaceholder')}
              value={searchText}
              onChange={(e) => setSearchText(e.target.value)}
            />
          </label>
        </div>
        <div className="panel overflow-hidden">
          <InvoicesTable
            query={query}
            onQueryChange={(next) => navigate(next)}
            selectedId={selected}
            onSelect={(invoice) => navigate({ invoice })}
            emptyText={t('billing.empty')}
          />
        </div>
      </div>
      {selected && (
        <div className="relative xl:sticky xl:top-0 xl:-my-6 xl:h-[calc(100vh-56px)]">
          <InvoicePanel invoiceId={selected} onClose={() => navigate({ invoice: null })} onOpen={(invoice) => navigate({ invoice })} />
        </div>
      )}
    </div>
  )
}
