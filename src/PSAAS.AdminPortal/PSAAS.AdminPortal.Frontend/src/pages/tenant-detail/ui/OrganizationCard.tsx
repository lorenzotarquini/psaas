import { faCheck } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import type { TenantDetail } from '@/entities/tenant'
import { buildAccessMatrix } from '../model/accessMatrix'

type NamedItem = { id: string; name: string; description: string | null }

function NameList({ title, items, empty }: { title: string; items: NamedItem[]; empty: string }) {
  return (
    <div>
      <h3 className="font-medium">{title}</h3>
      {items.length === 0 ? (
        <p className="text-sm text-base-content/70">{empty}</p>
      ) : (
        <ul className="list-disc pl-5">
          {items.map((item) => (
            <li key={item.id} title={item.description ?? undefined}>
              {item.name}
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

// UC2: profiles × productive units. A row without checks is a profile with no units (R1),
// a column without checks a unit no profile opens.
export function OrganizationCard({ tenant }: { tenant: TenantDetail }) {
  const { t } = useTranslation()
  const { profiles, productiveUnits } = tenant

  return (
    <section className="card bg-base-100 shadow-sm">
      <div className="card-body">
        <h2 className="card-title text-base">{t('tenantDetail.organization')}</h2>
        {profiles.length === 0 && productiveUnits.length === 0 ? (
          <p className="text-base-content/70">{t('tenantDetail.noOrganization')}</p>
        ) : profiles.length === 0 || productiveUnits.length === 0 ? (
          <div className="grid gap-4 sm:grid-cols-2">
            <NameList title={t('tenantDetail.profiles')} items={profiles} empty={t('tenantDetail.noProfiles')} />
            <NameList
              title={t('tenantDetail.productiveUnits')}
              items={productiveUnits}
              empty={t('tenantDetail.noProductiveUnits')}
            />
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="table">
              <caption className="pb-2 text-left text-sm text-base-content/70">
                {t('tenantDetail.matrixCaption')}
              </caption>
              <thead>
                <tr>
                  <th scope="col">{t('tenantDetail.profile')}</th>
                  {productiveUnits.map((unit) => (
                    <th key={unit.id} scope="col" className="text-center" title={unit.description ?? undefined}>
                      {unit.name}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {buildAccessMatrix(tenant).map(({ profile, opens }) => (
                  <tr key={profile.id} className="hover">
                    <th scope="row" title={profile.description ?? undefined}>
                      {profile.name}
                    </th>
                    {opens.map((open, index) => (
                      <td key={productiveUnits[index].id} className="text-center">
                        {open ? (
                          <FontAwesomeIcon icon={faCheck} className="text-success" title={t('tenantDetail.opens')} />
                        ) : (
                          <span className="text-base-content/30" aria-label={t('tenantDetail.doesNotOpen')}>
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
      </div>
    </section>
  )
}
