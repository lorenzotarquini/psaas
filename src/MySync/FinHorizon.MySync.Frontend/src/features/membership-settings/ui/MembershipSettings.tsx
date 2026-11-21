import { useEffect, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faArrowUp, faCreditCard, faRotateRight, faTriangleExclamation } from '@fortawesome/free-solid-svg-icons'
import {
  changePlan,
  goTo,
  membershipQueryKey,
  openBillingPortal,
  refreshMembership,
  startCheckout,
  useMembership,
  useMembershipPlans,
  type Membership,
  type MembershipPlan,
  type Tier,
} from '@/entities/membership'
import { ApiError } from '@/shared/api'
import { useFormat } from '@/shared/lib/format'
import { LoadError, Loading } from '@/shared/ui'

/** Stripe's decline codes the user can act on; anything else gets the generic text. */
const knownFailures = ['card_declined', 'generic_decline', 'insufficient_funds', 'expired_card', 'incorrect_cvc', 'processing_error', 'authentication_required', 'lost_card', 'stolen_card']

const badge: Record<Membership['status'], string> = {
  Active: 'badge-success',
  PastDue: 'badge-warning',
  Expired: 'badge-error',
}

/**
 * UC17: the subscription to MySync in the settings. Payments, the card and invoices live on Stripe's pages;
 * back from them (?membership=…) the state is read from Stripe at once.
 */
