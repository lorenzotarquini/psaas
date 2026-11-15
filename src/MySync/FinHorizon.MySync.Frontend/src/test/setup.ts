import { cleanup } from '@testing-library/react'
import { afterEach } from 'vitest'
import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import it from '../../public/locales/it/common.json'

// Tests render the real Italian texts, loaded synchronously instead of over HTTP.
void i18n.use(initReactI18next).init({
  lng: 'it',
  fallbackLng: 'it',
  ns: ['common'],
  defaultNS: 'common',
  resources: { it: { common: it } },
  interpolation: { escapeValue: false },
})

afterEach(() => cleanup())
