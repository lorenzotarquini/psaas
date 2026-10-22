import { useTranslation } from 'react-i18next'
import { Outlet } from 'react-router'
import { BreadcrumbProvider } from '@/shared/lib/breadcrumb'
import { Header } from '@/widgets/header'
import { Sidebar } from '@/widgets/sidebar'

const drawerId = 'app-drawer'

// daisyUI drawer: always open on desktop (screen 1b), a slide-over on mobile.
export function AppLayout() {
  const { t } = useTranslation()

  return (
    <BreadcrumbProvider>
      <div className="drawer lg:drawer-open min-h-screen">
        <input id={drawerId} type="checkbox" className="drawer-toggle" />
        <div className="drawer-content flex min-w-0 flex-col">
          <Header drawerId={drawerId} />
          <main className="flex flex-1 flex-col gap-5 px-4 py-6 md:px-7">
            <Outlet />
          </main>
        </div>
        <div className="drawer-side z-30">
          <label htmlFor={drawerId} aria-label={t('menu')} className="drawer-overlay" />
          <Sidebar />
        </div>
      </div>
    </BreadcrumbProvider>
  )
}
