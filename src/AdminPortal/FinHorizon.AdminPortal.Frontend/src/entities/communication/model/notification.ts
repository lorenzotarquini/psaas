import type { TFunction } from 'i18next'
import { onboardingPath, routes, tenantPath } from '@/shared/routes'
import type { Notification } from '../api/communicationApi'

/** UC28 step 2: what happens, to whom, and where to fix it. */
export function describeNotification(n: Notification, t: TFunction): { text: string; to: string } {
  const values = { tenant: n.tenantName, subject: n.subject ?? '', count: n.value ?? 0, limit: n.limit ?? 0 }
  const to: Record<Notification['kind'], string> = {
    'module.failed': tenantPath(n.tenantId, 'subscription'),
    'invoice.overdue': `${routes.billing}?${new URLSearchParams({ invoice: n.objectId ?? '' })}`,
    'trial.ending': tenantPath(n.tenantId, 'subscription'),
    'users.limit': tenantPath(n.tenantId, 'users'),
    'draft.stalled': onboardingPath(n.tenantId, 'registry'),
    'users.invites': `${routes.users}?status=Invited`,
  }
  return { text: t(`notifications.kinds.${n.kind}`, values), to: to[n.kind] }
}
