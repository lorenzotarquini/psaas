import { useTranslation } from 'react-i18next'
import type { CategorizedKind, Transaction } from '@/entities/transaction'
import { Modal } from '@/shared/ui'
import { TransactionForm, type TransactionPreset } from './TransactionForm'

interface TransactionDialogProps {
  open: boolean
  onClose: () => void
  transaction?: Transaction
  defaultKind?: CategorizedKind
  /** UC16 step 6: an expense filled from a subscription charge. */
  preset?: TransactionPreset
}

export function TransactionDialog({ open, onClose, transaction, defaultKind, preset }: TransactionDialogProps) {
  const { t } = useTranslation()
  const title = transaction ? t('transactions.editTitle') : t(defaultKind === 'Income' ? 'transactions.newIncome' : 'transactions.newExpense')
  return (
    <Modal open={open} title={title} onClose={onClose}>
      <TransactionForm
        key={transaction?.id ?? preset?.subscriptionId ?? defaultKind}
        transaction={transaction}
        defaultKind={defaultKind}
        preset={preset}
        onDone={onClose}
      />
    </Modal>
  )
}
