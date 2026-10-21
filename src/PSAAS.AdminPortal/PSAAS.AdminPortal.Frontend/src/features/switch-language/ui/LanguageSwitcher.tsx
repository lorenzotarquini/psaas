import { useTranslation } from 'react-i18next'
import { supportedLanguages } from '@/shared/i18n'

export function LanguageSwitcher() {
  const { t, i18n } = useTranslation()

  return (
    <select
      className="select select-sm w-auto"
      aria-label={t('language')}
      value={i18n.resolvedLanguage}
      onChange={(e) => i18n.changeLanguage(e.target.value)}
    >
      {supportedLanguages.map((lng) => (
        <option key={lng} value={lng}>
          {lng.toUpperCase()}
        </option>
      ))}
    </select>
  )
}
