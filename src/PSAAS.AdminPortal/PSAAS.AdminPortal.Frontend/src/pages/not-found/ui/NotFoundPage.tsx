import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { routes } from '@/shared/routes'

export function NotFoundPage() {
  const { t } = useTranslation()

  return (
    <div className="card bg-base-100 shadow-sm max-w-xl">
      <div className="card-body">
        <h1 className="card-title">{t('errors.pageNotFound')}</h1>
        <Link to={routes.home} className="link link-primary">
          {t('nav.home')}
        </Link>
      </div>
    </div>
  )
}
