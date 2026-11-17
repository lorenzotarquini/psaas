import { useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { useAccounts } from '@/entities/account'
import { CategoryPicker, type Category } from '@/entities/category'
import { createSubscription, updateSubscription, type Subscription } from '@/entities/subscription'
import { invalidateLedger } from '@/shared/api'
import { todayIso } from '@/shared/lib/date'
import { amountField, amountText, applyServerErrors, parseAmount, useErrorText, type ServerFieldMap } from '@/shared/lib/form'
import { Field } from '@/shared/ui'

// UC16-R1..R3, with the backend's error codes.
const schema = z.object({
  name: z.string().trim().min(1, 'Required').max(50, 'MaxLengthExceeded'),
  description: z.string().trim().max(250, 'MaxLengthExceeded'),
  accountId: z.string().min(1, 'Required'),
  category: z.custom<Category | null>().refine((category) => Boolean(category), 'Required'),
  amount: amountField({ positive: true }),
  firstChargeOn: z.string().min(1, 'Required'),
})

type Values = z.infer<typeof schema>
const serverFields: ServerFieldMap<Values> = {
  name: 'name',
  description: 'description',
  accountId: 'accountId',
  categoryId: 'category',
  amount: 'amount',
  firstChargeOn: 'firstChargeOn',
}

interface SubscriptionFormProps {
  /** The subscription to edit (UC16 flow M); a new one when missing (step 3). */
  subscription?: Subscription
  onSaved?: (subscription: Subscription) => void
}

/** UC16 steps 3–4 and flow M: no movement is recorded, saldi and patrimonio do not change (R18). */
export function SubscriptionForm({ subscription, onSaved }: SubscriptionFormProps) {
  const { t } = useTranslation()
  const errorText = useErrorText()
  const queryClient = useQueryClient()
  const accounts = useAccounts()
  // UC16-R2: only accounts in a traditional currency.
  const accountOptions = accounts.data?.items.filter((account) => account.kind !== 'VirtualCurrency') ?? []
  const [formErrors, setFormErrors] = useState<string[]>([])
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: subscription
      ? {
          name: subscription.name,
          description: subscription.description ?? '',
          accountId: subscription.accountId,
          category: { id: subscription.categoryId, kind: 'Expense', name: subscription.categoryName, fixed: false },
          amount: amountText(subscription.amount),
          firstChargeOn: subscription.firstChargeOn,
        }
      : { name: '', description: '', accountId: '', category: null, amount: '', firstChargeOn: todayIso() },
  })
  const { errors } = form.formState
  const accountId = useWatch({ control: form.control, name: 'accountId' })
  const currency = accountOptions.find((account) => account.id === accountId)?.currency ?? subscription?.currency ?? ''

  const mutation = useMutation({
    mutationFn: (values: Values) => {
      const input = {
        name: values.name,
        description: values.description || undefined,
        accountId: values.accountId,
        categoryId: values.category!.id,
        amount: parseAmount(values.amount),
        firstChargeOn: values.firstChargeOn,
      }
      return subscription ? updateSubscription(subscription.id, input) : createSubscription(input)
    },
    onSuccess: async (saved) => {
      setFormErrors([])
      await invalidateLedger(queryClient)
      onSaved?.(saved)
    },
    onError: (error) => setFormErrors(applyServerErrors(error, form.setError, serverFields)),
  })

  return (
    <form noValidate className="flex flex-col gap-1" onSubmit={form.handleSubmit((values) => mutation.mutate(values))}>
      {formErrors.map((code) => (
        <p key={code} role="alert" className="alert alert-warning alert-soft">
          {t(code === 'LimitReached' ? 'subscriptions.limitReached' : 'errors.saveFailed')}
        </p>
      ))}
      <Field label={t('subscriptions.fields.name')} htmlFor="sub-name" error={errorText(errors.name?.message)}>
        <input id="sub-name" className="input w-full" autoComplete="off" aria-invalid={!!errors.name} {...form.register('name')} />
      </Field>
      <div className="grid gap-x-3 sm:grid-cols-2">
        <Field label={t('subscriptions.fields.amount')} htmlFor="sub-amount" error={errorText(errors.amount?.message)}>
          <label className="input w-full">
            <span className="muted">{currency}</span>
            <input id="sub-amount" inputMode="decimal" autoComplete="off" aria-invalid={!!errors.amount} {...form.register('amount')} />
          </label>
        </Field>
        <Field label={t('subscriptions.fields.firstChargeOn')} htmlFor="sub-first-charge" hint={t('subscriptions.firstChargeHint')} error={errorText(errors.firstChargeOn?.message)}>
          <input id="sub-first-charge" type="date" className="input w-full" aria-invalid={!!errors.firstChargeOn} {...form.register('firstChargeOn')} />
        </Field>
      </div>
      <Field label={t('subscriptions.fields.account')} htmlFor="sub-account" error={errorText(errors.accountId?.message)}>
        <select id="sub-account" className="select w-full" aria-invalid={!!errors.accountId} {...form.register('accountId')}>
          <option value="">{t('actions.choose')}</option>
          {accountOptions.map((account) => (
            <option key={account.id} value={account.id}>
              {account.name}
            </option>
          ))}
        </select>
      </Field>
      <Field label={t('subscriptions.fields.category')} htmlFor="sub-category" error={errorText(errors.category?.message)}>
        <Controller
          control={form.control}
          name="category"
          render={({ field }) => <CategoryPicker id="sub-category" kind="Expense" value={field.value} onChange={field.onChange} invalid={!!errors.category} />}
        />
      </Field>
      <Field label={t('fields.description')} htmlFor="sub-description" error={errorText(errors.description?.message)}>
        <input id="sub-description" className="input w-full" autoComplete="off" {...form.register('description')} />
      </Field>
      <button type="submit" className="btn btn-primary mt-2" disabled={mutation.isPending}>
        {mutation.isPending && <span className="loading loading-spinner loading-xs" />}
        {t(subscription ? 'actions.save' : 'subscriptions.create')}
      </button>
    </form>
  )
}
