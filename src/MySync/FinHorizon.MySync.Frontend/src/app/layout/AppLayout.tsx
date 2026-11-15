import { useTranslation } from 'react-i18next'
import { NavLink, Outlet } from 'react-router'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faChartLine, faGear, faHouse, faRightLeft, faSeedling, faTableColumns, faWallet } from '@fortawesome/free-solid-svg-icons'
import { LanguageSwitcher } from '@/features/preferences'
import { storageKeys, useSessionState } from '@/shared/lib/session-state'
import { UserMenu } from '@/widgets/user-menu'

const navigation = [
  { to: '/', icon: faHouse, label: 'nav.dashboard', end: true },
  { to: '/transactions', icon: faRightLeft, label: 'nav.transactions', end: false },
  { to: '/portfolio', icon: faWallet, label: 'nav.portfolio', end: false },
  { to: '/net-worth', icon: faChartLine, label: 'nav.netWorth', end: false },
  { to: '/settings', icon: faGear, label: 'nav.settings', end: false },
] as const

/**
 * The template's shell: a sidebar of icons that the topbar's sidebar button expands to show the app name and a label
 * per icon (a bottom bar on phones), and a light topbar. The expanded/collapsed choice is remembered on the device.
 */
export function AppLayout() {
  const { t } = useTranslation()
  const [expanded, setExpanded] = useSessionState(storageKeys.sidebarExpanded, false, 'local')

  return (
    <div className={`min-h-screen ${expanded ? 'md:pl-64' : 'md:pl-24'}`}>
      <nav
        aria-label={t('nav.label')}
        className={`fixed inset-x-0 bottom-0 z-20 flex justify-around border-t border-base-300 bg-base-100 py-2 md:inset-y-4 md:left-4 md:right-auto md:flex-col md:justify-start md:gap-2 md:rounded-box md:border-0 md:py-5 md:shadow-sm md:transition-[width] ${expanded ? 'md:w-56 md:px-3' : 'md:w-16'}`}
      >
        <span className={`hidden items-center gap-2 pb-4 text-primary md:flex ${expanded ? 'px-2' : 'justify-center'}`}>
          <FontAwesomeIcon icon={faSeedling} className="text-2xl" aria-hidden />
          {expanded && <span className="text-lg font-semibold">{t('appName')}</span>}
        </span>
        {navigation.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.end}
            title={expanded ? undefined : t(item.label)}
            className={({ isActive }) =>
              `flex items-center text-lg transition-colors ${isActive ? 'bg-primary text-primary-content' : 'text-base-content/60 hover:bg-base-200'} ${
                expanded ? 'mx-auto size-11 justify-center rounded-full md:mx-0 md:h-11 md:w-full md:justify-start md:gap-3 md:rounded-field md:px-3' : 'mx-auto size-11 justify-center rounded-full'
              }`
            }
          >
            <FontAwesomeIcon icon={item.icon} className="w-5" />
            <span className={expanded ? 'sr-only text-sm font-medium md:not-sr-only' : 'sr-only'}>{t(item.label)}</span>
          </NavLink>
        ))}
        <span className={`muted hidden text-[11px] md:mt-auto md:block ${expanded ? 'md:px-2' : 'md:text-center'}`} title={t('version')}>
          v{__APP_VERSION__}
        </span>
      </nav>

      <header className="flex flex-wrap items-center gap-2 px-4 pt-4 md:px-6">
        <div className="flex-1">
          <button
            type="button"
            className="btn btn-ghost btn-sm btn-square hidden md:inline-flex"
            aria-expanded={expanded}
            aria-label={t(expanded ? 'nav.collapse' : 'nav.expand')}
            title={t(expanded ? 'nav.collapse' : 'nav.expand')}
            onClick={() => setExpanded(!expanded)}
          >
            <FontAwesomeIcon icon={faTableColumns} />
          </button>
        </div>
        <LanguageSwitcher />
        <UserMenu />
      </header>

      <main className="px-4 pb-24 pt-4 md:px-6 md:pb-8">
        <Outlet />
      </main>
    </div>
  )
}
