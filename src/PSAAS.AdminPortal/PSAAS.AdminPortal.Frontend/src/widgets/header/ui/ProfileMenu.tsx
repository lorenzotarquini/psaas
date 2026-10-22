import { faRightFromBracket, faSliders } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { keycloak, logout } from '@/shared/auth'
import { initials } from '@/shared/lib/format'
import { routes } from '@/shared/routes'

// UC8 lives here since the home became the overview: identity and roles straight from the token.
export function ProfileMenu() {
  const { t } = useTranslation()
  const token = keycloak.tokenParsed as
    | { sub?: string; given_name?: string; family_name?: string; email?: string; preferred_username?: string }
    | undefined
  // Administrator is a role of the adminportal-api client: gather client and realm roles alike.
  const roles = [
    ...new Set([...Object.values(keycloak.resourceAccess ?? {}).flatMap((access) => access.roles), ...(keycloak.realmAccess?.roles ?? [])]),
  ]
  // daisyUI dropdowns stay open while focused.
  const close = () => (document.activeElement as HTMLElement | null)?.blur()

  return (
    <div className="dropdown dropdown-end">
      <div
        tabIndex={0}
        role="button"
        className="ml-1 grid size-[30px] cursor-pointer place-items-center rounded-full bg-primary/15 text-xs font-semibold text-primary"
        aria-label={t('profile.title')}
      >
        {initials(token?.given_name, token?.family_name, token?.preferred_username)}
      </div>
      <div tabIndex={-1} className="dropdown-content z-30 mt-2 w-72 rounded-box border border-base-300 bg-base-100 shadow-lg">
        <div className="px-4 pt-3.5 pb-3">
          <p className="eyebrow text-base-content/50">{t('profile.title')}</p>
          <p className="mt-1 font-semibold">
            {token?.given_name} {token?.family_name}
          </p>
          <p className="break-all text-base-content/70">{token?.email}</p>
          <dl className="mt-2 grid grid-cols-[auto_1fr] gap-x-3 gap-y-1 text-[12px]">
            <dt className="text-base-content/50">{t('me.keycloakId')}</dt>
            <dd className="data truncate text-[11.5px]" title={token?.sub}>
              {token?.sub}
            </dd>
            <dt className="text-base-content/50">{t('me.roles')}</dt>
            <dd className="flex flex-wrap gap-1">
              {roles.map((role) => (
                <span key={role} className="badge badge-soft badge-primary badge-xs">
                  {role}
                </span>
              ))}
            </dd>
          </dl>
        </div>
        <ul className="menu w-full border-t border-base-300">
          <li>
            <Link to={routes.settings} onClick={close}>
              <FontAwesomeIcon icon={faSliders} fixedWidth />
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
