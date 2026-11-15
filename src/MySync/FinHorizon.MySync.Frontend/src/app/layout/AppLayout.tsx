import { useTranslation } from 'react-i18next'
import { Outlet } from 'react-router'
import { logout } from '@/shared/auth'

// Minimal shell: the navigation will grow with the first MySync features.
export function AppLayout() {
  const { t } = useTranslation()
  const environment = import.meta.env.VITE_ENVIRONMENT_LABEL

  return (
    <div className="flex min-h-screen flex-col">
      <header className="navbar gap-3 border-b border-base-300 bg-base-100 px-4 md:px-7">
        <span className="flex-1 text-[15px] font-semibold">{t('appName')}</span>
        {environment && <span className="badge badge-warning badge-sm">{environment}</span>}
        <button type="button" className="btn btn-sm" onClick={() => void logout()}>
          {t('logout')}
        </button>
      </header>
      <main className="flex flex-1 flex-col gap-5 px-4 py-6 md:px-7">
        <Outlet />
      </main>
    </div>
  )
}
