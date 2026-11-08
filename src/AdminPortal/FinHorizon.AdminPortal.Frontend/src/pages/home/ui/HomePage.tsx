import { faBuilding, faHourglassHalf, faPlus, faPuzzlePiece, faUserPlus, faUsers, type IconDefinition } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { AuditEventItem, useAudit } from '@/entities/audit'
import { useOverview } from '@/entities/overview'
import { keycloak } from '@/shared/auth'
import { useBreadcrumb } from '@/shared/lib/breadcrumb'
import { routes } from '@/shared/routes'
import { Kbd } from '@/shared/ui/kbd'
import { ServicesStatus } from '@/widgets/services-status'
import { TodoQueue } from './TodoQueue'

// UC29 (mock 1b): the work of the day — KPIs, what to handle, connected services, recent activity.
export function HomePage() {
  const { t, i18n } = useTranslation()
  useBreadcrumb([{ label: t('nav.home') }])
  const { data: kpis } = useOverview()
  const { data: activity } = useAudit({ page: 1, pageSize: 5 })
  const name = (keycloak.tokenParsed as { given_name?: string } | undefined)?.given_name
  // The moment the page opened: greeting and date do not change while it stays open.
  const [now] = useState(() => new Date())
  const hour = now.getHours()
  const greeting = t(hour < 13 ? 'overview.morning' : hour < 18 ? 'overview.afternoon' : 'overview.evening', { name: name ?? '' })
  const today = now.toLocaleDateString(i18n.language, { weekday: 'long', day: 'numeric', month: 'long' })

  const cards: { key: string; icon: IconDefinition; value: number | null | undefined; note: string }[] = [
    { key: 'activeTenants', icon: faBuilding, value: kpis?.activeTenants, note: t('overview.thisMonth', { count: kpis?.activeTenantsThisMonth ?? 0 }) },
    { key: 'trials', icon: faHourglassHalf, value: kpis?.trialTenants, note: t('overview.endingSoon', { count: kpis?.trialsEndingSoon ?? 0 }) },
    { key: 'users', icon: faUsers, value: kpis?.users, note: t('overview.createdThisMonth', { count: kpis?.usersThisMonth ?? 0 }) },
    { key: 'modules', icon: faPuzzlePiece, value: kpis?.modules, note: t('overview.inBeta', { count: kpis?.modulesInBeta ?? 0 }) },
  ]

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-[22px] font-semibold tracking-[-0.01em]">{greeting.trim().replace(/,$/, '')}</h1>
          <p className="text-base-content/60 first-letter:uppercase">
            {today}
            <TodoQueue.Summary />
          </p>
        </div>
        <div className="flex gap-2">
          <Link to={routes.userNew} className="btn btn-sm">
            <FontAwesomeIcon icon={faUserPlus} />
            {t('users.create')}
          </Link>
          <Link to={routes.tenantNew} className="btn btn-primary btn-sm">
            <FontAwesomeIcon icon={faPlus} />
            {t('tenantForm.createTitle')}
            <Kbd>N</Kbd>
          </Link>
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        {cards.map((card) => (
          <div key={card.key} className="panel px-4 py-3.5">
            <div className="flex items-center justify-between text-base-content/60">
              <span className="text-[12.5px]">{t(`overview.kpi.${card.key}`)}</span>
              <FontAwesomeIcon icon={card.icon} />
            </div>
            <div className="data mt-1.5 text-[26px] font-semibold">{card.value === undefined ? '…' : card.value === null ? '–' : card.value.toLocaleString(i18n.language)}</div>
            <div className="text-[12px] text-base-content/60">{card.note}</div>
          </div>
        ))}
      </div>

      <div className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_420px]">
        <TodoQueue />
        <aside className="flex flex-col gap-4">
          <ServicesStatus />
          <section className="panel px-4 py-3.5">
            <div className="mb-2.5 flex items-center justify-between">
              <h2 className="panel-title">{t('overview.recent')}</h2>
              <Link to={routes.audit} className="link link-primary link-hover text-[12px]">
                {t('audit.title')} →
              </Link>
            </div>
            <ul className="flex flex-col gap-3">
              {activity?.items.map((event) => (
                <AuditEventItem key={event.id} event={event} />
              ))}
            </ul>
          </section>
        </aside>
      </div>
    </div>
  )
}
