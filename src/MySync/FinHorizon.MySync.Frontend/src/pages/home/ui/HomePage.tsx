import { useTranslation } from 'react-i18next'
import { useMe } from '@/entities/me'

// First page: proves the whole chain (Keycloak login, token, MySync backend) works.
export function HomePage() {
  const { t } = useTranslation()
  const { data: me, error, isPending } = useMe()

  return (
    <section className="panel flex flex-col gap-2 px-5 py-4">
      <h1 className="text-[22px] font-semibold">{t('home.title')}</h1>
      {isPending ? (
        <span className="loading loading-spinner" aria-label={t('loading')} />
      ) : error ? (
        <p role="alert" className="text-error">{t('errors.loadFailed')}</p>
      ) : (
        <p className="text-base-content/70">{t('home.signedInAs', { name: me.name ?? me.email ?? me.keycloakId })}</p>
      )}
      <p className="text-base-content/60">{t('home.empty')}</p>
    </section>
  )
}
