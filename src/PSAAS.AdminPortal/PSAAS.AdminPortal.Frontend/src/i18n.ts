import i18n from 'i18next'
import LanguageDetector from 'i18next-browser-languagedetector'
import HttpBackend from 'i18next-http-backend'
import { initReactI18next } from 'react-i18next'
import { getLogger } from './logger'

const logger = getLogger('i18n')

export const supportedLanguages = ['it', 'en'] as const

// Translations are lazy-loaded from public/locales/<lng>/<ns>.json, one request per language and namespace.
i18n
  .use(HttpBackend)
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    supportedLngs: supportedLanguages,
    fallbackLng: 'it',
    load: 'languageOnly',
    ns: ['common'],
    defaultNS: 'common',
    backend: { loadPath: '/locales/{{lng}}/{{ns}}.json' },
    detection: { order: ['localStorage', 'navigator'], caches: ['localStorage'] },
    interpolation: { escapeValue: false },
  })
  .catch((error: unknown) => logger.error({ err: error }, 'i18n initialization failed'))

i18n.on('failedLoading', (lng, ns, msg) => logger.error({ lng, ns }, msg))

export default i18n
