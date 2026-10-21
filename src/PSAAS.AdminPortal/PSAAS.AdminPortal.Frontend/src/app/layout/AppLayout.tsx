import { useTranslation } from 'react-i18next'
import { Outlet } from 'react-router'
import { Header } from '@/widgets/header'
import { Sidebar } from '@/widgets/sidebar'

const drawerId = 'app-drawer'

// daisyUI drawer: checkbox open = sidebar expanded (desktop) or visible (mobile).
export function AppLayout() {
  const { t } = useTranslation()

  return (
    <div className="drawer lg:drawer-open min-h-screen bg-base-200">
      <input
        id={drawerId}
        type="checkbox"
        className="drawer-toggle"
        defaultChecked={window.matchMedia('(min-width: 64rem)').matches}
      />
      <div className="drawer-content flex flex-col">
        <Header drawerId={drawerId} />
        <main className="p-6">
          <Outlet />
        </main>
      </div>
      <div className="drawer-side is-drawer-close:overflow-visible">
        <label htmlFor={drawerId} aria-label={t('menu')} className="drawer-overlay" />
        <Sidebar drawerId={drawerId} />
      </div>
    </div>
  )
}
