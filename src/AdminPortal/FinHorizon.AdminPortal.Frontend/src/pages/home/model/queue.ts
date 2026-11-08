import type { TFunction } from 'i18next'
import type { Notification } from '@/entities/communication'

/** Filter tabs of mock 1b over the kinds of UC28. */
export const areas = {
  all: ['module.failed', 'invoice.overdue', 'trial.ending', 'users.limit', 'users.invites', 'draft.stalled'],
  provisioning: ['module.failed'],
  subscriptions: ['trial.ending', 'draft.stalled'],
  invoices: ['invoice.overdue'],
  users: ['users.limit', 'users.invites'],
} satisfies Record<string, Notification['kind'][]>
export type Area = keyof typeof areas

/** "12 min", "3 h", "2 g" since the item started (or until a future date, e.g. the end of a trial). */
export function ageLabel(iso: string, now: Date, t: TFunction) {
  const minutes = Math.round(Math.abs(now.getTime() - new Date(iso).getTime()) / 60_000)
  if (minutes < 60) return t('overview.age.minutes', { count: Math.max(1, minutes) })
  if (minutes < 60 * 24) return t('overview.age.hours', { count: Math.round(minutes / 60) })
  return t('overview.age.days', { count: Math.round(minutes / (60 * 24)) })
}
