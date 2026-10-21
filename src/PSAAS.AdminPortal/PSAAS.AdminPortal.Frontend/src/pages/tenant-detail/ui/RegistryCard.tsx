import { useTranslation } from 'react-i18next'
import type { TenantDetail } from '@/entities/tenant'

export function RegistryCard({ tenant }: { tenant: TenantDetail }) {
  const { t } = useTranslation()
  const { street, postalCode, city, country } = tenant.address

  return (
    <section className="card bg-base-100 shadow-sm">
      <div className="card-body">
        <h2 className="card-title text-base">{t('tenantDetail.registry')}</h2>
        <dl className="grid grid-cols-1 gap-x-6 gap-y-1 sm:grid-cols-[auto_1fr]">
          <dt className="font-medium">{t('tenants.companyName')}</dt>
          <dd>{tenant.companyName}</dd>
          <dt className="font-medium">{t('tenants.vatNumber')}</dt>
          <dd>{tenant.vatNumber}</dd>
          <dt className="font-medium">{t('tenants.completeVatNumber')}</dt>
          <dd>{tenant.completeVatNumber}</dd>
          <dt className="font-medium">{t('tenants.certifiedEmail')}</dt>
          <dd className="break-all">{tenant.certifiedEmail}</dd>
          <dt className="font-medium">{t('tenantDetail.address')}</dt>
          <dd>
            {street}, {postalCode} {city} ({country})
          </dd>
        </dl>
      </div>
    </section>
  )
}
