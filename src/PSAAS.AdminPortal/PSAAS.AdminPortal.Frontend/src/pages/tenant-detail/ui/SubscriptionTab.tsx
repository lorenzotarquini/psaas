import { faRotateRight } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import {
  formatLimits,
  formatPrice,
  ModuleIcon,
  ProvisioningBadge,
  SubscriptionStatusBadge,
  useRetryProvisioning,
  type Subscription,
} from '@/entities/catalog'
import { SubscriptionEditor } from '@/features/subscription-editor'
import { formatDate } from '@/shared/lib/format'
import { ErrorAlert } from '@/shared/ui/error-alert'

// UC17/UC18 (mock 1d "Abbonamento e moduli"): what the tenant has, its modules, and the plan change.
export function SubscriptionTab({ tenantId, subscription }: { tenantId: string; subscription: Subscription | null }) {
  const { t, i18n } = useTranslation()
  const retry = useRetryProvisioning(tenantId)
  const per = { Monthly: t('catalog.per.Monthly'), Yearly: t('catalog.per.Yearly') }
  const date = (iso: string) => formatDate(iso, i18n.language)

  return (
    <div className="flex flex-col gap-4">
      {subscription && (
        <section className="panel overflow-hidden">
          <div className="flex flex-wrap items-start justify-between gap-3 px-5 py-4">
            <div>
              <h2 className="panel-title flex items-center gap-2">
                {subscription.planName}
                <SubscriptionStatusBadge status={subscription.status} />
              </h2>
              <p className="mt-0.5 text-base-content/60">
                {subscription.status === 'Trial' && subscription.trialEndsAt
                  ? t('catalog.trialUntil', { date: date(subscription.trialEndsAt) })
                  : t(subscription.autoRenew ? 'catalog.renewsOn' : 'catalog.endsOn', { date: date(subscription.renewsAt) })}
                {' · '}
                {t('catalog.since', { date: date(subscription.startsAt) })}
              </p>
            </div>
            <div className="text-right">
              <div className="data text-[17px]">{formatPrice(subscription.price, subscription.billingPeriod, i18n.language, per)}</div>
              <div className="text-[12px] text-base-content/60">
                {formatLimits(t, subscription.maxUsers, subscription.maxProductiveUnits)}
              </div>
            </div>
          </div>
          {retry.error && (
            <div className="px-5 pb-3">
              <ErrorAlert error={retry.error} action="save" />
            </div>
          )}
          <table className="table table-dense border-t border-base-300">
            <thead>
              <tr>
                <th>{t('catalog.module')}</th>
                <th>{t('catalog.version')}</th>
                <th>{t('catalog.status')}</th>
                <th>{t('catalog.attempts')}</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {subscription.modules.map((module) => (
                <tr key={module.moduleId}>
                  <td>
                    <span className="flex items-center gap-2.5">
                      <ModuleIcon icon={module.icon} />
                      <span>
                        <span className="font-semibold">{module.name}</span>
                        {module.isAddOn && <span className="badge badge-ghost badge-sm ml-2">{t('catalog.addOn')}</span>}
                        {module.lastError && <span className="block text-[12px] text-error">{module.lastError}</span>}
                      </span>
                    </span>
                  </td>
                  <td className="data">v{module.version}</td>
                  <td>
                    <ProvisioningBadge status={module.status} />
                  </td>
                  <td className="data">{module.attempts}</td>
                  <td className="text-right">
                    {module.status === 'Failed' && (
                      <button
                        type="button"
                        className="btn btn-xs"
                        disabled={retry.isPending}
                        onClick={() => retry.mutate(module.moduleId)}
                      >
                        <FontAwesomeIcon icon={faRotateRight} />
                        {t('catalog.retry')}
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}
      <section className="flex flex-col gap-3">
        <h2 className="panel-title">{t(subscription ? 'catalog.changePlan' : 'catalog.choosePlan')}</h2>
        <SubscriptionEditor key={subscription?.planId ?? 'none'} tenantId={tenantId} current={subscription} formId="tenant-subscription" onSaved={() => undefined} />
        <div>
          <button type="submit" form="tenant-subscription" className="btn btn-primary btn-sm">
            {t('catalog.saveSubscription')}
          </button>
        </div>
      </section>
    </div>
  )
}
