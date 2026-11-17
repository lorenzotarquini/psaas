import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useSubscriptions } from '@/entities/subscription'
import { Amount } from '@/shared/ui'
import { DueCharges } from './DueCharges'

/** UC16 step 5 on the dashboard (RF24): the monthly total and the charges to register. Hidden while subscriptions are off (R4). */
export function SubscriptionsCard() {
  const { t } = useTranslation()
  const subscriptions = useSubscriptions()
  const data = subscriptions.data
  if (!data || (data.max === 0 && data.items.length === 0)) return null

  return (
    <section className="surface flex flex-col gap-3 p-5" aria-labelledby="subscriptions-card-title">
      <header className="flex items-center justify-between">
        <h2 id="subscriptions-card-title" className="surface-title">
          {t('subscriptions.title')}
        </h2>
        <Link to="/subscriptions" className="link link-primary text-sm">
          {t('subscriptions.manage')}
        </Link>
      </header>
      <p className="flex items-baseline justify-between">
        <span className="muted text-sm">{t('subscriptions.monthlyTotal')}</span>
        <Amount value={data.monthlyTotal} currency={data.currency} className="text-lg font-semibold" />
      </p>
      <DueCharges items={data.items} />
    </section>
  )
}
