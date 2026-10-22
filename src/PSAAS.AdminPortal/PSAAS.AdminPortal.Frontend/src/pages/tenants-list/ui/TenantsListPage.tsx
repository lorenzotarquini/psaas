import { faMagnifyingGlass, faPlus } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { useTenants, type TenantsQuery } from '@/entities/tenant'
import { useDebouncedSearch } from '@/shared/lib/debounce'
import { routes, tenantPath, type ListReturnState } from '@/shared/routes'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { Pagination } from '@/shared/ui/pagination'
import { pageSizes, readTenantsQuery, writeTenantsQuery } from '../model/searchParams'

// UC1: tenants ordered by company name, paged, searchable by company name, VAT number and PEC.
export function TenantsListPage() {
  const { t } = useTranslation()
  const [searchParams, setSearchParams] = useSearchParams()
  const query = readTenantsQuery(searchParams)
  const { data, error, isPending, isPlaceholderData } = useTenants(query)
  const navigateTo = useNavigate()
  const returnState: ListReturnState = { listSearch: searchParams.toString() }

  const navigate = (next: Partial<TenantsQuery>, replace = false) =>
    setSearchParams(writeTenantsQuery({ ...query, ...next }), { replace })

  // A new search starts from page 1; replace keeps typing out of the browser history.
  const [searchText, setSearchText] = useDebouncedSearch(query.search, (search) => navigate({ search, page: 1 }, true))

  return (
    <div className="card bg-base-100 shadow-sm">
      <div className="card-body gap-4">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <h1 className="card-title">{t('tenants.title')}</h1>
          <Link to={routes.tenantNew} className="btn btn-primary btn-sm">
            <FontAwesomeIcon icon={faPlus} />
            {t('tenantForm.createTitle')}
          </Link>
        </div>

        <label className="input w-full max-w-md">
          <FontAwesomeIcon icon={faMagnifyingGlass} className="opacity-50" />
          <input
            type="search"
            placeholder={t('tenants.searchPlaceholder')}
            aria-label={t('tenants.searchPlaceholder')}
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
          />
        </label>

        {error ? (
          <ErrorAlert error={error} />
        ) : isPending ? (
          <span className="loading loading-spinner" aria-label={t('loading')} />
        ) : data.items.length === 0 ? (
          <p className="py-8 text-center text-base-content/70">
            {query.search ? t('tenants.noResults', { search: query.search }) : t('tenants.empty')}
          </p>
        ) : (
          <>
            <div className={`overflow-x-auto ${isPlaceholderData ? 'opacity-60' : ''}`}>
              <table className="table table-zebra">
                <thead>
                  <tr>
                    <th>{t('tenants.companyName')}</th>
                    <th>{t('tenants.vatNumber')}</th>
                    <th>{t('tenants.completeVatNumber')}</th>
                    <th>{t('tenants.certifiedEmail')}</th>
                    <th>{t('tenants.city')}</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((tenant) => (
                    // The whole row is clickable; the link keeps it reachable by keyboard.
                    <tr
                      key={tenant.id}
                      className="hover:bg-base-200 cursor-pointer"
                      onClick={() => navigateTo(tenantPath(tenant.id), { state: returnState })}
                    >
                      <td className="font-medium">
                        <Link
                          to={tenantPath(tenant.id)}
                          state={returnState}
                          className="link link-hover"
                          onClick={(e) => e.stopPropagation()}
                        >
                          {tenant.companyName}
                        </Link>
                      </td>
                      <td>{tenant.vatNumber}</td>
                      <td>{tenant.completeVatNumber}</td>
                      <td>{tenant.certifiedEmail}</td>
                      <td>{tenant.city}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pagination
              page={data.page}
              pageSize={data.pageSize}
              totalCount={data.totalCount}
              pageSizes={pageSizes}
              onPageChange={(page) => navigate({ page })}
              onPageSizeChange={(pageSize) => navigate({ pageSize, page: 1 })}
            />
          </>
        )}
      </div>
    </div>
  )
}
