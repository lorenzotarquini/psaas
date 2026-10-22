import { faCompass } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useBreadcrumb } from '@/shared/lib/breadcrumb'
import { routes } from '@/shared/routes'

export function NotFoundPage() {
  const { t } = useTranslation()
  useBreadcrumb([{ label: t('errors.pageNotFound') }])

  return (
    <div className="panel mx-auto mt-10 flex max-w-md flex-col items-center gap-3 px-6 py-10 text-center">
      <FontAwesomeIcon icon={faCompass} className="text-3xl text-base-content/40" />
      <h1 className="text-[18px] font-semibold">{t('errors.pageNotFound')}</h1>
      <p className="text-base-content/60">{t('errors.pageNotFoundHint')}</p>
      <Link to={routes.home} className="btn btn-primary btn-sm">
        {t('nav.home')}
      </Link>
    </div>
  )
}
