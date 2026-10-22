import { faBars, faChevronRight } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { Fragment, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { LanguageSwitcher } from '@/features/switch-language'
import { ThemeSwitcher } from '@/features/switch-theme'
import { useCrumbs } from '@/shared/lib/breadcrumb'
import { ProfileMenu } from './ProfileMenu'

interface HeaderProps {
  drawerId: string
  /** Global search trigger (⌘K), in the middle of the bar. */
  search?: ReactNode
  /** Extra controls before the avatar, e.g. notifications. */
  actions?: ReactNode
}

export function Header({ drawerId, search, actions }: HeaderProps) {
  const { t } = useTranslation()
  const crumbs = useCrumbs()
  const environment = import.meta.env.VITE_ENVIRONMENT_LABEL

  return (
    <header className="sticky top-0 z-20 flex h-[52px] items-center gap-4 border-b border-base-300 bg-base-100 px-5">
      {/* Mobile only: on desktop the sidebar is always open. */}
      <label htmlFor={drawerId} aria-label={t('menu')} className="btn btn-square btn-ghost btn-sm lg:hidden">
        <FontAwesomeIcon icon={faBars} />
      </label>
      <nav aria-label={t('breadcrumb')} className="flex min-w-0 items-center gap-2 whitespace-nowrap">
        {crumbs.map((crumb, i) => (
          <Fragment key={`${i}-${crumb.label}`}>
            {i > 0 && <FontAwesomeIcon icon={faChevronRight} className="text-[10px] text-base-content/40" />}
            {crumb.to && i < crumbs.length - 1 ? (
              <Link to={crumb.to} className="text-base-content/60 hover:text-base-content">
                {crumb.label}
              </Link>
            ) : (
              <span className="truncate font-semibold">{crumb.label}</span>
            )}
          </Fragment>
        ))}
      </nav>
      <div className="hidden flex-1 justify-center md:flex">{search}</div>
      <div className="ml-auto flex items-center gap-1 md:ml-0">
        {environment && (
          <span className="badge badge-soft badge-warning badge-sm mr-2 gap-1.5 font-mono font-semibold">
            <span className="size-1.5 rounded-full bg-current" aria-hidden />
            {environment}
          </span>
        )}
        <LanguageSwitcher />
        <ThemeSwitcher />
        {actions}
        <ProfileMenu />
      </div>
    </header>
  )
}
