import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faBan, faPen, faRotateLeft, faTrash } from '@fortawesome/free-solid-svg-icons'
import {
  cancelSubscription,
  deleteSubscription,
  isCancelled,
  openCount,
  resumeSubscription,
  useSubscriptions,
  type Subscription,
} from '@/entities/subscription'
import { SubscriptionForm } from '@/features/subscription-form'
import { ApiError, invalidateLedger } from '@/shared/api'
import { todayIso } from '@/shared/lib/date'
import { useFormat } from '@/shared/lib/format'
import { useErrorText } from '@/shared/lib/form'
import { AddButton, Amount, EmptyState, Field, LoadError, Loading, Modal } from '@/shared/ui'
import { DueCharges } from '@/widgets/subscriptions'

type Dialog = { type: 'new' } | { type: 'edit' | 'cancel' | 'delete'; subscription: Subscription } | null

/** UC16: the subscriptions, the monthly total, the charges to register and every action on a subscription. */
export function SubscriptionsPage() {
  const { t } = useTranslation()
  const subscriptions = useSubscriptions()
  const [dialog, setDialog] = useState<Dialog>(null)
  const close = () => setDialog(null)

  if (subscriptions.isPending) return <Loading />
  if (subscriptions.isError) return <LoadError />
  const { items, monthlyTotal, currency, max } = subscriptions.data
  const open = items.filter((item) => !isCancelled(item))
  const cancelled = items.filter(isCancelled)

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">{t('subscriptions.title')}</h1>
          <p className="muted mt-1 text-sm">{t('subscriptions.intro')}</p>
        </div>
        {max > 0 && (
          <AddButton
            label={t('subscriptions.new')}
            limitReached={openCount(subscriptions.data) >= max}
            limitMessage={t('subscriptions.limitReached')}
            onClick={() => setDialog({ type: 'new' })}
          />
        )}
      </header>

      <section className="surface flex flex-col gap-4 p-5" aria-labelledby="subscriptions-active-title">
        <header className="flex flex-wrap items-baseline justify-between gap-2">
          <h2 id="subscriptions-active-title" className="surface-title">
            {t('subscriptions.active')} <span className="muted text-xs font-normal">{t('subscriptions.count', { count: open.length, max })}</span>
          </h2>
          <span className="flex items-baseline gap-2">
            <span className="muted text-sm">{t('subscriptions.monthlyTotal')}</span>
            <Amount value={monthlyTotal} currency={currency} className="text-lg font-semibold" />
          </span>
        </header>
        <DueCharges items={items} />
        {open.length === 0 ? (
          <EmptyState>{t('subscriptions.empty')}</EmptyState>
        ) : (
          <ul className="flex flex-col divide-y divide-base-200">
            {open.map((item) => (
              <SubscriptionRow key={item.id} item={item} onEdit={() => setDialog({ type: 'edit', subscription: item })} onCancel={() => setDialog({ type: 'cancel', subscription: item })} onDelete={() => setDialog({ type: 'delete', subscription: item })} />
            ))}
          </ul>
        )}
      </section>

      {cancelled.length > 0 && (
        <section className="surface flex flex-col gap-3 p-5" aria-labelledby="subscriptions-cancelled-title">
          <h2 id="subscriptions-cancelled-title" className="surface-title">
            {t('subscriptions.cancelled')}
          </h2>
          <ul className="flex flex-col divide-y divide-base-200">
            {cancelled.map((item) => (
              <SubscriptionRow key={item.id} item={item} onEdit={() => setDialog({ type: 'edit', subscription: item })} onCancel={() => setDialog({ type: 'cancel', subscription: item })} onDelete={() => setDialog({ type: 'delete', subscription: item })} />
            ))}
          </ul>
        </section>
      )}

      <Modal open={dialog?.type === 'new' || dialog?.type === 'edit'} title={t(dialog?.type === 'edit' ? 'subscriptions.editTitle' : 'subscriptions.new')} onClose={close}>
        {(dialog?.type === 'new' || dialog?.type === 'edit') && (
          <SubscriptionForm key={dialog.type === 'edit' ? dialog.subscription.id : 'new'} subscription={dialog.type === 'edit' ? dialog.subscription : undefined} onSaved={close} />
        )}
      </Modal>
      <Modal open={dialog?.type === 'cancel'} title={t('subscriptions.cancelTitle')} onClose={close}>
        {dialog?.type === 'cancel' && <CancelForm key={dialog.subscription.id} subscription={dialog.subscription} onDone={close} />}
      </Modal>
      <Modal open={dialog?.type === 'delete'} title={t('subscriptions.deleteTitle')} onClose={close}>
        {dialog?.type === 'delete' && <DeleteConfirm key={dialog.subscription.id} subscription={dialog.subscription} onDone={close} />}
      </Modal>
    </div>
  )
}

interface RowProps {
  item: Subscription
  onEdit: () => void
  onCancel: () => void
  onDelete: () => void
}

