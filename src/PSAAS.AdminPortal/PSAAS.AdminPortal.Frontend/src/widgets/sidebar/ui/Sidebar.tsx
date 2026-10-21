import { faBars, faBuilding, faHouse, faIdBadge, faUsers } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { NavLink } from 'react-router'
import { routes } from '@/shared/routes'

const items = [
  { key: 'home', href: routes.home, icon: faHouse },
  { key: 'tenants', href: routes.tenants, icon: faBuilding },
  { key: 'users', href: routes.users, icon: faUsers },
  { key: 'profiles', href: routes.profiles, icon: faIdBadge },
] as const

// Collapses to icons only on large screens when the drawer checkbox is unchecked (daisyUI is-drawer-close).
export function Sidebar({ drawerId }: { drawerId: string }) {
  const { t } = useTranslation()

  return (
    <nav className="flex min-h-full flex-col bg-base-100 border-r border-base-300 is-drawer-close:w-16 is-drawer-open:w-64">
      <div className="flex h-16 items-center gap-2 px-3">
        <label htmlFor={drawerId} aria-label={t('menu')} className="btn btn-square btn-ghost">
          <FontAwesomeIcon icon={faBars} />
        </label>
        <span className="is-drawer-close:hidden text-lg font-semibold whitespace-nowrap">{t('appName')}</span>
      </div>
      <ul className="menu w-full grow gap-1">
        {items.map(({ key, href, icon }) => (
          <li key={key}>
            <NavLink
              to={href}
              end={href === routes.home}
              className={({ isActive }) =>
                `is-drawer-close:tooltip is-drawer-close:tooltip-right ${isActive ? 'menu-active' : ''}`
              }
              data-tip={t(`nav.${key}`)}
            >
              <FontAwesomeIcon icon={icon} fixedWidth />
              <span className="is-drawer-close:hidden">{t(`nav.${key}`)}</span>
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  )
}
