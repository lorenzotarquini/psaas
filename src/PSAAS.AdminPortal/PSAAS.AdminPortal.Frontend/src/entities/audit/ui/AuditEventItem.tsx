import {
  faBuilding,
  faClockRotateLeft,
  faPen,
  faSitemap,
  faUserPlus,
  type IconDefinition,
} from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { formatWhen } from '@/shared/lib/format'
import { tenantPath } from '@/shared/routes'
import type { AuditEvent } from '../api/auditApi'
import { describeAuditEvent } from '../model/describe'

const icons: Record<string, IconDefinition> = {
  'tenant.created': faBuilding,
  'tenant.registry.updated': faPen,
  'tenant.organization.updated': faSitemap,
  'user.created': faUserPlus,
}

/** One line of mock 1b "Attività recente" / 1d "Storico": who did what to whom, when, and the detail. */
export function AuditEventItem({ event, showDetail = true }: { event: AuditEvent; showDetail?: boolean }) {
  const { t, i18n } = useTranslation()
  const { what, detail } = describeAuditEvent(event, t)
  const target =
    event.targetType === 'tenant' ? (
      <Link to={tenantPath(event.targetId)} className="font-semibold hover:underline">
        {event.targetName}
      </Link>
    ) : (
      <b className="font-semibold">{event.targetName}</b>
    )

  return (
    <li className="grid grid-cols-[22px_1fr] gap-2.5 py-1.5">
      <FontAwesomeIcon icon={icons[event.action] ?? faClockRotateLeft} className="mt-1 text-[12px] text-base-content/50" />
      <div className="min-w-0">
        <p className="leading-snug">
          <b className="font-semibold">{event.actorName}</b> {what} {target}
        </p>
        <p className="data mt-0.5 text-[11px] text-base-content/55">
          {formatWhen(event.occurredAt, i18n.language, t('audit.yesterday'))}
          {showDetail && detail && <span className="font-sans"> · {detail}</span>}
        </p>
      </div>
    </li>
  )
}
