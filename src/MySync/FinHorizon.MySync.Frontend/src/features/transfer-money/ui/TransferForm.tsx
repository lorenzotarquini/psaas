import { useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faArrowsUpDown } from '@fortawesome/free-solid-svg-icons'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { currencyTypeOf, useAccounts, type Account } from '@/entities/account'
import { decimalsOf } from '@/entities/currency'
import { amendTransaction, recordTransaction, type Transaction } from '@/entities/transaction'
import { invalidateLedger } from '@/shared/api'
import { todayIso } from '@/shared/lib/date'
import { amountField, amountText, applyServerErrors, fitsDecimals, parseAmount, useErrorText, type ServerFieldMap } from '@/shared/lib/form'
import { Field, LoadError, Loading } from '@/shared/ui'

const baseSchema = z.object({
  topAccountId: z.string().min(1, 'Required'),
  bottomAccountId: z.string().min(1, 'Required'),
  /** The amount of the top row. */
  amount: amountField({ positive: true, decimals: decimalsOf('Crypto') }),
  /** The amount of the bottom row, typed only between two currencies (UC13-R7). */
  otherAmount: z.string().trim(),
  date: z.string().min(1, 'Required'),
  note: z.string().trim().max(250, 'MaxLengthExceeded'),
})

type Values = z.infer<typeof baseSchema>

const decimalsOfAccount = (account: Account | undefined) => decimalsOf(currencyTypeOf(account?.kind ?? 'CurrentAccount'))

/** UC9-R2 and UC13: two different accounts, each amount with the decimals of its account currency. */
const schemaFor = (accounts: Account[]) =>
  baseSchema.superRefine((values, context) => {
    const top = accounts.find((account) => account.id === values.topAccountId)
    const bottom = accounts.find((account) => account.id === values.bottomAccountId)
    if (values.topAccountId === values.bottomAccountId) context.addIssue({ code: 'custom', path: ['bottomAccountId'], message: 'SameAccount' })
    if (values.amount && !fitsDecimals(values.amount, decimalsOfAccount(top))) context.addIssue({ code: 'custom', path: ['amount'], message: 'InvalidFormat' })
    if (!top || !bottom || top.currency === bottom.currency) return
    if (!values.otherAmount) context.addIssue({ code: 'custom', path: ['otherAmount'], message: 'Required' })
    else if (!fitsDecimals(values.otherAmount, decimalsOfAccount(bottom))) context.addIssue({ code: 'custom', path: ['otherAmount'], message: 'InvalidFormat' })
    else if (parseAmount(values.otherAmount) <= 0) context.addIssue({ code: 'custom', path: ['otherAmount'], message: 'InvalidValue' })
  })

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
    resolver: zodResolver(schemaFor(accounts)),
    defaultValues: {
      topAccountId: transaction?.accountId ?? accounts[0]?.id ?? '',
      bottomAccountId: transaction?.counterAccountId ?? accounts[1]?.id ?? '',
      amount: amountText(transaction?.amount),
      otherAmount: amountText(transaction?.counterAmount ?? undefined),
      date: transaction?.date ?? todayIso(),
      note: transaction?.note ?? '',
    },
  })
  const { errors } = form.formState
  const amount = useWatch({ control: form.control, name: 'amount' })
  const [topId, bottomId] = useWatch({ control: form.control, name: ['topAccountId', 'bottomAccountId'] })
  const topCurrency = accounts.find((account) => account.id === topId)?.currency
  const bottomCurrency = accounts.find((account) => account.id === bottomId)?.currency
  // UC13-R7: between two currencies the bottom row is typed too, it is not a copy of the top one.
  const twoCurrencies = !!topCurrency && !!bottomCurrency && topCurrency !== bottomCurrency
  const topSign = direction === 'topToBottom' ? '-' : '+'
  const bottomSign = direction === 'topToBottom' ? '+' : '-'
  const tone = (sign: string) => (sign === '-' ? 'text-error' : 'text-success')

  const mutation = useMutation({
    mutationFn: (values: Values) => {
      const [from, to] = direction === 'topToBottom' ? [values.topAccountId, values.bottomAccountId] : [values.bottomAccountId, values.topAccountId]
      const top = parseAmount(values.amount)
      const bottom = twoCurrencies ? parseAmount(values.otherAmount) : top
      const [sent, received] = direction === 'topToBottom' ? [top, bottom] : [bottom, top]
      const input = {
        kind: 'Transfer' as const,
        date: values.date,
        amount: sent,
        accountId: from,
        counterAccountId: to,
        counterAmount: twoCurrencies ? received : null,
        note: values.note || undefined,
      }
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
          ? { accountId: 'topAccountId', counterAccountId: 'bottomAccountId', amount: 'amount', counterAmount: 'otherAmount', date: 'date', note: 'note' }
          : { accountId: 'bottomAccountId', counterAccountId: 'topAccountId', amount: 'otherAmount', counterAmount: 'amount', date: 'date', note: 'note' }
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
              placeholder={twoCurrencies ? `${topSign}0 ${topCurrency}` : `${topSign}0`}
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
        {twoCurrencies ? (
          <Controller
            control={form.control}
            name="otherAmount"
            render={({ field }) => (
              <input
                aria-label={t(direction === 'topToBottom' ? 'transfer.amountIn' : 'transfer.amountOut')}
                inputMode="decimal"
                autoComplete="off"
                className={`input w-full text-right font-semibold ${tone(bottomSign)}`}
                placeholder={`${bottomSign}0 ${bottomCurrency}`}
                aria-invalid={!!errors.otherAmount}
                value={field.value ? `${bottomSign}${field.value}` : ''}
                onChange={(event) => field.onChange(digitsOnly(event.target.value))}
                onBlur={field.onBlur}
              />
            )}
          />
        ) : (
          <input
            readOnly
            tabIndex={-1}
            aria-label={t(direction === 'topToBottom' ? 'transfer.amountIn' : 'transfer.amountOut')}
            className={`input w-full text-right font-semibold ${tone(bottomSign)}`}
            placeholder={`${bottomSign}0`}
            value={amount ? `${bottomSign}${amount}` : ''}
          />
        )}
      </div>
      {twoCurrencies && <p className="muted text-xs">{t('transfer.twoCurrencies', { from: topCurrency, to: bottomCurrency })}</p>}
      {(errors.bottomAccountId || errors.amount || errors.otherAmount) && (
        <p role="alert" className="label text-error">
          {errorText(errors.bottomAccountId?.message ?? errors.amount?.message ?? errors.otherAmount?.message)}
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
