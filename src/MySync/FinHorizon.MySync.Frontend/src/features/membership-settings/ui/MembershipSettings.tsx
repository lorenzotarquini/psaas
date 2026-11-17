import { useEffect } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCreditCard, faRotateRight, faTriangleExclamation } from '@fortawesome/free-solid-svg-icons'
import { goTo, membershipQueryKey, openBillingPortal, refreshMembership, startCheckout, useMembership, type Membership } from '@/entities/membership'
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
  const day = (value: string) => format.date(value.slice(0, 10))

  const refresh = useMutation({
    mutationFn: refreshMembership,
    onSuccess: (fresh) => queryClient.setQueryData(membershipQueryKey, fresh),
  })
  const checkout = useMutation({ mutationFn: startCheckout, onSuccess: ({ url }) => goTo.url(url) })
  const portal = useMutation({ mutationFn: openBillingPortal, onSuccess: ({ url }) => goTo.url(url) })

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
  const busy = checkout.isPending || portal.isPending
  const actionFailed = checkout.isError || portal.isError

  return (
    <section className="surface flex flex-col gap-4 p-5" aria-labelledby="settings-membership">
      <header className="flex flex-wrap items-center justify-between gap-2">
        <h2 id="settings-membership" className="surface-title">
          {t('membership.title')}
        </h2>
        <span className={`badge ${badge[data.status]}`}>{t(`membership.status.${data.status}`)}</span>
      </header>

      <dl className="grid gap-x-6 gap-y-3 sm:grid-cols-3">
        <div>
          <dt className="muted text-xs">{t('membership.price')}</dt>
          <dd className="font-medium">{t('membership.perMonth', { amount: format.money(data.price, data.currency) })}</dd>
        </div>
        <div>
          <dt className="muted text-xs">{t(data.cancelAtPeriodEnd ? 'membership.endsOn' : 'membership.renewsOn')}</dt>
          <dd className="font-medium">{data.status !== 'Expired' && data.currentPeriodEnd ? day(data.currentPeriodEnd) : '—'}</dd>
        </div>
        <div>
          <dt className="muted text-xs">{t('membership.paymentMethod')}</dt>
          <dd className="font-medium">
            {data.card
              ? t('membership.card', { brand: data.card.brand.toUpperCase(), last4: data.card.last4, month: String(data.card.expMonth).padStart(2, '0'), year: data.card.expYear })
              : t('membership.noCard')}
          </dd>
        </div>
      </dl>

      {data.status === 'Expired' && (
        <p role="status" className="alert alert-error alert-soft">
          {t('membership.expiredHelp')}
        </p>
      )}

      {failure && data.status !== 'Active' && (
        <p role="alert" className="alert alert-warning alert-soft">
          <FontAwesomeIcon icon={faTriangleExclamation} />
          {t('membership.failed', {
            date: day(failure.failedAt),
            reason: t(`membership.failures.${failure.code && knownFailures.includes(failure.code) ? failure.code : 'other'}`),
          })}
        </p>
      )}

      {refresh.isPending && <p className="muted">{t('membership.checking')}</p>}
      {actionFailed && (
        <p role="alert" className="alert alert-error alert-soft">
          {t('membership.unavailable')}
        </p>
      )}

      <div className="flex flex-wrap gap-2">
        {data.status === 'Expired' && (
          <button type="button" className="btn btn-primary btn-sm" disabled={busy} onClick={() => checkout.mutate()}>
            <FontAwesomeIcon icon={faRotateRight} /> {t('membership.renew')}
          </button>
        )}
        {data.status === 'PastDue' && data.payUrl && (
          <a className="btn btn-primary btn-sm" href={data.payUrl}>
            {t('membership.payNow')}
          </a>
        )}
        {data.hasCustomer && (
          <button type="button" className="btn btn-sm" disabled={busy} onClick={() => portal.mutate()}>
            <FontAwesomeIcon icon={faCreditCard} /> {t(data.status === 'PastDue' ? 'membership.updateMethod' : 'membership.manage')}
          </button>
        )}
      </div>
    </section>
  )
}
