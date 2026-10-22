import { faCheck } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import type { TenantDetail } from '@/entities/tenant'
import { tenantPath } from '@/shared/routes'
import { buildAccessMatrix } from '../model/accessMatrix'

// Overview tab (UC2): profiles × productive units, read only; editing happens in the Organization tab.
// A row without checks is a profile with no units (R1), a column without checks a unit no profile opens.
export function OrganizationCard({ tenant }: { tenant: TenantDetail }) {
  const { t } = useTranslation()
  const { profiles, productiveUnits } = tenant

  return (
    <section className="panel overflow-hidden">
      <header className="flex items-center justify-between border-b border-base-300 px-4 py-3">
        <h2 className="panel-title">{t('tenantDetail.organization')}</h2>
        <Link to={tenantPath(tenant.id, 'organization')} className="text-[12px] text-primary hover:underline">
          {t('tenantDetail.editOrganization')} →
        </Link>
      </header>
      {profiles.length === 0 && productiveUnits.length === 0 ? (
        <p className="px-4 py-8 text-center text-base-content/60">{t('tenantDetail.noOrganization')}</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="table table-dense">
            <caption className="sr-only">{t('tenantDetail.matrixCaption')}</caption>
            <thead>
              <tr>
                <th scope="col">{t('tenantDetail.profile')}</th>
                <th scope="col">{t('organizationEditor.users')}</th>
                {productiveUnits.map((unit) => (
                  <th key={unit.id} scope="col" className="text-center" title={unit.description ?? undefined}>
                    {unit.name}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {buildAccessMatrix(tenant).map(({ profile, opens }) => (
                <tr key={profile.id}>
                  <th scope="row" className="font-semibold">
                    {profile.name}
                    {profile.description && <div className="text-[12px] font-normal text-base-content/60">{profile.description}</div>}
                  </th>
                  <td className="data">{profile.userCount ?? '–'}</td>
                  {opens.map((open, index) => (
                    <td key={productiveUnits[index].id} className="text-center">
                      {open ? (
                        <FontAwesomeIcon icon={faCheck} className="text-primary" title={t('tenantDetail.opens')} />
                      ) : (
                        <span className="text-base-content/25" aria-label={t('tenantDetail.doesNotOpen')}>
                          —
                        </span>
                      )}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
