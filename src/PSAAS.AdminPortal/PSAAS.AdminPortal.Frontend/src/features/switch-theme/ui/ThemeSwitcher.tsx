import { faMoon, faSun } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'

// Must match the daisyUI themes enabled in app/styles/index.css (dark is --prefersdark).
type Theme = 'light' | 'dark'

const currentTheme = (): Theme =>
  (document.documentElement.dataset.theme as Theme | undefined) ??
  (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light')

export function ThemeSwitcher() {
  const { t } = useTranslation()
  const [theme, setTheme] = useState(currentTheme)
  const next: Theme = theme === 'dark' ? 'light' : 'dark'

  const toggle = () => {
    document.documentElement.dataset.theme = next
    setTheme(next)
  }

  return (
    <div className="tooltip tooltip-bottom" data-tip={t(`themes.${next}`)}>
      <button type="button" className="btn btn-square btn-ghost" aria-label={t('theme')} onClick={toggle}>
        <FontAwesomeIcon icon={theme === 'dark' ? faMoon : faSun} />
      </button>
    </div>
  )
}
