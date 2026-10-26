import { faBell } from '@fortawesome/free-regular-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { describeNotification, useNotifications } from '@/entities/communication'

const dots = { critical: 'bg-error', warning: 'bg-warning', info: 'bg-info' } as const

// UC28 (mock 1b): the number of things to handle, and the list in a dropdown.
export function NotificationsBell() {
  const { t } = useTranslation()
  const { data: items = [] } = useNotifications()

  return (
    <div className="dropdown dropdown-end">
      <button type="button" tabIndex={0} className="btn btn-ghost btn-sm btn-circle relative" aria-label={t('notifications.label', { count: items.length })}>
        <FontAwesomeIcon icon={faBell} />
        {items.length > 0 && (
          <span className="absolute -top-0.5 -right-0.5 min-w-4 rounded-full bg-error px-1 font-mono text-[10px] leading-4 text-error-content">{items.length}</span>
        )}
      </button>
      <div tabIndex={0} className="dropdown-content z-30 mt-2 w-96 rounded-box border border-base-300 bg-base-100 shadow-lg">
        <div className="border-b border-base-300 px-4 py-2.5 font-semibold">{t('notifications.title')}</div>
        {items.length === 0 ? (
          <p className="px-4 py-6 text-center text-base-content/60">{t('notifications.empty')}</p>
        ) : (
          <ul className="max-h-96 divide-y divide-base-300 overflow-y-auto">
            {items.map((item, i) => {
              const { text, to } = describeNotification(item, t)
              return (
                <li key={i}>
                  <Link to={to} className="flex items-start gap-2.5 px-4 py-2.5 hover:bg-base-200/60">
                    <span className={`mt-1.5 size-2 shrink-0 rounded-full ${dots[item.severity]}`} aria-hidden />
                    <span className="text-[13px]">{text}</span>
                  </Link>
                </li>
              )
            })}
          </ul>
        )}
      </div>
    </div>
  )
}
