import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { formatPrice, SubscriptionStatusBadge, useSubscriptions } from '@/entities/catalog'
import { formatDate } from '@/shared/lib/format'
import { tenantPath } from '@/shared/routes'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { PageHeader } from '@/shared/ui/page-header'
import { StatusBadge } from '@/shared/ui/status-badge'

// UC17: every subscription at a glance; failed provisionings stand out (UC18).
export function SubscriptionsPage() {
  const { t, i18n } = useTranslation()
  const { data: subscriptions = [], error, isPending } = useSubscriptions()
  const per = { Monthly: t('catalog.per.Monthly'), Yearly: t('catalog.per.Yearly') }

  return (
    <>
      <PageHeader title={t('nav.subscriptions')} count={isPending ? undefined : subscriptions.length} subtitle={t('catalog.subscriptionsSubtitle')} />
      <div className="panel overflow-hidden">
        {error ? (
          <div className="p-4">
            <ErrorAlert error={error} />
          </div>
        ) : isPending ? (
          <span className="loading loading-spinner m-4" aria-label={t('loading')} />
        ) : subscriptions.length === 0 ? (
          <p className="py-10 text-center text-base-content/60">{t('catalog.noSubscriptions')}</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="table table-dense">
              <thead>
                <tr>
                  <th>{t('catalog.tenant')}</th>
                  <th>{t('catalog.plan')}</th>
                  <th>{t('catalog.status')}</th>
                  <th>{t('catalog.price')}</th>
                  <th>{t('catalog.renewal')}</th>
                  <th>{t('catalog.modules')}</th>
                </tr>
              </thead>
              <tbody>
                {subscriptions.map((s) => {
                  const failed = s.modules.filter((m) => m.status === 'Failed').length
                  return (
                    <tr key={s.tenantId} className="hover:bg-base-200/60">
                      <td>
                        <Link to={tenantPath(s.tenantId, 'subscription')} className="link link-hover font-semibold">
                          {s.tenantName}
                        </Link>
                      </td>
                      <td>{s.planName}</td>
                      <td>
                        <SubscriptionStatusBadge status={s.status} />
                      </td>
                      <td className="data">{formatPrice(s.price, s.billingPeriod, i18n.language, per)}</td>
                      <td className="data">{formatDate(s.trialEndsAt ?? s.renewsAt, i18n.language)}</td>
                      <td>
                        <span className="data">{s.modules.length}</span>
                        {failed > 0 && (
                          <span className="ml-2">
                            <StatusBadge tone="error">{t('catalog.failedCount', { count: failed })}</StatusBadge>
                          </span>
                        )}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </>
  )
}
