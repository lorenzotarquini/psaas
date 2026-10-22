import { faBuilding, faPen } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams, useSearchParams } from 'react-router'
import { useTenant, type TenantDetail } from '@/entities/tenant'
import { ApiError } from '@/shared/api'
import { useBreadcrumb } from '@/shared/lib/breadcrumb'
import { routes, tenantPath, type TenantTab } from '@/shared/routes'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { Tabs } from '@/shared/ui/tabs'
import { OrganizationCard } from './OrganizationCard'
import { OrganizationTab } from './OrganizationTab'
import { RegistryCard } from './RegistryCard'
import { RegistryTab } from './RegistryTab'

const tabs: TenantTab[] = ['overview', 'registry', 'organization']

// UC2 (mock 1d): everything about a tenant, one tab per area; registry (UC4) and organization (UC5) edit in place.
export function TenantDetailPage() {
  const { t } = useTranslation()
  const { tenantId = '' } = useParams()
  const { data: tenant, error, isPending } = useTenant(tenantId)
  useBreadcrumb([{ label: t('nav.tenants'), to: routes.tenants }, { label: tenant?.companyName ?? '…' }])

  if (error) {
    return error instanceof ApiError && error.status === 404 ? (
      <div role="alert" className="alert alert-warning alert-soft">
        {t('tenantDetail.notFound')}
      </div>
    ) : (
      <ErrorAlert error={error} />
    )
  }
  if (isPending) return <span className="loading loading-spinner" aria-label={t('loading')} />
  return <TenantView tenant={tenant} />
}

function TenantView({ tenant }: { tenant: TenantDetail }) {
  const { t } = useTranslation()
  const [searchParams] = useSearchParams()
  const requested = searchParams.get('tab') as TenantTab | null
  const active = requested && tabs.includes(requested) ? requested : 'overview'

  const content: Record<string, ReactNode> = {
    overview: <OrganizationCard tenant={tenant} />,
    // key: a saved tenant replaces the form defaults.
    registry: <RegistryTab key={tenant.completeVatNumber + tenant.companyName} tenant={tenant} />,
    organization: <OrganizationTab tenant={tenant} />,
  }

  return (
    <>
      <div className="-mx-4 -mt-6 border-b border-base-300 bg-base-100 px-4 pt-5 md:-mx-7 md:px-7">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="flex items-start gap-3.5">
            <div className="grid size-11 place-items-center rounded-box bg-primary/10 text-lg text-primary">
              <FontAwesomeIcon icon={faBuilding} />
            </div>
            <div>
              <h1 className="text-[22px] font-semibold tracking-[-0.01em]">{tenant.companyName}</h1>
              <p className="data mt-0.5 text-base-content/60">{tenant.completeVatNumber}</p>
            </div>
          </div>
          <Link to={tenantPath(tenant.id, 'registry')} className="btn btn-sm">
            <FontAwesomeIcon icon={faPen} />
            {t('tenantDetail.editRegistry')}
          </Link>
        </div>
        <div className="mt-4">
          <Tabs
            label={t('tenantDetail.tabsLabel')}
            active={active}
            items={tabs.map((key) => ({ key, label: t(`tenantDetail.tabs.${key}`), to: tenantPath(tenant.id, key) }))}
          />
        </div>
      </div>
      <div className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_320px]">
        <div className="min-w-0">{content[active]}</div>
        <aside className="flex flex-col gap-4">
          <RegistryCard tenant={tenant} />
        </aside>
      </div>
    </>
  )
}
