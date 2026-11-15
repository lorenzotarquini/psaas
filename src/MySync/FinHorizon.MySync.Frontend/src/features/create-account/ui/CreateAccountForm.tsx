import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { z } from 'zod'
import { accountKinds, createAccount, useTags, type Account } from '@/entities/account'
import { invalidateLedger } from '@/shared/api'
import { amountField, applyServerErrors, parseAmount, sameFields, useErrorText } from '@/shared/lib/form'
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

/** UC2: a new account. Used by the first setup (UC1) and by the dashboard. */
export function CreateAccountForm({ onCreated }: { onCreated?: (account: Account) => void }) {
  const { t } = useTranslation()
  const errorText = useErrorText()
  const queryClient = useQueryClient()
  const tags = useTags()
  const [formErrors, setFormErrors] = useState<string[]>([])
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { kind: 'CurrentAccount', name: '', description: '', tag: '', initialBalance: '' },
  })
  const { errors } = form.formState

  const mutation = useMutation({
    mutationFn: (values: Values) =>
      createAccount({
        kind: values.kind,
        name: values.name,
        description: values.description || undefined,
        tag: values.tag || undefined,
        initialBalance: parseAmount(values.initialBalance),
      }),
    onSuccess: async (account) => {
      form.reset()
      setFormErrors([])
      await invalidateLedger(queryClient)
      onCreated?.(account)
    },
    onError: (error) => setFormErrors(applyServerErrors(error, form.setError, fields)),
  })

  return (
    <form noValidate className="flex flex-col gap-1" onSubmit={form.handleSubmit((values) => mutation.mutate(values))}>
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
      <button type="submit" className="btn btn-primary mt-2" disabled={mutation.isPending}>
        {mutation.isPending && <span className="loading loading-spinner loading-xs" />}
        {t('accounts.create')}
      </button>
    </form>
  )
}
