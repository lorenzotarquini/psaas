import { useTranslation } from 'react-i18next'
import type { Transaction } from '@/entities/transaction'
import { Modal } from '@/shared/ui'
import { TransferForm } from './TransferForm'

interface TransferDialogProps {
  open: boolean
  onClose: () => void
  /** The transfer to amend; a new one when missing. */
  transaction?: Transaction
}

export function TransferDialog({ open, onClose, transaction }: TransferDialogProps) {
  const { t } = useTranslation()
  return (
    <Modal open={open} title={t(transaction ? 'transfer.editTitle' : 'transfer.title')} onClose={onClose}>
      <TransferForm key={transaction?.id ?? 'new'} transaction={transaction} onDone={onClose} />
    </Modal>
  )
}
