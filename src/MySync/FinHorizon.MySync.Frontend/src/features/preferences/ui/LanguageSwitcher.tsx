import { useTranslation } from 'react-i18next'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faChevronDown } from '@fortawesome/free-solid-svg-icons'
import { supportedLanguages } from '@/shared/i18n'
import { useDropdown } from '@/shared/lib/dropdown'

/** Country of the flag shown for each language (flag-icons codes). */
const flags: Record<(typeof supportedLanguages)[number], string> = { it: 'it', en: 'gb' }

function Flag({ language }: { language: (typeof supportedLanguages)[number] }) {
  return <span className={`fi fi-${flags[language]} rounded-sm`} aria-hidden />
}

/** The language, with its flag; remembered by i18next's language detector (localStorage). */
export function LanguageSwitcher() {
  const { t, i18n } = useTranslation()
  const { open, toggle, close, containerRef, buttonRef } = useDropdown()
  const current = (supportedLanguages as readonly string[]).includes(i18n.resolvedLanguage ?? '')
    ? (i18n.resolvedLanguage as (typeof supportedLanguages)[number])
    : supportedLanguages[0]

  function choose(language: (typeof supportedLanguages)[number]) {
    void i18n.changeLanguage(language)
    close()
  }

  return (
    <div ref={containerRef} className="relative">
      <button
        ref={buttonRef}
        type="button"
        className="btn btn-sm gap-2"
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-label={`${t('preferences.language')}: ${t(`preferences.languages.${current}`)}`}
        onClick={toggle}
      >
        <Flag language={current} />
        <span className="hidden sm:inline">{t(`preferences.languages.${current}`)}</span>
        <FontAwesomeIcon icon={faChevronDown} className="text-xs" />
      </button>
      {open && (
        <ul role="listbox" aria-label={t('preferences.language')} className="menu absolute right-0 z-30 mt-2 w-44 rounded-box bg-base-100 p-1 shadow-lg">
          {supportedLanguages.map((language) => (
            <li key={language} role="option" aria-selected={language === current}>
              <button type="button" className={language === current ? 'menu-active' : ''} onClick={() => choose(language)}>
                <Flag language={language} />
                {t(`preferences.languages.${language}`)}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
