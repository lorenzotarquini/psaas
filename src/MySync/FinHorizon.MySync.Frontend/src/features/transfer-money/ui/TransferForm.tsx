import { useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faArrowsUpDown } from '@fortawesome/free-solid-svg-icons'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { useAccounts, type Account } from '@/entities/account'
import { amendTransaction, recordTransaction, type Transaction } from '@/entities/transaction'
import { invalidateLedger } from '@/shared/api'
import { todayIso } from '@/shared/lib/date'
import { amountField, amountText, applyServerErrors, parseAmount, useErrorText, type ServerFieldMap } from '@/shared/lib/form'
import { Field, LoadError, Loading } from '@/shared/ui'

const schema = z
  .object({
    topAccountId: z.string().min(1, 'Required'),
    bottomAccountId: z.string().min(1, 'Required'),
    amount: amountField({ positive: true }),
    date: z.string().min(1, 'Required'),
    note: z.string().trim().max(250, 'MaxLengthExceeded'),
  })
  // UC9-R2: money moves between two different accounts.
  .refine((values) => values.topAccountId !== values.bottomAccountId, { path: ['bottomAccountId'], message: 'SameAccount' })

type Values = z.infer<typeof schema>

/** Which row the money leaves: the top one (shown as "-x") or the bottom one. */
type Direction = 'topToBottom' | 'bottomToTop'

interface TransferFormProps {
  /** The transfer to amend (UC8); a new one when missing. */
  transaction?: Transaction
  onDone: () => void
}

/** Keeps digits and one decimal separator: the sign is shown by the row, not typed. */
const digitsOnly = (text: string) => text.replace(/[^\d.,]/g, '')

/**
 * UC9: two rows, one per account. The top row starts as the source ("-0"), the bottom one as the destination ("+0");
 * the button between them swaps the direction, not the accounts.
 */
export function TransferForm(props: TransferFormProps) {
  const accounts = useAccounts()
  // The default rows are the first two accounts: the form starts only once they are known.
  if (accounts.isPending) return <Loading />
  if (accounts.isError) return <LoadError />
  return <TransferFields {...props} accounts={accounts.data.items} />
}

