import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { useAccounts, useTags } from '@/entities/account'
import { CurrencySelect, usePreferences } from '@/entities/currency'
import { createDebt, debtKinds, updateDebt, type Debt } from '@/entities/debt'
import { invalidateLedger } from '@/shared/api'
import { amountField, amountText, applyServerErrors, parseAmount, sameFields, useErrorText } from '@/shared/lib/form'
import { Field } from '@/shared/ui'

// UC3-R1..R3, with the backend's error codes.
const schema = z.object({
  kind: z.enum(debtKinds, 'Required'),
  name: z.string().trim().min(1, 'Required').max(50, 'MaxLengthExceeded'),
  description: z.string().trim().max(250, 'MaxLengthExceeded'),
  tag: z.string().trim().max(30, 'MaxLengthExceeded'),
  accountId: z.string().min(1, 'Required'),
  currency: z.string().min(1, 'Required'),
  residual: amountField({ positive: true }),
  // UC3-R8: optional; when given, a positive amount in cents.
  monthlyPayment: z.union([z.literal(''), amountField({ positive: true })]),
})

type Values = z.infer<typeof schema>
const fields = sameFields<Values>('kind', 'name', 'description', 'tag', 'accountId', 'currency', 'residual', 'monthlyPayment')

interface DebtFormProps {
  /** The debt to edit (UC10); a new one when missing (UC3). */
  debt?: Debt
  onSaved?: (debt: Debt) => void
}

/** UC3 and UC10: creates a debt, linked to the account its installments usually come from, or edits one. */
export function DebtForm({ debt, onSaved }: DebtFormProps) {
  const { t } = useTranslation()
  const errorText = useErrorText()
  const queryClient = useQueryClient()
  const accounts = useAccounts()
  const tags = useTags()
  const displayCurrency = usePreferences().data?.displayCurrency ?? 'EUR'
  const [formErrors, setFormErrors] = useState<string[]>([])
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: debt
      ? {
          kind: debt.kind,
          name: debt.name,
          description: debt.description ?? '',
          tag: debt.tag ?? '',
          accountId: debt.accountId,
          currency: debt.currency,
          residual: amountText(debt.initialResidual),
          monthlyPayment: debt.monthlyPayment ? amountText(debt.monthlyPayment) : '',
        }
      : { kind: 'Loan', name: '', description: '', tag: '', accountId: '', currency: displayCurrency, residual: '', monthlyPayment: '' },
  })
  const { errors } = form.formState
  const accountId = form.watch('accountId')

  // UC13-R8: installments leave from an account in the debt currency, so a new debt proposes the currency of its account.
  useEffect(() => {
    const linked = accounts.data?.items.find((account) => account.id === accountId)
    if (!debt && linked && linked.kind !== 'VirtualCurrency') form.setValue('currency', linked.currency)
  }, [accountId, accounts.data, debt, form])

  const mutation = useMutation({
    mutationFn: (values: Values) => {
      const input = {
        kind: values.kind,
        name: values.name,
        description: values.description || undefined,
        tag: values.tag || undefined,
        accountId: values.accountId,
        currency: values.currency,
        residual: parseAmount(values.residual),
        monthlyPayment: values.monthlyPayment ? parseAmount(values.monthlyPayment) : undefined,
      }
      return debt ? updateDebt(debt.id, input) : createDebt(input)
    },
    onSuccess: async (saved) => {
      if (!debt) form.reset()
      setFormErrors([])
      await invalidateLedger(queryClient)
      onSaved?.(saved)
    },
    onError: (error) => setFormErrors(applyServerErrors(error, form.setError, fields)),
  })

  return (
    <form noValidate className="flex flex-col gap-1" onSubmit={form.handleSubmit((values) => mutation.mutate(values))}>
      {formErrors.map((code) => (
        <p key={code} role="alert" className="alert alert-warning alert-soft">
          {t(code === 'LimitReached' ? 'debts.limitReached' : 'errors.saveFailed')}
        </p>
      ))}
      <Field label={t('debts.fields.kind')} htmlFor="debt-kind" error={errorText(errors.kind?.message)}>
        <select id="debt-kind" className="select w-full" {...form.register('kind')}>
          {debtKinds.map((kind) => (
            <option key={kind} value={kind}>
              {t(`debts.kinds.${kind}`)}
            </option>
          ))}
        </select>
      </Field>
      <Field label={t('debts.fields.name')} htmlFor="debt-name" error={errorText(errors.name?.message)}>
        <input id="debt-name" className="input w-full" autoComplete="off" aria-invalid={!!errors.name} {...form.register('name')} />
      </Field>
      <Field label={t('debts.fields.account')} htmlFor="debt-account" hint={t('debts.accountHint')} error={errorText(errors.accountId?.message)}>
        <select id="debt-account" className="select w-full" aria-invalid={!!errors.accountId} {...form.register('accountId')}>
          <option value="">{t('actions.choose')}</option>
          {accounts.data?.items.map((account) => (
            <option key={account.id} value={account.id}>
              {account.name}
            </option>
          ))}
        </select>
      </Field>
      <Field label={t('debts.fields.currency')} htmlFor="debt-currency" error={errorText(errors.currency?.message)}>
        <CurrencySelect id="debt-currency" type="Fiat" aria-invalid={!!errors.currency} {...form.register('currency')} />
      </Field>
      <Field label={t(debt ? 'debts.fields.initialResidual' : 'debts.fields.residual')} htmlFor="debt-residual" error={errorText(errors.residual?.message)}>
        <label className="input w-full">
          <span className="muted">{form.watch('currency')}</span>
          <input id="debt-residual" inputMode="decimal" autoComplete="off" aria-invalid={!!errors.residual} {...form.register('residual')} />
        </label>
      </Field>
      <Field label={t('debts.fields.monthlyPayment')} htmlFor="debt-monthly-payment" hint={t('debts.monthlyPaymentHint')} error={errorText(errors.monthlyPayment?.message)}>
        <label className="input w-full">
          <span className="muted">{form.watch('currency')}</span>
          <input id="debt-monthly-payment" inputMode="decimal" autoComplete="off" aria-invalid={!!errors.monthlyPayment} {...form.register('monthlyPayment')} />
        </label>
      </Field>
      <Field label={t('fields.description')} htmlFor="debt-description" error={errorText(errors.description?.message)}>
        <input id="debt-description" className="input w-full" autoComplete="off" {...form.register('description')} />
      </Field>
      <Field label={t('fields.tag')} htmlFor="debt-tag" error={errorText(errors.tag?.message)}>
        <input id="debt-tag" className="input w-full" list="debt-tags" autoComplete="off" {...form.register('tag')} />
        <datalist id="debt-tags">
          {tags.data?.map((tag) => <option key={tag} value={tag} />)}
        </datalist>
      </Field>
      <button type="submit" className="btn btn-primary mt-2" disabled={mutation.isPending}>
        {mutation.isPending && <span className="loading loading-spinner loading-xs" />}
        {t(debt ? 'actions.save' : 'debts.create')}
      </button>
    </form>
  )
}
