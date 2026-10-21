import { faCircleUser, faGear, faRightFromBracket } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { keycloak, logout } from '@/shared/auth'
import { routes } from '@/shared/routes'

// Name and email come from the Keycloak token (standard OIDC claims).
export function ProfileMenu() {
  const { t } = useTranslation()
  const token = keycloak.tokenParsed

  return (
    <div className="dropdown dropdown-end">
      <div tabIndex={0} role="button" className="btn btn-square btn-ghost text-xl" aria-label={t('profile.title')}>
        <FontAwesomeIcon icon={faCircleUser} />
      </div>
      <div tabIndex={-1} className="dropdown-content z-10 mt-2 w-64 rounded-box bg-base-100 shadow-md">
        <div className="px-4 pt-4 pb-2">
          <p className="text-xs font-semibold uppercase text-base-content/60">{t('profile.title')}</p>
          <p className="mt-1 font-medium">
            {token?.given_name} {token?.family_name}
          </p>
          <p className="text-sm text-base-content/70 break-all">{token?.email}</p>
        </div>
        <ul className="menu w-full border-t border-base-300">
          <li>
            {/* daisyUI dropdowns stay open while focused. */}
            <Link to={routes.settings} onClick={() => (document.activeElement as HTMLElement | null)?.blur()}>
              <FontAwesomeIcon icon={faGear} fixedWidth />
              {t('profile.settings')}
            </Link>
          </li>
          <li>
            <button type="button" className="text-error" onClick={() => void logout()}>
              <FontAwesomeIcon icon={faRightFromBracket} fixedWidth />
              {t('logout')}
            </button>
          </li>
        </ul>
      </div>
    </div>
  )
}
