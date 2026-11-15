import { useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { useAccounts } from '@/entities/account'
import { CategoryPicker, type Category } from '@/entities/category'
import { useDebts } from '@/entities/debt'
import {
  amendTransaction,
  recordTransaction,
  categorizedKinds,
  type Transaction,
  type TransactionInput,
  type CategorizedKind,
} from '@/entities/transaction'
import { invalidateLedger } from '@/shared/api'
import { todayIso } from '@/shared/lib/date'
import { amountField, amountText, applyServerErrors, parseAmount, useErrorText, type ServerFieldMap } from '@/shared/lib/form'
import { Field } from '@/shared/ui'

// UC5-R1, R2 (the rules on stored data are the backend's).
const schema = z.object({
  kind: z.enum(categorizedKinds),
  date: z.string().min(1, 'Required'),
  amount: amountField({ positive: true }),
  accountId: z.string().min(1, 'Required'),
  category: z.custom<Category | null>().refine((category) => Boolean(category), 'Required'),
  debtId: z.string(),
  note: z.string().trim().max(250, 'MaxLengthExceeded'),
})

type Values = z.infer<typeof schema>
const serverFields: ServerFieldMap<Values> = {
  kind: 'kind',
  date: 'date',
  amount: 'amount',
  accountId: 'accountId',
  categoryId: 'category',
  debtId: 'debtId',
  note: 'note',
}

interface TransactionFormProps {
  /** The movement to amend (UC8); a new one when missing (UC5). */
  transaction?: Transaction
  defaultKind?: CategorizedKind
  onDone: () => void
}

function initialValues(transaction: Transaction | undefined, kind: CategorizedKind): Values {
  return transaction
    ? {
        kind: transaction.kind as CategorizedKind,
        date: transaction.date,
        amount: amountText(transaction.amount),
        accountId: transaction.accountId,
        category: transaction.categoryId ? { id: transaction.categoryId, kind: transaction.kind as CategorizedKind, name: transaction.categoryName ?? '' } : null,
        debtId: transaction.debtId ?? '',
        note: transaction.note ?? '',
      }
    : { kind, date: todayIso(), amount: '', accountId: '', category: null, debtId: '', note: '' }
}

/** UC5 and UC8: records a new movement or amends an existing one, with the same fields and rules. */
export function TransactionForm({ transaction, defaultKind = 'Expense', onDone }: TransactionFormProps) {
  const { t } = useTranslation()
  const errorText = useErrorText()
  const queryClient = useQueryClient()
  const accounts = useAccounts()
  const debts = useDebts()
  const [formErrors, setFormErrors] = useState<string[]>([])
  const form = useForm<Values>({ resolver: zodResolver(schema), defaultValues: initialValues(transaction, defaultKind) })
  const { errors } = form.formState
  const kind = useWatch({ control: form.control, name: 'kind' })
  const debtId = useWatch({ control: form.control, name: 'debtId' })
  // A paid-off debt stays selectable only for the movement that is already its installment (UC8-R3).
  const debtOptions = debts.data?.items.filter((debt) => !debt.paidOff || debt.id === transaction?.debtId) ?? []

  const mutation = useMutation({
    mutationFn: (values: Values) => {
      const input: TransactionInput = {
        kind: values.kind,
        date: values.date,
        amount: parseAmount(values.amount),
        accountId: values.accountId,
        categoryId: values.category!.id,
        debtId: values.kind === 'Expense' && values.debtId ? values.debtId : null,
        note: values.note || undefined,
      }
      return transaction ? amendTransaction(transaction.id, input) : recordTransaction(input)
    },
    onSuccess: async () => {
      await invalidateLedger(queryClient)
      onDone()
    },
    onError: (error) => setFormErrors(applyServerErrors(error, form.setError, serverFields)),
  })

  function changeKind(next: CategorizedKind) {
    form.setValue('kind', next)
    // Categories are per kind and only expenses pay debts (D5).
    form.setValue('category', null)
    form.setValue('debtId', '')
  }

  function changeDebt(nextDebtId: string) {
    form.setValue('debtId', nextDebtId)
    // D6: the debt's account is the default, the user may still change it.
    const debt = debts.data?.items.find((item) => item.id === nextDebtId)
    if (debt && !form.getValues('accountId')) form.setValue('accountId', debt.accountId)
  }

  return (
    <form noValidate className="flex flex-col gap-1" onSubmit={form.handleSubmit((values) => mutation.mutate(values))}>
      {formErrors.length > 0 && (
        <p role="alert" className="alert alert-error alert-soft">
          {t('errors.saveFailed')}
        </p>
      )}
      <div role="radiogroup" aria-label={t('transactions.fields.kind')} className="join w-full">
        {categorizedKinds.map((option) => (
          <button
            key={option}
            type="button"
            role="radio"
            aria-checked={kind === option}
            className={`btn join-item flex-1 ${kind === option ? (option === 'Expense' ? 'btn-error' : 'btn-success') : ''}`}
            onClick={() => changeKind(option)}
          >
            {t(`transactions.kinds.${option}`)}
          </button>
        ))}
      </div>
      <div className="grid gap-x-3 sm:grid-cols-2">
        <Field label={t('transactions.fields.amount')} htmlFor="tx-amount" error={errorText(errors.amount?.message)}>
          <label className="input w-full">
            <span className="muted">€</span>
            <input id="tx-amount" inputMode="decimal" autoComplete="off" aria-invalid={!!errors.amount} {...form.register('amount')} />
          </label>
        </Field>
        <Field label={t('transactions.fields.date')} htmlFor="tx-date" error={errorText(errors.date?.message)}>
          <input id="tx-date" type="date" className="input w-full" max={todayIso()} aria-invalid={!!errors.date} {...form.register('date')} />
        </Field>
      </div>
      <Field label={t('transactions.fields.account')} htmlFor="tx-account" error={errorText(errors.accountId?.message)}>
        <select id="tx-account" className="select w-full" aria-invalid={!!errors.accountId} {...form.register('accountId')}>
          <option value="">{t('actions.choose')}</option>
          {accounts.data?.items.map((account) => (
            <option key={account.id} value={account.id}>
              {account.name}
            </option>
          ))}
        </select>
      </Field>
      <Field label={t('transactions.fields.category')} htmlFor="tx-category" error={errorText(errors.category?.message)}>
        <Controller
          control={form.control}
          name="category"
          render={({ field }) => (
            <CategoryPicker key={kind} id="tx-category" kind={kind} value={field.value} onChange={field.onChange} invalid={!!errors.category} />
          )}
        />
      </Field>
      {kind === 'Expense' && debtOptions.length > 0 && (
        <Field label={t('transactions.fields.debt')} htmlFor="tx-debt" hint={t('transactions.debtHint')} error={errorText(errors.debtId?.message)}>
          <select id="tx-debt" className="select w-full" value={debtId} onChange={(event) => changeDebt(event.target.value)}>
            <option value="">{t('transactions.noDebt')}</option>
            {debtOptions.map((debt) => (
              <option key={debt.id} value={debt.id}>
                {debt.name}
              </option>
            ))}
          </select>
        </Field>
      )}
      <Field label={t('transactions.fields.note')} htmlFor="tx-note" error={errorText(errors.note?.message)}>
        <input id="tx-note" className="input w-full" autoComplete="off" {...form.register('note')} />
      </Field>
      <button type="submit" className="btn btn-primary mt-2" disabled={mutation.isPending}>
        {mutation.isPending && <span className="loading loading-spinner loading-xs" />}
        {t(transaction ? 'actions.save' : 'transactions.record')}
      </button>
    </form>
  )
}
