import { faTriangleExclamation } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { ApiError } from '@/shared/api'

/** What failed: a 404 means "not found" only when reading; when saving it is a failed save like any other. */
export type ErrorAction = 'load' | 'save'

const messageKeys: Record<ErrorAction, Record<number, string>> = {
  load: { 403: 'errors.forbidden', 404: 'errors.notFound', 503: 'errors.unavailable' },
  save: { 400: 'errors.validation', 403: 'errors.forbidden', 503: 'errors.unavailable' },
}

const fallbackKeys: Record<ErrorAction, string> = { load: 'errors.loadFailed', save: 'errors.saveFailed' }

// 401 never gets here for long: the API client has already started a new login.
export function ErrorAlert({ error, action = 'load' }: { error: unknown; action?: ErrorAction }) {
  const { t } = useTranslation()
  const message =
    error instanceof ApiError
      ? t(messageKeys[action][error.status] ?? fallbackKeys[action], { status: error.status })
      : // fetch rejects with a TypeError when the server cannot be reached.
        t(error instanceof TypeError ? 'errors.network' : 'errors.generic')

  return (
    <div role="alert" className="alert alert-error">
      <FontAwesomeIcon icon={faTriangleExclamation} />
      <span>{message}</span>
    </div>
  )
}
