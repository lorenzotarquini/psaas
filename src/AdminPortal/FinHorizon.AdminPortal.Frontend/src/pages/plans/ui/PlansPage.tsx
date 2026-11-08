import { faPen, faPlus } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { formatPrice, usePlans, type Plan } from '@/entities/catalog'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { PageHeader } from '@/shared/ui/page-header'
import { StatusBadge } from '@/shared/ui/status-badge'
import { PlanForm } from './PlanForm'

// UC16 (EXP-02): what is sold. The form opens above the table, for a new plan or the one being edited.
export function PlansPage() {
  const { t, i18n } = useTranslation()
  const { data: plans = [], error, isPending } = usePlans()
  const [editing, setEditing] = useState<Plan | 'new' | null>(null)
  const per = { Monthly: t('catalog.per.Monthly'), Yearly: t('catalog.per.Yearly') }

  return (
    <>
      <PageHeader
        title={t('nav.plans')}
        count={isPending ? undefined : plans.length}
        subtitle={t('catalog.plansSubtitle')}
        actions={
          <button type="button" className="btn btn-primary btn-sm" onClick={() => setEditing('new')}>
            <FontAwesomeIcon icon={faPlus} />
            {t('catalog.newPlan')}
          </button>
        }
      />
      {editing && (
        <PlanForm key={editing === 'new' ? 'new' : editing.id} plan={editing === 'new' ? null : editing} onClose={() => setEditing(null)} />
      )}
      <div className="panel overflow-hidden">
        {error ? (
          <div className="p-4">
            <ErrorAlert error={error} />
          </div>
        ) : isPending ? (
          <span className="loading loading-spinner m-4" aria-label={t('loading')} />
        ) : plans.length === 0 ? (
          <p className="py-10 text-center text-base-content/60">{t('catalog.noPlans')}</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="table table-dense">
              <thead>
                <tr>
                  <th>{t('catalog.plan')}</th>
                  <th>{t('catalog.price')}</th>
                  <th>{t('catalog.trial')}</th>
                  <th>{t('catalog.maxUsers')}</th>
                  <th>{t('catalog.maxProductiveUnits')}</th>
                  <th>{t('catalog.modules')}</th>
                  <th className="text-right">{t('catalog.subscribers')}</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {plans.map((plan) => (
                  <tr key={plan.id} className="hover:bg-base-200/60">
                    <td>
                      <span className="font-semibold">{plan.name}</span>
                      {!plan.active && (
                        <span className="ml-2">
                          <StatusBadge tone="neutral">{t('catalog.archived')}</StatusBadge>
                        </span>
                      )}
                      <span className="data block text-[11px] text-base-content/50">{plan.code}</span>
                    </td>
                    <td className="data">{formatPrice(plan.price, plan.billingPeriod, i18n.language, per)}</td>
                    <td>{plan.trialDays ? t('catalog.trialDays', { count: plan.trialDays }) : '–'}</td>
                    <td className="data">{plan.maxUsers ?? '∞'}</td>
                    <td className="data">{plan.maxProductiveUnits ?? '∞'}</td>
                    <td className="data text-[12px]">{plan.moduleIds.join(', ')}</td>
                    <td className="data text-right">{plan.subscriberCount}</td>
                    <td className="text-right">
                      <button type="button" className="btn btn-ghost btn-xs" onClick={() => setEditing(plan)} aria-label={t('catalog.editPlan', { name: plan.name })}>
                        <FontAwesomeIcon icon={faPen} />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </>
  )
}
