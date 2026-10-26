import { faMoon, faSun } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useSyncExternalStore } from 'react'
import { useTranslation } from 'react-i18next'
import { storageKey, themes, type Mode } from '../model/theme'

const currentMode = (): Mode => {
  const theme = document.documentElement.dataset.theme
  if (theme) return theme === themes.dark ? 'dark' : 'light'
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

// The theme lives on <html data-theme>: every switcher (header, settings page) follows it.
const subscribe = (onChange: () => void) => {
  const observer = new MutationObserver(onChange)
  observer.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] })
  return () => observer.disconnect()
}

export function ThemeSwitcher({ className = 'btn btn-square btn-ghost btn-sm' }: { className?: string }) {
  const { t } = useTranslation()
  const mode = useSyncExternalStore(subscribe, currentMode)
  const next: Mode = mode === 'dark' ? 'light' : 'dark'

  const toggle = () => {
    document.documentElement.dataset.theme = themes[next]
    try {
      localStorage.setItem(storageKey, next)
    } catch {
      // Not persisted: the choice lasts until reload.
    }
  }

  return (
    <div className="tooltip tooltip-bottom" data-tip={t(`themes.${next}`)}>
      <button type="button" className={className} aria-label={t('theme')} onClick={toggle}>
        <FontAwesomeIcon icon={mode === 'dark' ? faMoon : faSun} />
      </button>
    </div>
  )
}
