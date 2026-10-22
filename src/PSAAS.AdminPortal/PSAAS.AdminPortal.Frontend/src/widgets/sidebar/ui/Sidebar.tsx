import {
  faBuilding,
  faBullhorn,
  faClockRotateLeft,
  faFileContract,
  faFileInvoice,
  faGaugeHigh,
  faIdBadge,
  faLayerGroup,
  faPuzzlePiece,
  faSliders,
  faUsers,
  type IconDefinition,
} from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { NavLink } from 'react-router'
import { useModules } from '@/entities/catalog'
import { keycloak } from '@/shared/auth'
import { routes } from '@/shared/routes'

interface NavItem {
  key: string
  href: string
  icon: IconDefinition
}

// Groups of screen 1b: daily work, what is sold, how the platform is run.
const groups: { key: string; items: NavItem[] }[] = [
  {
    key: 'operations',
    items: [
      { key: 'home', href: routes.home, icon: faGaugeHigh },
      { key: 'tenants', href: routes.tenants, icon: faBuilding },
      { key: 'users', href: routes.users, icon: faUsers },
      { key: 'profiles', href: routes.profiles, icon: faIdBadge },
    ],
  },
  {
    key: 'catalog',
    items: [
      { key: 'modules', href: routes.modules, icon: faPuzzlePiece },
      { key: 'plans', href: routes.plans, icon: faLayerGroup },
      { key: 'subscriptions', href: routes.subscriptions, icon: faFileContract },
    ],
  },
  {
    key: 'administration',
    items: [
      { key: 'billing', href: routes.billing, icon: faFileInvoice },
      { key: 'communications', href: routes.communications, icon: faBullhorn },
      { key: 'audit', href: routes.audit, icon: faClockRotateLeft },
      { key: 'settings', href: routes.settings, icon: faSliders },
    ],
  },
]

/** Ink sidebar: it stays dark in both themes to set the operator console apart from the public platform. */
export function Sidebar({ badges = {} }: { badges?: Partial<Record<string, number>> }) {
  const { t } = useTranslation()
  // Footer of mock 1b: the platform version is the version of the core module.
  const core = useModules().data?.find((module) => module.isCore)

  return (
    <nav className="flex min-h-full w-[228px] flex-col gap-5 bg-neutral px-2.5 py-3.5 text-neutral-content">
      <div className="flex items-center gap-2.5 px-2 pb-1">
        <div className="grid size-7 place-items-center rounded-md bg-white/10 font-mono text-xs font-semibold text-primary">ps</div>
        <div>
          <div className="text-[13.5px] font-semibold text-white">PSAAS</div>
          <div className="text-[11px] text-neutral-content/60">{t('nav.console')}</div>
        </div>
      </div>
      {groups.map((group) => (
        <div key={group.key} className="flex flex-col gap-px">
          <div className="eyebrow px-2.5 pb-1.5 text-neutral-content/55">{t(`nav.groups.${group.key}`)}</div>
          {group.items.map(({ key, href, icon }) => (
            <NavLink
              key={key}
              to={href}
              end={href === routes.home}
              className={({ isActive }) =>
                `flex h-8 items-center gap-2.5 rounded-md px-2.5 transition-colors ${
                  isActive ? 'bg-white/12 text-white' : 'text-neutral-content/90 hover:bg-white/6 hover:text-white'
                }`
              }
            >
              <FontAwesomeIcon icon={icon} fixedWidth className="text-[12.5px]" />
              <span className="flex-1">{t(`nav.${key}`)}</span>
              {Boolean(badges[key]) && (
                <span className="rounded-full bg-error px-1.5 font-mono text-[11px] font-medium text-error-content">{badges[key]}</span>
              )}
            </NavLink>
          ))}
        </div>
      ))}
      <div className="mt-auto flex justify-between border-t border-white/10 px-2.5 pt-2.5 font-mono text-[11px] text-neutral-content/55">
        <span title={`console v${__APP_VERSION__}`}>{core ? `core ${core.version}` : `v${__APP_VERSION__}`}</span>
        <span>realm {keycloak.realm}</span>
      </div>
    </nav>
  )
}
