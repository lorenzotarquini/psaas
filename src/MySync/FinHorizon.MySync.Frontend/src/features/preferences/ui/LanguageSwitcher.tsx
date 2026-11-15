import { useTranslation } from 'react-i18next'
import { supportedLanguages } from '@/shared/i18n'

/** The language is remembered by i18next's language detector (localStorage). */
export function LanguageSwitcher() {
  const { t, i18n } = useTranslation()
  return (
    <select
      className="select select-sm w-auto"
      aria-label={t('preferences.language')}
      value={i18n.resolvedLanguage}
      onChange={(event) => void i18n.changeLanguage(event.target.value)}
    >
      {supportedLanguages.map((language) => (
        <option key={language} value={language}>
          {t(`preferences.languages.${language}`)}
        </option>
      ))}
    </select>
  )
}
