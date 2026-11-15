import { useTranslation } from 'react-i18next'
import type { CategorizedKind, Transaction } from '@/entities/transaction'
import { Modal } from '@/shared/ui'
import { TransactionForm } from './TransactionForm'

interface TransactionDialogProps {
  open: boolean
  onClose: () => void
  transaction?: Transaction
  defaultKind?: CategorizedKind
}

export function TransactionDialog({ open, onClose, transaction, defaultKind }: TransactionDialogProps) {
  const { t } = useTranslation()
  const title = transaction ? t('transactions.editTitle') : t(defaultKind === 'Income' ? 'transactions.newIncome' : 'transactions.newExpense')
  return (
    <Modal open={open} title={title} onClose={onClose}>
      <TransactionForm key={transaction?.id ?? defaultKind} transaction={transaction} defaultKind={defaultKind} onDone={onClose} />
    </Modal>
  )
}
