import { useTranslation } from 'react-i18next'
import { StatusBadge, type StatusTone } from '@/shared/ui/status-badge'
import type { TenantStatus } from '../api/tenantApi'

// Mock 1a: green active, indigo trial, amber onboarding, red suspended, grey notice and ceased.
const tones: Record<TenantStatus, StatusTone> = {
  Onboarding: 'warning',
  Trial: 'info',
  Active: 'success',
  Suspended: 'error',
  Cancelling: 'neutral',
  Ceased: 'neutral',
}

export function TenantStatusBadge({ status }: { status: TenantStatus }) {
  const { t } = useTranslation()
  return <StatusBadge tone={tones[status]}>{t(`tenantStatus.${status}`)}</StatusBadge>
}
