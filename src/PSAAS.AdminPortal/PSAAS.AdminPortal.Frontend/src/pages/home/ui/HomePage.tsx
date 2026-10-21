import { useTranslation } from 'react-i18next'
import { MeDetails, useMe } from '@/entities/me'
import { ErrorAlert } from '@/shared/ui/error-alert'

export function HomePage() {
  const { t } = useTranslation()
  const { data: me, error, isPending } = useMe()

  return (
    <div className="card bg-base-100 shadow-sm max-w-xl">
      <div className="card-body">
        <h1 className="card-title">{t('me.title')}</h1>
        {error ? (
          <ErrorAlert error={error} />
        ) : isPending ? (
          <span className="loading loading-spinner" aria-label={t('loading')} />
        ) : (
          <MeDetails me={me} />
        )}
      </div>
    </div>
  )
}
