import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPenToSquare } from '@fortawesome/free-regular-svg-icons'
import { faArrowDown, faArrowUp, faRightLeft } from '@fortawesome/free-solid-svg-icons'
import { isTransfer, signedAmount, type Transaction } from '@/entities/transaction'
import { DeleteTransactionButton } from '@/features/delete-transaction'
import { TransactionDialog } from '@/features/edit-transaction'
import { TransferDialog } from '@/features/transfer-money'
import { useFormat } from '@/shared/lib/format'
import { Amount } from '@/shared/ui'

const kindStyle = {
  Expense: { icon: faArrowUp, tone: 'bg-error/10 text-error' },
  Income: { icon: faArrowDown, tone: 'bg-success/10 text-success' },
  Transfer: { icon: faRightLeft, tone: 'bg-info/10 text-info' },
} as const

/** Movements with their category (or route, for a transfer), account, debt and the edit/delete actions of UC8. */
export function TransactionTable({ items }: { items: Transaction[] }) {
  const { t } = useTranslation()
  const format = useFormat()
  const [editing, setEditing] = useState<Transaction | undefined>()

  return (
    <div className="overflow-x-auto">
      <table className="table">
        <thead>
          <tr>
            <th>{t('transactions.fields.category')}</th>
            <th>{t('transactions.fields.date')}</th>
            <th className="hidden md:table-cell">{t('transactions.fields.account')}</th>
            <th className="hidden lg:table-cell">{t('transactions.fields.note')}</th>
            <th className="text-right">{t('transactions.fields.amount')}</th>
            <th>
              <span className="sr-only">{t('actions.title')}</span>
            </th>
          </tr>
        </thead>
        <tbody>
          {items.map((transaction) => (
            <tr key={transaction.id} className="hover:bg-base-200/60">
              <td>
                <div className="flex items-center gap-3">
                  <span className={`grid size-8 shrink-0 place-items-center rounded-full ${kindStyle[transaction.kind].tone}`} aria-hidden>
                    <FontAwesomeIcon icon={kindStyle[transaction.kind].icon} />
                  </span>
                  <div>
                    <div className="font-medium">{isTransfer(transaction) ? t('transactions.kinds.Transfer') : transaction.categoryName}</div>
                    {isTransfer(transaction) && (
                      <div className="muted text-xs">{t('transfer.route', { from: transaction.accountName, to: transaction.counterAccountName })}</div>
                    )}
                    {transaction.debtName && <div className="badge badge-ghost badge-sm">{t('transactions.installmentOf', { name: transaction.debtName })}</div>}
                  </div>
                </div>
              </td>
              <td className="whitespace-nowrap">{format.date(transaction.date)}</td>
              <td className="hidden md:table-cell">
                {isTransfer(transaction) ? t('transfer.route', { from: transaction.accountName, to: transaction.counterAccountName }) : transaction.accountName}
              </td>
              <td className="muted hidden max-w-56 truncate lg:table-cell">{transaction.note}</td>
              <td className="text-right font-medium">
                {isTransfer(transaction) ? <Amount value={transaction.amount} /> : <Amount value={signedAmount(transaction)} signed />}
              </td>
              <td className="whitespace-nowrap text-right">
                <button type="button" className="btn btn-ghost btn-sm btn-square" aria-label={t('actions.edit')} onClick={() => setEditing(transaction)}>
                  <FontAwesomeIcon icon={faPenToSquare} />
                </button>
                <DeleteTransactionButton transaction={transaction} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <TransactionDialog open={editing !== undefined && !isTransfer(editing)} transaction={editing} onClose={() => setEditing(undefined)} />
      <TransferDialog open={editing !== undefined && isTransfer(editing)} transaction={editing} onClose={() => setEditing(undefined)} />
    </div>
  )
}
