import { useEffect, useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { accountKinds, createAccount, currencyTypeOf, updateAccount, useTags, type Account } from '@/entities/account'
import { CurrencySelect, decimalsOf, usePreferences } from '@/entities/currency'
import { invalidateLedger } from '@/shared/api'
import { amountField, amountText, applyServerErrors, fitsDecimals, parseAmount, sameFields, useErrorText } from '@/shared/lib/form'
import { Field } from '@/shared/ui'

// UC2-R1..R4 and UC13-R2 (the decimals of the currency), with the backend's error codes.
const schema = z
  .object({
    kind: z.enum(accountKinds, 'Required'),
    name: z.string().trim().min(1, 'Required').max(50, 'MaxLengthExceeded'),
    description: z.string().trim().max(250, 'MaxLengthExceeded'),
    tag: z.string().trim().max(30, 'MaxLengthExceeded'),
    currency: z.string().min(1, 'Required'),
    initialBalance: amountField({ positive: false, decimals: decimalsOf('Crypto') }),
  })
  .refine((values) => fitsDecimals(values.initialBalance, decimalsOf(currencyTypeOf(values.kind))), {
    message: 'InvalidFormat',
    path: ['initialBalance'],
  })

type Values = z.infer<typeof schema>
const fields = sameFields<Values>('kind', 'name', 'description', 'tag', 'currency', 'initialBalance')

/** The currency a new account of this type starts with: the display currency, or bitcoin for "Moneta virtuale". */
const defaultCurrency = (type: 'Fiat' | 'Crypto', displayCurrency: string) => (type === 'Crypto' ? 'BTC' : displayCurrency)

interface AccountFormProps {
  /** The account to edit (UC10); a new one when missing (UC2). */
  account?: Account
  onSaved?: (account: Account) => void
}

/**
 * UC2 and UC10: creates an account or edits one. Editing asks for confirmation first, because a new
 * initial balance recomputes today's net worth and the whole trend (UC10-R2).
 */
export function AccountForm({ account, onSaved }: AccountFormProps) {
  const { t } = useTranslation()
  const errorText = useErrorText()
  const queryClient = useQueryClient()
  const tags = useTags()
  const displayCurrency = usePreferences().data?.displayCurrency ?? 'EUR'
  const [formErrors, setFormErrors] = useState<string[]>([])
  const [toConfirm, setToConfirm] = useState<Values | null>(null)
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: account
      ? {
          kind: account.kind,
          name: account.name,
          description: account.description ?? '',
          tag: account.tag ?? '',
          currency: account.currency,
          initialBalance: amountText(account.initialBalance),
        }
      : { kind: 'CurrentAccount', name: '', description: '', tag: '', currency: displayCurrency, initialBalance: '' },
  })
  const { errors } = form.formState
  const currencyType = currencyTypeOf(form.watch('kind'))
  const currency = form.watch('currency')

  // UC13: switching between "Moneta virtuale" and the other kinds switches catalog, so the currency starts over.
  const previousType = useRef(currencyType)
  useEffect(() => {
    if (previousType.current === currencyType) return
    previousType.current = currencyType
    form.setValue('currency', account && currencyTypeOf(account.kind) === currencyType ? account.currency : defaultCurrency(currencyType, displayCurrency))
  }, [account, currencyType, displayCurrency, form])

  const mutation = useMutation({
    mutationFn: (values: Values) => {
      const input = {
        kind: values.kind,
        name: values.name,
        description: values.description || undefined,
        tag: values.tag || undefined,
        initialBalance: parseAmount(values.initialBalance),
        currency: values.currency,
      }
      return account ? updateAccount(account.id, input) : createAccount(input)
    },
    onSuccess: async (saved) => {
      if (!account) form.reset()
      setFormErrors([])
      setToConfirm(null)
      await invalidateLedger(queryClient)
      onSaved?.(saved)
    },
    onError: (error) => {
      setToConfirm(null)
      setFormErrors(applyServerErrors(error, form.setError, fields))
    },
  })

  return (
    <form noValidate className="flex flex-col gap-1" onSubmit={form.handleSubmit((values) => (account ? setToConfirm(values) : mutation.mutate(values)))}>
      {formErrors.map((code) => (
        <p key={code} role="alert" className="alert alert-warning alert-soft">
          {t(code === 'LimitReached' ? 'accounts.limitReached' : 'errors.saveFailed')}
        </p>
      ))}
      <Field label={t('accounts.fields.kind')} htmlFor="account-kind" error={errorText(errors.kind?.message)}>
        <select id="account-kind" className="select w-full" {...form.register('kind')}>
          {accountKinds.map((kind) => (
            <option key={kind} value={kind}>
              {t(`accounts.kinds.${kind}`)}
            </option>
          ))}
        </select>
      </Field>
      <Field label={t('accounts.fields.name')} htmlFor="account-name" error={errorText(errors.name?.message)}>
        <input id="account-name" className="input w-full" autoComplete="off" aria-invalid={!!errors.name} {...form.register('name')} />
      </Field>
      <Field label={t('accounts.fields.currency')} htmlFor="account-currency" hint={t('accounts.currencyHint')} error={errorText(errors.currency?.message)}>
        <CurrencySelect id="account-currency" type={currencyType} aria-invalid={!!errors.currency} {...form.register('currency')} />
      </Field>
      <Field label={t('accounts.fields.initialBalance')} htmlFor="account-balance" hint={t('accounts.balanceHint')} error={errorText(errors.initialBalance?.message)}>
        <label className="input w-full">
          <span className="muted">{currency}</span>
          <input id="account-balance" inputMode="decimal" autoComplete="off" aria-invalid={!!errors.initialBalance} {...form.register('initialBalance')} />
        </label>
      </Field>
      <Field label={t('fields.description')} htmlFor="account-description" error={errorText(errors.description?.message)}>
        <input id="account-description" className="input w-full" autoComplete="off" {...form.register('description')} />
      </Field>
      <Field label={t('fields.tag')} htmlFor="account-tag" error={errorText(errors.tag?.message)}>
        <input id="account-tag" className="input w-full" list="account-tags" autoComplete="off" {...form.register('tag')} />
        <datalist id="account-tags">
          {tags.data?.map((tag) => <option key={tag} value={tag} />)}
        </datalist>
      </Field>
      {toConfirm ? (
        <div role="alertdialog" aria-labelledby="account-confirm" className="alert alert-warning alert-soft mt-2 flex flex-col items-stretch gap-3">
          <p id="account-confirm">{t('accounts.confirmEdit')}</p>
          <div className="flex justify-end gap-2">
            <button type="button" className="btn btn-sm" onClick={() => setToConfirm(null)}>
              {t('actions.cancel')}
            </button>
            <button type="button" className="btn btn-sm btn-warning" disabled={mutation.isPending} onClick={() => mutation.mutate(toConfirm)}>
              {mutation.isPending && <span className="loading loading-spinner loading-xs" />}
              {t('actions.proceed')}
            </button>
          </div>
        </div>
      ) : (
        <button type="submit" className="btn btn-primary mt-2" disabled={mutation.isPending}>
          {mutation.isPending && <span className="loading loading-spinner loading-xs" />}
          {t(account ? 'actions.save' : 'accounts.create')}
        </button>
      )}
    </form>
  )
}
