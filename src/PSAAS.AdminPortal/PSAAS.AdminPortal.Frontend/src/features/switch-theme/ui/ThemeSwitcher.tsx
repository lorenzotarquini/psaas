import { useTranslation } from 'react-i18next'

// Must match the daisyUI themes enabled in app/styles/index.css.
const themes = ['light', 'dark'] as const

export function ThemeSwitcher() {
  const { t } = useTranslation()

  return (
    <select
      className="select select-sm w-auto"
      aria-label={t('theme')}
      defaultValue=""
      onChange={(e) => (document.documentElement.dataset.theme = e.target.value)}
    >
      <option value="" disabled>
        {t('theme')}
      </option>
      {themes.map((theme) => (
        <option key={theme} value={theme}>
          {t(`themes.${theme}`)}
        </option>
      ))}
    </select>
  )
}
