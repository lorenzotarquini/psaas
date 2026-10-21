import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { getMe, MeDetails, type Me } from '@/entities/me'
import { ApiError } from '@/shared/api'
import { Header } from '@/widgets/header'

export function HomePage() {
  const { t } = useTranslation()
  const [me, setMe] = useState<Me>()
  const [errorStatus, setErrorStatus] = useState<number>()

  useEffect(() => {
    const controller = new AbortController()
    getMe(controller.signal)
      .then(setMe)
      .catch((error: unknown) => {
        if (!controller.signal.aborted) setErrorStatus(error instanceof ApiError ? error.status : 0)
      })
    return () => controller.abort()
  }, [])

  return (
    <div className="min-h-screen bg-base-200">
      <Header />
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
              <MeDetails me={me} />
            )}
          </div>
        </div>
      </main>
    </div>
  )
}
