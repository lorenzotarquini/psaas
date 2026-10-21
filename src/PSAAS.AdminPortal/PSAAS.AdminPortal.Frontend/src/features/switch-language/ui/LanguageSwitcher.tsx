import { faGlobe } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { supportedLanguages } from '@/shared/i18n'

// Endonyms: each language is shown in its own language.
const languageNames: Record<(typeof supportedLanguages)[number], string> = { it: 'Italiano', en: 'English' }

export function LanguageSwitcher() {
  const { t, i18n } = useTranslation()

  const select = (lng: string) => {
    void i18n.changeLanguage(lng)
    // daisyUI dropdowns stay open while focused.
    ;(document.activeElement as HTMLElement | null)?.blur()
  }

  return (
    <div className="dropdown dropdown-end">
      <div tabIndex={0} role="button" className="btn btn-ghost gap-2" aria-label={t('language')}>
        <FontAwesomeIcon icon={faGlobe} />
        <span className="uppercase">{i18n.resolvedLanguage}</span>
      </div>
      <ul tabIndex={-1} className="dropdown-content menu bg-base-100 rounded-box z-10 w-36 p-2 shadow-sm">
        {supportedLanguages.map((lng) => (
          <li key={lng}>
            <button
              type="button"
              lang={lng}
              className={i18n.resolvedLanguage === lng ? 'menu-active' : ''}
              onClick={() => select(lng)}
            >
              {languageNames[lng]}
            </button>
          </li>
        ))}
      </ul>
    </div>
  )
}
