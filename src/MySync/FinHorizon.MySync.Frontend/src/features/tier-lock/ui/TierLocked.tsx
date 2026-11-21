import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faLock } from '@fortawesome/free-solid-svg-icons'
import { ApiError } from '@/shared/api'

/** Contract C5 (docs/architettura-moduli.md): the backend refuses a feature outside the paid tier with 403. */
export const isFeatureLocked = (error: unknown) => error instanceof ApiError && error.status === 403

/** C5: the feature is not in the plan the user pays; the settings show the plans that include it. */
export function TierLocked({ compact = false }: { compact?: boolean }) {
  const { t } = useTranslation()
  if (compact) {
    return (
      <span className="muted flex items-center gap-1.5 text-sm">
        <FontAwesomeIcon icon={faLock} aria-hidden /> {t('tierLock.short')}
      </span>
    )
  }

  return (
    <section role="status" className="surface flex flex-col items-start gap-3 p-6">
      <h2 className="surface-title flex items-center gap-2">
        <FontAwesomeIcon icon={faLock} aria-hidden /> {t('tierLock.title')}
      </h2>
      <p className="muted">{t('tierLock.help')}</p>
      <Link to="/settings?tab=billing" className="btn btn-primary btn-sm">
        {t('tierLock.choosePlan')}
      </Link>
    </section>
  )
}
