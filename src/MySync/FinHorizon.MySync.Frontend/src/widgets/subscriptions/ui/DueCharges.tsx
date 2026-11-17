import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { skipCharge, type Subscription } from '@/entities/subscription'
import { TransactionDialog, type TransactionPreset } from '@/features/edit-transaction'
import { invalidateLedger } from '@/shared/api'
import { useFormat } from '@/shared/lib/format'
import { Amount } from '@/shared/ui'

/**
 * UC16 steps 5–7 and 5a: the charges to register. "Registra" opens the expense of UC5 already filled, which the user saves;
 * "Salta" removes a charge that did not happen. Nothing is recorded without the user (D19).
 */
export function DueCharges({ items }: { items: Subscription[] }) {
  const { t } = useTranslation()
  const format = useFormat()
  const queryClient = useQueryClient()
  const [preset, setPreset] = useState<TransactionPreset | undefined>()
  const due = items.filter((item) => item.dueOn !== null)
  const skip = useMutation({
    mutationFn: (item: Subscription) => skipCharge(item.id, item.dueOn!),
    onSuccess: () => invalidateLedger(queryClient),
  })

  if (due.length === 0) return null

  return (
    <div className="flex flex-col gap-2">
      <h3 className="text-sm font-medium">{t('subscriptions.dueTitle')}</h3>
      <ul className="flex flex-col gap-2">
        {due.map((item) => (
          <li key={item.id} className="flex flex-wrap items-center justify-between gap-2 rounded-box bg-warning/10 px-3 py-2">
            <span>
              <span className="font-medium">{item.name}</span>{' '}
              <span className="muted text-xs">· {t('subscriptions.chargeOn', { date: format.date(item.dueOn!) })}</span>
            </span>
            <span className="flex items-center gap-2">
              <Amount value={item.amount} currency={item.currency} className="font-semibold" />
              <button
                type="button"
                className="btn btn-primary btn-xs"
                aria-label={t('subscriptions.registerAria', { name: item.name })}
                onClick={() =>
                  setPreset({
                    date: item.dueOn!,
                    amount: item.amount,
                    accountId: item.accountId,
                    category: { id: item.categoryId, kind: 'Expense', name: item.categoryName },
                    subscriptionId: item.id,
                  })
                }
              >
                {t('subscriptions.register')}
              </button>
              <button
                type="button"
                className="btn btn-ghost btn-xs"
                aria-label={t('subscriptions.skipAria', { name: item.name })}
                disabled={skip.isPending}
                onClick={() => skip.mutate(item)}
              >
                {t('subscriptions.skip')}
              </button>
            </span>
          </li>
        ))}
      </ul>
      <TransactionDialog open={preset !== undefined} defaultKind="Expense" preset={preset} onClose={() => setPreset(undefined)} />
    </div>
  )
}
