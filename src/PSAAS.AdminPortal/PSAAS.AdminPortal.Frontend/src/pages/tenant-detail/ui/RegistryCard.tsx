import { useTranslation } from 'react-i18next'
import type { TenantDetail } from '@/entities/tenant'

// Side card of mock 1d: the registry at a glance, data in mono.
export function RegistryCard({ tenant }: { tenant: TenantDetail }) {
  const { t } = useTranslation()
  const { street, postalCode, city, country } = tenant.address

  return (
    <section className="panel px-4 py-3.5">
      <h2 className="panel-title mb-2.5">{t('tenantDetail.registry')}</h2>
      <dl className="grid grid-cols-[6.5rem_1fr] gap-x-3 gap-y-2">
        <dt className="text-base-content/60">{t('tenants.vatNumber')}</dt>
        <dd className="data">{tenant.completeVatNumber}</dd>
        <dt className="text-base-content/60">{t('tenants.certifiedEmail')}</dt>
        <dd className="break-all">{tenant.certifiedEmail}</dd>
        <dt className="text-base-content/60">{t('tenantDetail.address')}</dt>
        <dd>
          {street}, {postalCode} {city} ({country})
        </dd>
        <dt className="text-base-content/60">{t('tenantForm.sdiCode')}</dt>
        <dd className="data">{tenant.sdiCode ?? '–'}</dd>
        <dt className="text-base-content/60">{t('tenantForm.accountManager')}</dt>
        <dd>{tenant.accountManager ?? '–'}</dd>
      </dl>
    </section>
  )
}