function TransferFields({ transaction, onDone, accounts }: TransferFormProps & { accounts: Account[] }) {
  const { t } = useTranslation()
  const errorText = useErrorText()
  const queryClient = useQueryClient()
  const [direction, setDirection] = useState<Direction>('topToBottom')
  const [formErrors, setFormErrors] = useState<string[]>([])
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      topAccountId: transaction?.accountId ?? accounts[0]?.id ?? '',
      bottomAccountId: transaction?.counterAccountId ?? accounts[1]?.id ?? '',
      amount: amountText(transaction?.amount),
      date: transaction?.date ?? todayIso(),
      note: transaction?.note ?? '',
    },
  })
  const { errors } = form.formState
  const amount = useWatch({ control: form.control, name: 'amount' })
  const topSign = direction === 'topToBottom' ? '-' : '+'
  const bottomSign = direction === 'topToBottom' ? '+' : '-'
  const tone = (sign: string) => (sign === '-' ? 'text-error' : 'text-success')

  const mutation = useMutation({
    mutationFn: (values: Values) => {
      const [from, to] = direction === 'topToBottom' ? [values.topAccountId, values.bottomAccountId] : [values.bottomAccountId, values.topAccountId]
      const input = { kind: 'Transfer' as const, date: values.date, amount: parseAmount(values.amount), accountId: from, counterAccountId: to, note: values.note || undefined }
      return transaction ? amendTransaction(transaction.id, input) : recordTransaction(input)
    },
    onSuccess: async () => {
      await invalidateLedger(queryClient)
      onDone()
    },
    onError: (error) => {
      // The backend speaks of source and destination; the form of top and bottom rows.
      const fields: ServerFieldMap<Values> =
        direction === 'topToBottom'
          ? { accountId: 'topAccountId', counterAccountId: 'bottomAccountId', amount: 'amount', date: 'date', note: 'note' }
          : { accountId: 'bottomAccountId', counterAccountId: 'topAccountId', amount: 'amount', date: 'date', note: 'note' }
      setFormErrors(applyServerErrors(error, form.setError, fields))
    },
  })

  return (
    <form noValidate className="flex flex-col gap-1" onSubmit={form.handleSubmit((values) => mutation.mutate(values))}>
      {formErrors.length > 0 && (
        <p role="alert" className="alert alert-error alert-soft">
          {t('errors.saveFailed')}
        </p>
      )}

      <div className="grid grid-cols-[minmax(0,1fr)_9rem] gap-2">
        <select className="select w-full" aria-label={t('transfer.topAccount')} aria-invalid={!!errors.topAccountId} {...form.register('topAccountId')}>
          {accounts.map((account) => (
            <option key={account.id} value={account.id}>
              {account.name}
            </option>
          ))}
        </select>
        <Controller
          control={form.control}
          name="amount"
          render={({ field }) => (
            <input
              aria-label={t(direction === 'topToBottom' ? 'transfer.amountOut' : 'transfer.amountIn')}
              inputMode="decimal"
              autoComplete="off"
              className={`input w-full text-right font-semibold ${tone(topSign)}`}
              placeholder={`${topSign}0`}
              aria-invalid={!!errors.amount}
              value={field.value ? `${topSign}${field.value}` : ''}
              onChange={(event) => field.onChange(digitsOnly(event.target.value))}
              onBlur={field.onBlur}
            />
          )}
        />
      </div>
      {errors.topAccountId && <p className="label text-error">{errorText(errors.topAccountId.message)}</p>}

      <button
        type="button"
        className="btn btn-circle btn-sm mx-auto my-1"
        aria-label={t('transfer.swap')}
        title={t('transfer.swap')}
        onClick={() => setDirection((current) => (current === 'topToBottom' ? 'bottomToTop' : 'topToBottom'))}
      >
        <FontAwesomeIcon icon={faArrowsUpDown} />
      </button>

      <div className="grid grid-cols-[minmax(0,1fr)_9rem] gap-2">
        <select className="select w-full" aria-label={t('transfer.bottomAccount')} aria-invalid={!!errors.bottomAccountId} {...form.register('bottomAccountId')}>
          {accounts.map((account) => (
            <option key={account.id} value={account.id}>
              {account.name}
            </option>
          ))}
        </select>
        <input
          readOnly
          tabIndex={-1}
          aria-label={t(direction === 'topToBottom' ? 'transfer.amountIn' : 'transfer.amountOut')}
          className={`input w-full text-right font-semibold ${tone(bottomSign)}`}
          placeholder={`${bottomSign}0`}
          value={amount ? `${bottomSign}${amount}` : ''}
        />
      </div>
      {(errors.bottomAccountId || errors.amount) && (
        <p role="alert" className="label text-error">
          {errorText(errors.bottomAccountId?.message ?? errors.amount?.message)}
        </p>
      )}

      <div className="mt-2 grid gap-x-3 sm:grid-cols-2">
        <Field label={t('transactions.fields.date')} htmlFor="transfer-date" error={errorText(errors.date?.message)}>
          <input id="transfer-date" type="date" className="input w-full" max={todayIso()} {...form.register('date')} />
        </Field>
        <Field label={t('transactions.fields.note')} htmlFor="transfer-note" error={errorText(errors.note?.message)}>
          <input id="transfer-note" className="input w-full" autoComplete="off" {...form.register('note')} />
        </Field>
      </div>

      <button type="submit" className="btn btn-primary mt-2" disabled={mutation.isPending}>
        {mutation.isPending && <span className="loading loading-spinner loading-xs" />}
        {t(transaction ? 'actions.save' : 'transfer.submit')}
      </button>
    </form>
  )
}