function SubscriptionRow({ item, onEdit, onCancel, onDelete }: RowProps) {
  const { t } = useTranslation()
  const format = useFormat()
  const queryClient = useQueryClient()
  const [limitReached, setLimitReached] = useState(false)
  const resume = useMutation({
    mutationFn: () => resumeSubscription(item.id),
    onSuccess: () => invalidateLedger(queryClient),
    onError: (error) => setLimitReached(error instanceof ApiError && error.fieldErrors.limit?.includes('LimitReached')),
  })

  return (
    <li className={`flex flex-wrap items-center justify-between gap-3 py-3 ${item.active ? '' : 'opacity-60'}`}>
      <div className="flex flex-col">
        <span className="font-medium">{item.name}</span>
        <span className="muted text-xs">
          {item.accountName} · {item.categoryName}
          {item.nextChargeOn && <> · {t('subscriptions.nextCharge', { date: format.date(item.nextChargeOn) })}</>}
          {item.endsOn && <> · {t('subscriptions.endsOn', { date: format.date(item.endsOn) })}</>}
        </span>
        {limitReached && (
          <span role="alert" className="text-xs text-warning">
            {t('subscriptions.limitReached')}
          </span>
        )}
      </div>
      <div className="flex items-center gap-1">
        <Amount value={item.amount} currency={item.currency} className="mr-2 font-semibold" />
        <button type="button" className="btn btn-ghost btn-sm btn-square" aria-label={t('subscriptions.editAria', { name: item.name })} onClick={onEdit}>
          <FontAwesomeIcon icon={faPen} />
        </button>
        {isCancelled(item) ? (
          <button type="button" className="btn btn-ghost btn-sm" aria-label={t('subscriptions.resumeAria', { name: item.name })} disabled={resume.isPending} onClick={() => resume.mutate()}>
            <FontAwesomeIcon icon={faRotateLeft} /> {t('subscriptions.resume')}
          </button>
        ) : (
          <button type="button" className="btn btn-ghost btn-sm" aria-label={t('subscriptions.cancelAria', { name: item.name })} onClick={onCancel}>
            <FontAwesomeIcon icon={faBan} /> {t('subscriptions.cancel')}
          </button>
        )}
        <button type="button" className="btn btn-ghost btn-sm btn-square text-error" aria-label={t('subscriptions.deleteAria', { name: item.name })} onClick={onDelete}>
          <FontAwesomeIcon icon={faTrash} />
        </button>
      </div>
    </li>
  )
}

/** UC16 flow A: the end date, today by default, also in the future (R13). */
function CancelForm({ subscription, onDone }: { subscription: Subscription; onDone: () => void }) {
  const { t } = useTranslation()
  const errorText = useErrorText()
  const queryClient = useQueryClient()
  const [endsOn, setEndsOn] = useState(todayIso())
  const [error, setError] = useState<string | undefined>()
  const mutation = useMutation({
    mutationFn: () => cancelSubscription(subscription.id, endsOn),
    onSuccess: async () => {
      await invalidateLedger(queryClient)
      onDone()
    },
    onError: (failure) => setError(failure instanceof ApiError ? (failure.fieldErrors.endsOn?.[0] ?? 'saveFailed') : 'saveFailed'),
  })

  return (
    <form
      noValidate
      className="flex flex-col gap-2"
      onSubmit={(event) => {
        event.preventDefault()
        if (!endsOn) setError('Required')
        else mutation.mutate()
      }}
    >
      <p className="text-sm">{t('subscriptions.cancelHint', { name: subscription.name })}</p>
      <Field label={t('subscriptions.fields.endsOn')} htmlFor="sub-ends-on" error={error === 'saveFailed' ? t('errors.saveFailed') : error === 'InvalidValue' ? t('subscriptions.endsOnInvalid') : errorText(error)}>
        <input id="sub-ends-on" type="date" className="input w-full" aria-invalid={!!error} value={endsOn} onChange={(event) => setEndsOn(event.target.value)} />
      </Field>
      <button type="submit" className="btn btn-warning mt-2" disabled={mutation.isPending}>
        {t('subscriptions.cancel')}
      </button>
    </form>
  )
}

/** UC16 flow E and R15: with linked expenses the subscription is cancelled, not deleted. */
function DeleteConfirm({ subscription, onDone }: { subscription: Subscription; onDone: () => void }) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [refused, setRefused] = useState(subscription.hasPayments)
  const mutation = useMutation({
    mutationFn: () => deleteSubscription(subscription.id),
    onSuccess: async () => {
      await invalidateLedger(queryClient)
      onDone()
    },
    onError: () => setRefused(true),
  })

  if (refused) {
    return (
      <div className="flex flex-col gap-3">
        <p role="alert" className="alert alert-warning alert-soft">
          {t('subscriptions.deleteRefused', { name: subscription.name })}
        </p>
        <button type="button" className="btn" onClick={onDone}>
          {t('actions.close')}
        </button>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-3">
      <p>{t('subscriptions.deleteConfirm', { name: subscription.name })}</p>
      <div className="flex justify-end gap-2">
        <button type="button" className="btn btn-ghost" onClick={onDone}>
          {t('actions.cancel')}
        </button>
        <button type="button" className="btn btn-error" disabled={mutation.isPending} onClick={() => mutation.mutate()}>
          {t('actions.delete')}
        </button>
      </div>
    </div>
  )
}
