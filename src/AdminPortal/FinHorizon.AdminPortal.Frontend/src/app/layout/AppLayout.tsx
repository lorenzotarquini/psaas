import { useTranslation } from 'react-i18next'
import { Outlet } from 'react-router'
import { BreadcrumbProvider } from '@/shared/lib/breadcrumb'
import { Header } from '@/widgets/header'
import { useNotifications } from '@/entities/communication'
import { CommandPalette } from '@/widgets/command-palette'
import { Sidebar } from '@/widgets/sidebar'

const drawerId = 'app-drawer'

// daisyUI drawer: always open on desktop (screen 1b), a slide-over on mobile.
export function AppLayout() {
  const { t } = useTranslation()
  // UC28: the Panoramica badge of mock 1b is the number of things to handle.
  const { data: notifications } = useNotifications()

  return (
    <BreadcrumbProvider>
      <div className="drawer lg:drawer-open min-h-screen">
        <input id={drawerId} type="checkbox" className="drawer-toggle" />
        <div className="drawer-content flex min-w-0 flex-col">
          <Header drawerId={drawerId} search={<CommandPalette />} />
          <main className="flex flex-1 flex-col gap-5 px-4 py-6 md:px-7">
            <Outlet />
          </main>
        </div>
        <div className="drawer-side z-30">
          <label htmlFor={drawerId} aria-label={t('menu')} className="drawer-overlay" />
          <Sidebar badges={{ home: notifications?.length }} />
        </div>
      </div>
    </BreadcrumbProvider>
  )
}
