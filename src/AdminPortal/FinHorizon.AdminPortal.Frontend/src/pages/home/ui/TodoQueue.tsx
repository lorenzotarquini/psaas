import { faEnvelope, faFileInvoice, faGears, faHourglassHalf, faPersonDigging, faUsers, type IconDefinition } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useRetryProvisioning } from '@/entities/catalog'
import { describeNotification, useNotifications, type Notification } from '@/entities/communication'
import { useResendInvites } from '@/entities/overview'
import { routes, tenantPath } from '@/shared/routes'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { areas, ageLabel, type Area } from '../model/queue'

const icons: Record<Notification['kind'], IconDefinition> = {
  'module.failed': faGears,
  'invoice.overdue': faFileInvoice,
  'trial.ending': faHourglassHalf,
  'users.limit': faUsers,
  'users.invites': faEnvelope,
  'draft.stalled': faPersonDigging,
}
const tones = { critical: 'bg-error/10 text-error', warning: 'bg-warning/15 text-warning', info: 'bg-info/10 text-info' } as const

// UC29 step 3 (mock 1b "Da gestire"): the queue of UC28 with a direct action on each item.
export function TodoQueue() {
  const { t } = useTranslation()
  const { data: items = [], error } = useNotifications()
  const [area, setArea] = useState<Area>('all')
  // Ages are counted from when the queue was opened.
  const [now] = useState(() => new Date())
  const visible = items.filter((n) => (areas[area] as readonly Notification['kind'][]).includes(n.kind))

  return (
    <section className="panel overflow-hidden">
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-base-300 px-4 py-3">
        <h2 className="panel-title flex items-center gap-2">
          {t('notifications.title')}
          <span className="rounded bg-base-200 px-1.5 font-mono text-[11px] text-base-content/70">{items.length}</span>
        </h2>
        <div className="flex gap-1 text-[12.5px]" role="tablist">
          {(Object.keys(areas) as Area[]).map((key) => (
            <button key={key} type="button" role="tab" aria-selected={key === area}
              className={`rounded px-2 py-1 ${key === area ? 'font-semibold text-base-content' : 'text-base-content/60 hover:text-base-content'}`}
              onClick={() => setArea(key)}>
              {t(`overview.areas.${key}`)}
            </button>
          ))}
        </div>
      </div>
      {error && (
        <div className="p-4">
          <ErrorAlert error={error} />
        </div>
      )}
      {visible.length === 0 ? (
        <p className="px-4 py-10 text-center text-base-content/60">{t('notifications.empty')}</p>
      ) : (
        <ul className="divide-y divide-base-300">
          {visible.map((item, i) => (
            <Item key={`${item.kind}-${item.tenantId}-${item.objectId ?? i}`} item={item} now={now} />
          ))}
        </ul>
      )}
    </section>
  )
}

function Item({ item, now }: { item: Notification; now: Date }) {
  const { t } = useTranslation()
  const { text } = describeNotification(item, t)
  return (
    <li className="flex items-center gap-3 px-4 py-3">
      <span className={`grid size-9 shrink-0 place-items-center rounded-lg ${tones[item.severity]}`}>
        <FontAwesomeIcon icon={icons[item.kind]} />
      </span>
      <div className="min-w-0 flex-1">
        <div className="font-semibold">{text}</div>
        <div className="truncate text-[12.5px] text-base-content/60">{item.tenantName || t('overview.variousTenants')}</div>
      </div>
      <span className="data text-[12px] text-base-content/55">{ageLabel(item.at, now, t)}</span>
      <Action item={item} />
    </li>
  )
}

// One action per kind, as in mock 1b: Riprova, Apri fattura, Converti, Proponi upgrade, Reinvia inviti.
function Action({ item }: { item: Notification }) {
  const { t } = useTranslation()
  const retry = useRetryProvisioning(item.tenantId)
  const resend = useResendInvites()
  const link = (to: string, label: string) => (
    <Link to={to} className="btn btn-sm">
      {t(label)}
    </Link>
  )
  switch (item.kind) {
    case 'module.failed':
      return (
        <button type="button" className="btn btn-sm" disabled={retry.isPending} onClick={() => retry.mutate(item.objectId!)}>
          {t('overview.actions.retry')}
        </button>
      )
    case 'invoice.overdue':
      return link(`${routes.billing}?${new URLSearchParams({ invoice: item.objectId ?? '' })}`, 'overview.actions.openInvoice')
    case 'trial.ending':
      return link(tenantPath(item.tenantId, 'subscription'), 'overview.actions.convert')
    case 'users.limit':
      return link(`${routes.communications}?${new URLSearchParams([['new', '1'], ['tenantId', item.tenantId]])}`, 'overview.actions.proposeUpgrade')
    case 'users.invites':
      return (
        <button type="button" className="btn btn-sm" disabled={resend.isPending} onClick={() => resend.mutate()}>
          {t('overview.actions.resendInvites')}
        </button>
      )
    case 'draft.stalled':
      return link(describeNotification(item, t).to, 'overview.actions.resume')
  }
}

// "5 attività da gestire, 2 urgenti" next to the date.
TodoQueue.Summary = function Summary() {
  const { t } = useTranslation()
  const { data: items } = useNotifications()
  if (!items) return null
  const urgent = items.filter((n) => n.severity === 'critical').length
  return <> · {t('overview.summary', { count: items.length, urgent })}</>
}
