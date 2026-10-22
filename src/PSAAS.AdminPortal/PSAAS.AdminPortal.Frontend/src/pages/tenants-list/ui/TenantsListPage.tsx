import { faBullhorn, faDownload, faLayerGroup, faPlus } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router'
import { tenantsExportPath, useTenants, type TenantsQuery } from '@/entities/tenant'
import { apiDownload } from '@/shared/api'
import { useDebouncedSearch } from '@/shared/lib/debounce'
import { routes, type ListReturnState } from '@/shared/routes'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { PageHeader } from '@/shared/ui/page-header'
import { Pagination } from '@/shared/ui/pagination'
import {
  emptyFilters,
  filtersOf,
  optionalColumns,
  pageSizes,
  readTenantsQuery,
  statusTabs,
  writeTenantsQuery,
  type OptionalColumn,
} from '../model/searchParams'
import { ChangePlanDialog } from './ChangePlanDialog'
import { FilterBar } from './FilterBar'
import { ListTabs } from './ListTabs'
import { TenantsTable } from './TenantsTable'

const columnsKey = 'psaas-tenant-columns'

// Column choice is a per-browser convenience: storage may be unavailable (private mode), defaults then.
function readColumns(): OptionalColumn[] {
  try {
    const stored = JSON.parse(localStorage.getItem(columnsKey) ?? 'null') as OptionalColumn[] | null
    return stored?.filter((c) => (optionalColumns as readonly string[]).includes(c)) ?? [...optionalColumns]
  } catch {
    return [...optionalColumns]
  }
}

const tabFilters = Object.fromEntries(statusTabs.map((tab) => [tab.key, filtersOf({ ...emptyFilters, ...tab.filters, page: 1, pageSize: 20 })]))

// UC1 + UC21 + UC22 (mock 1c): tabs with counts and saved views, filters, sortable columns, selection with bulk actions.
export function TenantsListPage() {
  const { t } = useTranslation()
  const [searchParams, setSearchParams] = useSearchParams()
  const query = readTenantsQuery(searchParams)
  const { data, error, isPending, isPlaceholderData } = useTenants(query)
  const returnState: ListReturnState = { listSearch: searchParams.toString() }
  const [columns, setColumns] = useState(readColumns)
  const [selected, setSelected] = useState<string[]>([])
  const [changingPlan, setChangingPlan] = useState<{ id: string; companyName: string }[] | null>(null)
  const [exportError, setExportError] = useState<unknown>(null)

  const navigate = (next: Partial<TenantsQuery>, replace = false) =>
    setSearchParams(writeTenantsQuery({ ...query, ...next }), { replace })
  // A new search starts from page 1; replace keeps typing out of the browser history.
  const [searchText, setSearchText] = useDebouncedSearch(query.search, (search) => navigate({ search, page: 1 }, true))

  const saveColumns = (next: OptionalColumn[]) => {
    setColumns(next)
    try {
      localStorage.setItem(columnsKey, JSON.stringify(next))
    } catch {
      // Not remembered: fine.
    }
  }

  // "Esc per deselezionare" (mock 1c).
  useEffect(() => {
    if (selected.length === 0) return
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && setSelected([])
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [selected.length])

  const exportCsv = async (ids: string[] = []) => {
    setExportError(null)
    try {
      await apiDownload(tenantsExportPath(query, ids), 'clienti.csv')
    } catch (e) {
      setExportError(e)
    }
  }

  const selectedTenants = data?.items.filter((tenant) => selected.includes(tenant.id)) ?? []
  const filtered = filtersOf(query) !== ''

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        title={t('tenants.title')}
        count={data?.totalCount}
        actions={
          <>
            <button type="button" className="btn btn-ghost btn-sm" onClick={() => void exportCsv()}>
              <FontAwesomeIcon icon={faDownload} />
              {t('tenants.exportCsv')}
            </button>
            <Link to={routes.tenantNew} className="btn btn-primary btn-sm">
              <FontAwesomeIcon icon={faPlus} />
              {t('tenantForm.createTitle')}
            </Link>
          </>
        }
      />
      <ListTabs filters={filtersOf(query)} tabFilters={tabFilters} onApply={(filters) => setSearchParams(new URLSearchParams(filters))} />
      <FilterBar
        query={query}
        searchText={searchText}
        onSearchText={setSearchText}
        onChange={(next) => navigate({ ...next, page: 1 })}
        columns={columns}
        onColumns={saveColumns}
      />
      {exportError ? <ErrorAlert error={exportError} /> : null}

      <div className="panel overflow-hidden">
        {selected.length > 0 && (
          <div className="flex flex-wrap items-center gap-4 bg-neutral px-4 py-2.5 text-neutral-content" role="toolbar" aria-label={t('tenants.bulk.label')}>
            <span className="font-semibold text-white">{t('tenants.bulk.selected', { count: selected.length })}</span>
            <button type="button" className="flex items-center gap-1.5 hover:text-white" onClick={() => setChangingPlan(selectedTenants)} disabled={selectedTenants.length === 0}>
              <FontAwesomeIcon icon={faLayerGroup} />
              {t('tenants.bulk.changePlan')}
            </button>
            <button type="button" className="flex items-center gap-1.5 opacity-50" disabled title={t('tenants.bulk.communicationSoon')}>
              <FontAwesomeIcon icon={faBullhorn} />
              {t('tenants.bulk.communicate')}
            </button>
            <button type="button" className="flex items-center gap-1.5 hover:text-white" onClick={() => void exportCsv(selected)}>
              <FontAwesomeIcon icon={faDownload} />
              {t('tenants.bulk.export')}
            </button>
            <span className="ml-auto text-[12px] text-neutral-content/60">{t('tenants.bulk.escHint')}</span>
          </div>
        )}
        {error ? (
          <div className="p-4">
            <ErrorAlert error={error} />
          </div>
        ) : isPending ? (
          <span className="loading loading-spinner m-4" aria-label={t('loading')} />
        ) : data.items.length === 0 ? (
          <p className="py-10 text-center text-base-content/60">
            {query.search ? t('tenants.noResults', { search: query.search }) : filtered ? t('tenants.noMatch') : t('tenants.empty')}
          </p>
        ) : (
          <div className={isPlaceholderData ? 'opacity-60' : ''}>
            <TenantsTable
              tenants={data.items}
              columns={columns}
              sort={query.sort}
              onSort={(sort) => navigate({ sort, page: 1 })}
              selected={selected}
              onSelected={setSelected}
              returnState={returnState}
            />
            <Pagination
              page={data.page}
              pageSize={data.pageSize}
              totalCount={data.totalCount}
              pageSizes={pageSizes}
              onPageChange={(page) => navigate({ page })}
              onPageSizeChange={(pageSize) => navigate({ pageSize, page: 1 })}
            />
          </div>
        )}
      </div>
      <ChangePlanDialog tenants={changingPlan} onClose={() => setChangingPlan(null)} />
    </div>
  )
}
