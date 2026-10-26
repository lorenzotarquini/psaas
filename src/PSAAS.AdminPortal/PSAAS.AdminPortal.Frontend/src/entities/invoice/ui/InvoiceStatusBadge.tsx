import { useTranslation } from 'react-i18next'
import { StatusBadge, type StatusTone } from '@/shared/ui/status-badge'
import type { InvoiceStatus } from '../api/invoiceApi'

const tones: Record<InvoiceStatus, StatusTone> = { Issued: 'info', Overdue: 'error', Paid: 'success', Cancelled: 'neutral' }

export function InvoiceStatusBadge({ status }: { status: InvoiceStatus }) {
  const { t } = useTranslation()
  return <StatusBadge tone={tones[status]}>{t(`billing.status.${status}`)}</StatusBadge>
}
