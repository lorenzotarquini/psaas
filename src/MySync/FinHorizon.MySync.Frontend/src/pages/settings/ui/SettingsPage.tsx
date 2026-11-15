import { useTranslation } from 'react-i18next'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faKey, faUserPen } from '@fortawesome/free-solid-svg-icons'
import { DisplayCurrencySetting } from '@/features/display-currency'
import { LanguageSwitcher, ThemeToggle } from '@/features/preferences'
import { session } from '@/shared/auth'

/**
 * UC11: the user's own data and password. Both are edited on Keycloak's pages (Application Initiated Actions):
 * MySync never sees the password and needs no admin rights on the realm.
 */
export function SettingsPage() {
  const { t } = useTranslation()
  const profile = session.profile()
  const rows = [
    { label: t('settings.firstName'), value: profile.firstName },
    { label: t('settings.lastName'), value: profile.lastName },
    { label: t('settings.email'), value: profile.email },
    { label: t('settings.username'), value: profile.username },
  ]

  return (
    <div className="flex max-w-3xl flex-col gap-6">
      <header>
        <h1 className="text-2xl font-semibold">{t('settings.title')}</h1>
        <p className="muted">{t('settings.subtitle')}</p>
      </header>

      <section className="surface flex flex-col gap-4 p-5" aria-labelledby="settings-profile">
        <header className="flex flex-wrap items-center justify-between gap-2">
          <h2 id="settings-profile" className="surface-title">
            {t('settings.profileTitle')}
          </h2>
          <button type="button" className="btn btn-primary btn-sm" onClick={() => void session.updateProfile()}>
            <FontAwesomeIcon icon={faUserPen} /> {t('settings.editProfile')}
          </button>
        </header>
        <dl className="grid gap-x-6 gap-y-3 sm:grid-cols-2">
          {rows.map((row) => (
            <div key={row.label}>
              <dt className="muted text-xs">{row.label}</dt>
              <dd className="font-medium">{row.value || '—'}</dd>
            </div>
          ))}
        </dl>
      </section>

      <section className="surface flex flex-wrap items-center justify-between gap-3 p-5" aria-labelledby="settings-password">
        <div>
          <h2 id="settings-password" className="surface-title">
            {t('settings.passwordTitle')}
          </h2>
          <p className="muted">{t('settings.passwordHelp')}</p>
        </div>
        <button type="button" className="btn btn-sm" onClick={() => void session.changePassword()}>
          <FontAwesomeIcon icon={faKey} /> {t('settings.changePassword')}
        </button>
      </section>

      <section className="surface flex flex-col gap-4 p-5" aria-labelledby="settings-preferences">
        <header className="flex flex-wrap items-center justify-between gap-3">
          <h2 id="settings-preferences" className="surface-title">
            {t('settings.preferencesTitle')}
          </h2>
          <div className="flex items-center gap-2">
            <LanguageSwitcher />
            <ThemeToggle />
          </div>
        </header>
        <DisplayCurrencySetting />
      </section>
    </div>
  )
}
