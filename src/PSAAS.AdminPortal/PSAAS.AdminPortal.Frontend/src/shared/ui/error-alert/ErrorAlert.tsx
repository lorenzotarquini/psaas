import { faTriangleExclamation } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { ApiError } from '@/shared/api'

const messageKeys: Record<number, string> = {
  400: 'errors.validation',
  403: 'errors.forbidden',
  404: 'errors.notFound',
  503: 'errors.unavailable',
}

// 401 never gets here for long: the API client has already started a new login.
export function ErrorAlert({ error }: { error: unknown }) {
  const { t } = useTranslation()
  const key = (error instanceof ApiError && messageKeys[error.status]) || 'errors.generic'

  return (
    <div role="alert" className="alert alert-error">
      <FontAwesomeIcon icon={faTriangleExclamation} />
      <span>{t(key)}</span>
    </div>
  )
}
