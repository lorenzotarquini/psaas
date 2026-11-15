import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

export function Loading() {
  const { t } = useTranslation()
  return <span className="loading loading-spinner loading-md text-primary" role="status" aria-label={t('loading')} />
}

export function LoadError() {
  const { t } = useTranslation()
  return (
    <p role="alert" className="alert alert-error alert-soft">
      {t('errors.loadFailed')}
    </p>
  )
}

export function EmptyState({ children }: { children: ReactNode }) {
  return <div className="muted rounded-box border border-dashed border-base-300 px-4 py-6 text-center">{children}</div>
}
