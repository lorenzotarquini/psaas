import { useTranslation } from 'react-i18next'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faMoon, faSun } from '@fortawesome/free-solid-svg-icons'
import { useThemeMode } from '@/shared/theme'

export function ThemeToggle() {
  const { t } = useTranslation()
  const { mode, toggle } = useThemeMode()
  return (
    <button
      type="button"
      className="btn btn-ghost btn-circle"
      aria-label={t(mode === 'dark' ? 'preferences.lightMode' : 'preferences.darkMode')}
      aria-pressed={mode === 'dark'}
      onClick={toggle}
    >
      <FontAwesomeIcon icon={mode === 'dark' ? faSun : faMoon} />
    </button>
  )
}
