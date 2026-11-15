import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faTrashCan } from '@fortawesome/free-regular-svg-icons'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { deleteTransaction, isTransfer, signedAmount, type Transaction } from '@/entities/transaction'
import { invalidateLedger } from '@/shared/api'
import { useFormat } from '@/shared/lib/format'
import { Amount, Modal } from '@/shared/ui'

/** UC8 "Eliminare": asks for confirmation with date, amount and account, and says it cannot be undone (R4). */
export function DeleteTransactionButton({ transaction }: { transaction: Transaction }) {
  const { t } = useTranslation()
  const format = useFormat()
  const queryClient = useQueryClient()
  const [open, setOpen] = useState(false)
  const mutation = useMutation({
    mutationFn: () => deleteTransaction(transaction.id),
    onSuccess: async () => {
      setOpen(false)
      await invalidateLedger(queryClient)
    },
  })

  return (
    <>
      <button type="button" className="btn btn-ghost btn-sm btn-square" aria-label={t('actions.delete')} onClick={() => setOpen(true)}>
        <FontAwesomeIcon icon={faTrashCan} />
      </button>
      <Modal open={open} title={t('transactions.deleteTitle')} onClose={() => setOpen(false)}>
        <dl className="mb-4 grid grid-cols-[auto_1fr] gap-x-4 gap-y-1">
          <dt className="muted">{t('transactions.fields.date')}</dt>
          <dd>{format.date(transaction.date)}</dd>
          <dt className="muted">{t('transactions.fields.amount')}</dt>
          <dd>
            {isTransfer(transaction) ? <Amount value={transaction.amount} /> : <Amount value={signedAmount(transaction)} signed />}
          </dd>
          <dt className="muted">{t('transactions.fields.account')}</dt>
          <dd>{isTransfer(transaction) ? t('transfer.route', { from: transaction.accountName, to: transaction.counterAccountName }) : transaction.accountName}</dd>
        </dl>
        <p role="alert" className="alert alert-warning alert-soft mb-4">
          {t('transactions.deleteWarning')}
        </p>
        {mutation.isError && (
          <p role="alert" className="alert alert-error alert-soft mb-4">
            {t('errors.saveFailed')}
          </p>
        )}
        <div className="flex justify-end gap-2">
          <button type="button" className="btn" onClick={() => setOpen(false)}>
            {t('actions.cancel')}
          </button>
          <button type="button" className="btn btn-error" disabled={mutation.isPending} onClick={() => mutation.mutate()}>
            {t('actions.delete')}
          </button>
        </div>
      </Modal>
    </>
  )
}
