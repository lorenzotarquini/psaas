import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { ModuleIcon, ProvisioningBadge, type Subscription } from '@/entities/catalog'
import { tenantPath } from '@/shared/routes'

// Side card of mock 1d: the modules of the tenant with their provisioning status (UC18).
export function ModulesCard({ tenantId, subscription }: { tenantId: string; subscription: Subscription | null | undefined }) {
  const { t } = useTranslation()

  return (
    <section className="panel px-4 py-3.5">
      <div className="mb-2 flex items-center justify-between">
        <h2 className="panel-title">{t('catalog.activeModules')}</h2>
        <Link to={tenantPath(tenantId, 'subscription')} className="link link-primary link-hover text-[12px]">
          {t('catalog.manage')} →
        </Link>
      </div>
      {!subscription ? (
        <p className="text-base-content/60">{t('catalog.noSubscription')}</p>
      ) : (
        <ul className="divide-y divide-base-300">
          {subscription.modules.map((module) => (
            <li key={module.moduleId} className="flex items-center gap-2.5 py-2">
              <ModuleIcon icon={module.icon} />
              <div className="min-w-0 flex-1">
                <div className="font-semibold">{module.name}</div>
                <div className="data text-[11px] text-base-content/55">v{module.version}</div>
              </div>
              <ProvisioningBadge status={module.status} />
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
