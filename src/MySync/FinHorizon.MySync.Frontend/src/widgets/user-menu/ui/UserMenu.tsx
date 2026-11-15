import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faGear, faMoon, faRightFromBracket } from '@fortawesome/free-solid-svg-icons'
import { session } from '@/shared/auth'
import { useDropdown } from '@/shared/lib/dropdown'
import { useThemeMode } from '@/shared/theme'

/** "Mario Rossi" → "MR"; one word gives one letter, nothing gives "?". */
export function initials(firstName: string, lastName: string, fallback: string) {
  const letters = [firstName, lastName].map((part) => part.trim()[0] ?? '').join('')
  return (letters || fallback.trim()[0] || '?').toUpperCase()
}

function Avatar({ text, size }: { text: string; size: 'sm' | 'lg' }) {
  return (
    <span
      className={`grid shrink-0 place-items-center rounded-full bg-primary/15 font-semibold text-primary ${size === 'lg' ? 'size-16 text-xl' : 'size-9 text-sm'}`}
      aria-hidden
    >
      {text}
    </span>
  )
}

/** The avatar of the topbar and its menu: bigger avatar and name, dark theme, settings, sign out. */
export function UserMenu() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { mode, toggle: toggleTheme } = useThemeMode()
  const { open, toggle, close, containerRef, buttonRef } = useDropdown()
  const profile = session.profile()
  const name = session.userName()
  const avatar = initials(profile.firstName, profile.lastName, name)

  return (
    <div ref={containerRef} className="relative">
      <button
        ref={buttonRef}
        type="button"
        className="btn btn-ghost btn-circle"
        aria-haspopup="menu"
        aria-expanded={open}
        aria-label={t('userMenu.open')}
        onClick={toggle}
      >
        <Avatar text={avatar} size="sm" />
      </button>
      {open && (
        <div role="menu" aria-label={t('userMenu.label')} className="absolute right-0 z-30 mt-2 w-64 rounded-box bg-base-100 p-2 shadow-lg">
          <div className="flex flex-col items-center gap-2 px-2 pb-2 pt-3">
            <Avatar text={avatar} size="lg" />
            <span className="font-medium">{name}</span>
          </div>
          <div className="my-1 border-t border-base-300" />
          <ul className="menu w-full p-0">
            <li>
              <button type="button" role="menuitemcheckbox" aria-checked={mode === 'dark'} onClick={toggleTheme}>
                <FontAwesomeIcon icon={faMoon} />
                {t('userMenu.darkTheme')}
                <input type="checkbox" className="toggle toggle-primary toggle-sm ml-auto" checked={mode === 'dark'} readOnly tabIndex={-1} aria-hidden />
              </button>
            </li>
            <li>
              <button
                type="button"
                role="menuitem"
                onClick={() => {
                  close()
                  void navigate('/settings')
                }}
              >
                <FontAwesomeIcon icon={faGear} />
                {t('nav.settings')}
              </button>
            </li>
          </ul>
          <div className="my-1 border-t border-base-300" />
          <ul className="menu w-full p-0">
            <li>
              <button type="button" role="menuitem" className="text-error" onClick={() => void session.logout()}>
                <FontAwesomeIcon icon={faRightFromBracket} />
                {t('logout')}
              </button>
            </li>
          </ul>
        </div>
      )}
    </div>
  )
}
