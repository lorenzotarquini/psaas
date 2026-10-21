import { faArrowLeft, faPen, faSitemap } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { Link, useLocation, useParams } from 'react-router'
import { useTenant } from '@/entities/tenant'
import { ApiError } from '@/shared/api'
import { routes, tenantEditPath, tenantOrganizationPath, type ListReturnState } from '@/shared/routes'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { OrganizationCard } from './OrganizationCard'
import { RegistryCard } from './RegistryCard'

// UC2: everything about a tenant. A missing tenant (2a) gets its own message.
export function TenantDetailPage() {
  const { t } = useTranslation()
  const { tenantId = '' } = useParams()
  const listSearch = (useLocation().state as ListReturnState | null)?.listSearch
  const { data: tenant, error, isPending } = useTenant(tenantId)

  return (
    <div className="flex flex-col gap-4">
      <Link to={listSearch ? `${routes.tenants}?${listSearch}` : routes.tenants} className="link link-hover w-fit">
        <FontAwesomeIcon icon={faArrowLeft} className="mr-2" />
        {t('tenantDetail.backToList')}
      </Link>

      {error ? (
        error instanceof ApiError && error.status === 404 ? (
          <div role="alert" className="alert alert-warning">
            {t('tenantDetail.notFound')}
          </div>
        ) : (
          <ErrorAlert error={error} />
        )
      ) : isPending ? (
        <span className="loading loading-spinner" aria-label={t('loading')} />
      ) : (
        <>
          <div className="flex flex-wrap items-center justify-between gap-2">
            <h1 className="text-2xl font-semibold">{tenant.companyName}</h1>
            <div className="flex flex-wrap gap-2">
              <Link to={tenantEditPath(tenant.id)} className="btn btn-sm">
                <FontAwesomeIcon icon={faPen} />
                {t('tenantDetail.editRegistry')}
              </Link>
              <Link to={tenantOrganizationPath(tenant.id)} className="btn btn-sm">
                <FontAwesomeIcon icon={faSitemap} />
                {t('tenantDetail.editOrganization')}
              </Link>
            </div>
          </div>
          <RegistryCard tenant={tenant} />
          <OrganizationCard tenant={tenant} />
        </>
      )}
    </div>
  )
}
