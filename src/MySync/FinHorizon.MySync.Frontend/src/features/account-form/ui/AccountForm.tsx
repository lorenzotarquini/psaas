import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { accountKinds, createAccount, updateAccount, useTags, type Account } from '@/entities/account'
import { invalidateLedger } from '@/shared/api'
import { amountField, amountText, applyServerErrors, parseAmount, sameFields, useErrorText } from '@/shared/lib/form'
import { Field } from '@/shared/ui'

// UC2-R1..R4, with the backend's error codes.
const schema = z.object({
  kind: z.enum(accountKinds, 'Required'),
  name: z.string().trim().min(1, 'Required').max(50, 'MaxLengthExceeded'),
  description: z.string().trim().max(250, 'MaxLengthExceeded'),
  tag: z.string().trim().max(30, 'MaxLengthExceeded'),
  initialBalance: amountField({ positive: false }),
})

type Values = z.infer<typeof schema>
const fields = sameFields<Values>('kind', 'name', 'description', 'tag', 'initialBalance')

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
  const [formErrors, setFormErrors] = useState<string[]>([])
  const [toConfirm, setToConfirm] = useState<Values | null>(null)
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: account
      ? { kind: account.kind, name: account.name, description: account.description ?? '', tag: account.tag ?? '', initialBalance: amountText(account.initialBalance) }
      : { kind: 'CurrentAccount', name: '', description: '', tag: '', initialBalance: '' },
  })
  const { errors } = form.formState

  const mutation = useMutation({
    mutationFn: (values: Values) => {
      const input = {
        kind: values.kind,
        name: values.name,
        description: values.description || undefined,
        tag: values.tag || undefined,
        initialBalance: parseAmount(values.initialBalance),
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
      <Field label={t('accounts.fields.initialBalance')} htmlFor="account-balance" hint={t('accounts.balanceHint')} error={errorText(errors.initialBalance?.message)}>
        <label className="input w-full">
          <span className="muted">€</span>
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
