import { faMoon, faSun } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useSyncExternalStore } from 'react'
import { useTranslation } from 'react-i18next'

// Must match the daisyUI themes enabled in app/styles/index.css (dark is --prefersdark).
type Theme = 'light' | 'dark'

const currentTheme = (): Theme =>
  (document.documentElement.dataset.theme as Theme | undefined) ??
  (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light')

// The theme lives on <html data-theme>: every switcher (header, settings page) follows it.
const subscribe = (onChange: () => void) => {
  const observer = new MutationObserver(onChange)
  observer.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] })
  return () => observer.disconnect()
}

export function ThemeSwitcher() {
  const { t } = useTranslation()
  const theme = useSyncExternalStore(subscribe, currentTheme)
  const next: Theme = theme === 'dark' ? 'light' : 'dark'

  return (
    <div className="tooltip tooltip-bottom" data-tip={t(`themes.${next}`)}>
      <button
        type="button"
        className="btn btn-square btn-ghost"
        aria-label={t('theme')}
        onClick={() => (document.documentElement.dataset.theme = next)}
      >
        <FontAwesomeIcon icon={theme === 'dark' ? faMoon : faSun} />
      </button>
    </div>
  )
}
