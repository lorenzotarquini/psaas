import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ApiError, apiGet, type Me } from './api'
import { keycloak, logout } from './auth'
import { supportedLanguages } from './i18n'

const themes = ['light', 'dark'] as const

export default function App() {
  const { t, i18n } = useTranslation()
  const [me, setMe] = useState<Me>()
  const [errorStatus, setErrorStatus] = useState<number>()

  useEffect(() => {
    const controller = new AbortController()
    apiGet<Me>('/api/me', controller.signal)
      .then(setMe)
      .catch((error: unknown) => {
        if (!controller.signal.aborted) setErrorStatus(error instanceof ApiError ? error.status : 0)
      })
    return () => controller.abort()
  }, [])

  return (
    <div className="min-h-screen bg-base-200">
      <header className="navbar bg-base-100 shadow-sm gap-2">
        <span className="flex-1 text-xl font-semibold px-2">{t('appName')}</span>
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
        <span className="text-sm">{keycloak.tokenParsed?.preferred_username}</span>
        <button type="button" className="btn btn-sm btn-ghost" onClick={() => void logout()}>
          {t('logout')}
        </button>
      </header>

      <main className="p-6">
        <div className="card bg-base-100 shadow-sm max-w-xl">
          <div className="card-body">
            <h1 className="card-title">{t('me.title')}</h1>
            {errorStatus !== undefined ? (
              <div role="alert" className="alert alert-error">
                {t('me.error', { status: errorStatus })}
              </div>
            ) : !me ? (
              <span className="loading loading-spinner" aria-label={t('loading')} />
            ) : (
              <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1">
                <dt className="font-medium">{t('me.keycloakId')}</dt>
                <dd>{me.keycloakId}</dd>
                <dt className="font-medium">{t('me.name')}</dt>
                <dd>{me.name}</dd>
                <dt className="font-medium">{t('me.email')}</dt>
                <dd>{me.email}</dd>
                <dt className="font-medium">{t('me.roles')}</dt>
                <dd className="flex flex-wrap gap-1">
                  {me.roles.map((role) => (
                    <span key={role} className="badge badge-primary">
                      {role}
                    </span>
                  ))}
                </dd>
              </dl>
            )}
          </div>
        </div>
      </main>
    </div>
  )
}