export function MembershipSettings() {
  const { t } = useTranslation()
  const format = useFormat()
  const queryClient = useQueryClient()
  const membership = useMembership()
  const expired = membership.data?.status === 'Expired'
  const active = membership.data?.status === 'Active'
  const plans = useMembershipPlans(expired || active)
  const [chosen, setChosen] = useState<string | null>(null)
  const [upgradeTo, setUpgradeTo] = useState<string | null>(null)
  const day = (value: string) => format.date(value.slice(0, 10))

  const refresh = useMutation({
    mutationFn: refreshMembership,
    onSuccess: (fresh) => queryClient.setQueryData(membershipQueryKey, fresh),
  })
  const checkout = useMutation({ mutationFn: startCheckout, onSuccess: ({ url }) => goTo.url(url) })
  const price = (plan: { amount: number; currency: string; interval: 'Month' | 'Year' }) =>
    t(plan.interval === 'Year' ? 'membership.perYear' : 'membership.perMonth', { amount: format.money(plan.amount, plan.currency) })
  const portal = useMutation({ mutationFn: openBillingPortal, onSuccess: ({ url }) => goTo.url(url) })
  // UC17 flow 1b: the new tier applies at once; a refused payment leaves the plan as it was, with the failure (1b-a).
  const upgrade = useMutation({
    mutationFn: changePlan,
    onSuccess: (fresh) => {
      queryClient.setQueryData(membershipQueryKey, fresh)
      setUpgradeTo(null)
    },
    onSettled: () => void queryClient.invalidateQueries({ queryKey: membershipQueryKey }),
  })
  const upgradeRefused = upgrade.error instanceof ApiError && upgrade.error.fieldErrors.membership?.[0] === 'PaymentFailed'

  const { mutate: refreshNow } = refresh
  useEffect(() => {
    const params = new URLSearchParams(window.location.search)
    if (!params.has('membership')) return
    params.delete('membership')
    const query = params.toString()
    window.history.replaceState(null, '', `${window.location.pathname}${query ? `?${query}` : ''}`)
    refreshNow()
  }, [refreshNow])

  if (membership.isPending) return <Loading />
  if (membership.isError) return <LoadError />
  const data = membership.data
  const failure = data.lastFailure
  /** C2: what a tier gives, in words: its features, then its limits. */
  const tierSummary = (tier: Tier) =>
    [
      ...tier.features.map((feature) => t(`membership.features.${feature}`, { defaultValue: feature })),
      ...Object.entries(tier.limits).map(([key, max]) => t(`membership.limits.${key}`, { count: max, defaultValue: `${key}: ${max}` })),
    ].join(' · ')
  const busy = checkout.isPending || portal.isPending || upgrade.isPending
  const available = plans.data ?? []
  const selected = chosen ?? available[0]?.code ?? null
  const upgrades = active ? available.filter((plan) => plan.canUpgradeTo) : []
  const selectedUpgrade = upgradeTo ?? upgrades[0]?.code ?? null
  const actionFailed = checkout.isError || portal.isError || (upgrade.isError && !upgradeRefused)
  const failureReason = (code: string | null) => t(`membership.failures.${code && knownFailures.includes(code) ? code : 'other'}`)
  const planOptions = (options: MembershipPlan[], name: string, current: string | null, onSelect: (code: string) => void) =>
    options.map((plan) => (
      <label key={plan.code} className="flex cursor-pointer items-start gap-3 rounded-box border border-base-300 p-3 has-checked:border-primary">
        <input type="radio" name={name} className="radio radio-sm radio-primary mt-1" checked={current === plan.code} onChange={() => onSelect(plan.code)} />
        <span className="flex flex-col">
          <span className="font-medium">
            {plan.name} · {price({ amount: plan.price, currency: plan.currency, interval: plan.interval })}
          </span>
          {plan.tier && (
            <span className="text-xs">
              {plan.tier.name}: {tierSummary(plan.tier)}
            </span>
          )}
          <span className="muted text-xs">
            {[
              plan.vatRate !== null && t(plan.priceIncludesVat ? 'membership.vatIncluded' : 'membership.vatExcluded', { rate: plan.vatRate }),
              !data.hasCustomer && plan.trialDays > 0 && t('membership.trial', { count: plan.trialDays }),
              plan.description,
            ]
              .filter(Boolean)
              .join(' · ')}
          </span>
        </span>
      </label>
    ))

  const cardText = data.card
    ? t('membership.card', { brand: data.card.brand.toUpperCase(), last4: data.card.last4, month: String(data.card.expMonth).padStart(2, '0'), year: data.card.expYear })
    : t('membership.noCard')

  // Rows of equal height: what is paid beside how it is paid; below, across the page, what it can grow to.
  return (
    <div className="grid gap-6 lg:grid-cols-2">
      <section className={`surface flex flex-col gap-4 p-5 ${data.hasCustomer ? '' : 'lg:col-span-2'}`} aria-labelledby="settings-membership">
        <header className="flex flex-wrap items-center justify-between gap-2">
          <h2 id="settings-membership" className="surface-title">
            {t('membership.title')}
          </h2>
          <span className={`badge ${badge[data.status]}`}>{t(`membership.status.${data.status}`)}</span>
        </header>

        <dl className="grid gap-x-6 gap-y-3 sm:grid-cols-2">
          <div>
            <dt className="muted text-xs">{t('membership.tier')}</dt>
            <dd className="font-medium">{data.tier ? data.tier.name : '—'}</dd>
          </div>
          <div>
            <dt className="muted text-xs">{t('membership.plan')}</dt>
            <dd className="font-medium">{data.plan ? `${data.plan.name} · ${price(data.plan)}` : '—'}</dd>
          </div>
          <div>
            <dt className="muted text-xs">{t(data.cancelAtPeriodEnd ? 'membership.endsOn' : 'membership.renewsOn')}</dt>
            <dd className="font-medium">{data.status !== 'Expired' && data.currentPeriodEnd ? day(data.currentPeriodEnd) : '—'}</dd>
          </div>
        </dl>

        {data.tier && (
          <div>
            <p className="muted text-xs">{t('membership.includes')}</p>
            <p className="text-sm">{tierSummary(data.tier)}</p>
          </div>
        )}

        {data.status === 'Expired' && (
          <p role="status" className="alert alert-error alert-soft">
            {t('membership.expiredHelp')}
          </p>
        )}

        {refresh.isPending && <p className="muted">{t('membership.checking')}</p>}
        {actionFailed && (
          <p role="alert" className="alert alert-error alert-soft">
            {t('membership.unavailable')}
          </p>
        )}

        {expired && (
          <>
            <fieldset className="flex flex-col gap-2">
              <legend className="muted pb-1 text-xs">{t('membership.choosePlan')}</legend>
              {plans.isPending && <Loading />}
              {plans.isError && <LoadError />}
              {plans.isSuccess && available.length === 0 && <p className="muted">{t('membership.noPlans')}</p>}
              {planOptions(available, 'membership-plan', selected, setChosen)}
            </fieldset>
            <div>
              <button type="button" className="btn btn-primary btn-sm" disabled={busy || !selected} onClick={() => selected && checkout.mutate(selected)}>
                <FontAwesomeIcon icon={faRotateRight} /> {t(data.hasCustomer ? 'membership.renew' : 'membership.subscribe')}
              </button>
            </div>
          </>
        )}
      </section>

      {data.hasCustomer && (
        <section className="surface flex flex-col gap-4 p-5" aria-labelledby="settings-payment">
          <header>
            <h2 id="settings-payment" className="surface-title">
              {t('membership.paymentTitle')}
            </h2>
            <p className="muted text-sm">{t('membership.paymentHelp')}</p>
          </header>
          <dl>
            <dt className="muted text-xs">{t('membership.paymentMethod')}</dt>
            <dd className="font-medium">{cardText}</dd>
          </dl>
          {failure && data.status !== 'Active' && (
            <p role="alert" className="alert alert-warning alert-soft">
              <FontAwesomeIcon icon={faTriangleExclamation} />
              {t('membership.failed', { date: day(failure.failedAt), reason: failureReason(failure.code) })}
            </p>
          )}
          <div className="flex flex-wrap gap-2">
            {data.status === 'PastDue' && data.payUrl && (
              <a className="btn btn-primary btn-sm" href={data.payUrl}>
                {t('membership.payNow')}
              </a>
            )}
            <button type="button" className="btn btn-sm" disabled={busy} onClick={() => portal.mutate()}>
              <FontAwesomeIcon icon={faCreditCard} /> {t(data.status === 'PastDue' ? 'membership.updateMethod' : 'membership.manage')}
            </button>
          </div>
        </section>
      )}

      {upgrades.length > 0 && (
        <section className="surface flex flex-col gap-4 p-5 lg:col-span-2" aria-labelledby="settings-upgrade">
          <header>
            <h2 id="settings-upgrade" className="surface-title">
              {t('membership.upgradeTitle')}
            </h2>
            <p className="muted text-sm">{t('membership.upgradeHelp')}</p>
          </header>
          <fieldset className="grid gap-2 md:grid-cols-2" aria-label={t('membership.upgradeTitle')}>
            {planOptions(upgrades, 'membership-upgrade', selectedUpgrade, setUpgradeTo)}
          </fieldset>
          {upgradeRefused && (
            <p role="alert" className="alert alert-warning alert-soft">
              <FontAwesomeIcon icon={faTriangleExclamation} />
              {t('membership.upgradeRefused', { reason: failureReason(data.lastFailure?.code ?? null) })}
            </p>
          )}
          <div>
            <button type="button" className="btn btn-primary btn-sm" disabled={busy || !selectedUpgrade} onClick={() => selectedUpgrade && upgrade.mutate(selectedUpgrade)}>
              <FontAwesomeIcon icon={faArrowUp} /> {t('membership.upgrade')}
            </button>
          </div>
        </section>
      )}

    </div>
  )
}
