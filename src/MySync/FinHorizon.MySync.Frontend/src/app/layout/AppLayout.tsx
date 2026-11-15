import { useTranslation } from 'react-i18next'
import { NavLink, Outlet } from 'react-router'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faChartLine, faHouse, faRightFromBracket, faRightLeft, faSeedling, faWallet } from '@fortawesome/free-solid-svg-icons'
import { LanguageSwitcher, ThemeToggle } from '@/features/preferences'
import { session } from '@/shared/auth'

const navigation = [
  { to: '/', icon: faHouse, label: 'nav.dashboard', end: true },
  { to: '/transactions', icon: faRightLeft, label: 'nav.transactions', end: false },
  { to: '/portfolio', icon: faWallet, label: 'nav.portfolio', end: false },
  { to: '/net-worth', icon: faChartLine, label: 'nav.netWorth', end: false },
] as const

/** The template's shell: a slim icon sidebar (a bottom bar on phones) and a light topbar. */
export function AppLayout() {
  const { t } = useTranslation()
  const environment = import.meta.env.VITE_ENVIRONMENT_LABEL

  return (
    <div className="min-h-screen md:pl-24">
      <nav
        aria-label={t('nav.label')}
        className="fixed inset-x-0 bottom-0 z-20 flex justify-around border-t border-base-300 bg-base-100 py-2 md:inset-y-4 md:left-4 md:right-auto md:w-16 md:flex-col md:justify-start md:gap-3 md:rounded-box md:border-0 md:py-5 md:shadow-sm"
      >
        <span className="hidden place-items-center text-2xl text-primary md:grid md:pb-4" aria-hidden>
          <FontAwesomeIcon icon={faSeedling} />
        </span>
        {navigation.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.end}
            title={t(item.label)}
            className={({ isActive }) =>
              `mx-auto grid size-11 place-items-center rounded-full text-lg transition-colors ${isActive ? 'bg-primary text-primary-content' : 'text-base-content/60 hover:bg-base-200'}`
            }
          >
            <FontAwesomeIcon icon={item.icon} />
            <span className="sr-only">{t(item.label)}</span>
          </NavLink>
        ))}
      </nav>

      <header className="flex flex-wrap items-center gap-2 px-4 pt-4 md:px-6">
        <span className="flex-1 text-lg font-semibold text-primary">{t('appName')}</span>
        {environment && <span className="badge badge-warning badge-sm">{environment}</span>}
        <LanguageSwitcher />
        <ThemeToggle />
        <span className="hidden text-sm sm:inline">{session.userName()}</span>
        <button type="button" className="btn btn-ghost btn-circle" aria-label={t('logout')} title={t('logout')} onClick={() => void session.logout()}>
          <FontAwesomeIcon icon={faRightFromBracket} />
        </button>
      </header>

      <main className="px-4 pb-24 pt-4 md:px-6 md:pb-8">
        <Outlet />
      </main>
    </div>
  )
}
