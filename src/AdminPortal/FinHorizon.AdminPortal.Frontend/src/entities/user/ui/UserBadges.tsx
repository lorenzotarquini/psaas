import { useTranslation } from 'react-i18next'
import { initials } from '@/shared/lib/format'
import { StatusBadge, type StatusTone } from '@/shared/ui/status-badge'
import type { UserStatus } from '../api/userApi'

const tones: Record<UserStatus, StatusTone> = { Active: 'success', Invited: 'warning', Suspended: 'error', Disabled: 'neutral' }

export function UserStatusBadge({ status }: { status: UserStatus }) {
  const { t } = useTranslation()
  return <StatusBadge tone={tones[status]}>{t(`users.status.${status}`)}</StatusBadge>
}

export function UserAvatar({ firstName, lastName, email, large }: { firstName: string; lastName: string; email: string; large?: boolean }) {
  return (
    <span
      className={`grid shrink-0 place-items-center rounded-full bg-primary/12 font-semibold text-primary ${large ? 'size-11 text-[14px]' : 'size-7 text-[11px]'}`}
      aria-hidden
    >
      {initials(firstName, lastName, email)}
    </span>
  )
}
