import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { useLocation } from 'react-router'

/** One step of the topbar breadcrumb; the last one is the current page and has no link. */
export interface Crumb {
  label: string
  to?: string
}

interface PageCrumbs {
  pathname: string
  crumbs: Crumb[]
}

const CrumbsContext = createContext<PageCrumbs>({ pathname: '', crumbs: [] })
const SetCrumbsContext = createContext<(value: PageCrumbs) => void>(() => {})

// Pages declare where they are (through PageHeader); the topbar of the layout shows it.
export function BreadcrumbProvider({ children }: { children: ReactNode }) {
  const [value, setValue] = useState<PageCrumbs>({ pathname: '', crumbs: [] })
  return (
    <SetCrumbsContext.Provider value={setValue}>
      <CrumbsContext.Provider value={value}>{children}</CrumbsContext.Provider>
    </SetCrumbsContext.Provider>
  )
}

/** Crumbs of the current page only: a page that declares none must not show the previous page's. */
export function useCrumbs() {
  const { pathname, crumbs } = useContext(CrumbsContext)
  return pathname === useLocation().pathname ? crumbs : []
}

export function useBreadcrumb(crumbs: Crumb[]) {
  const setValue = useContext(SetCrumbsContext)
  const { pathname } = useLocation()
  // Compared by content: callers build a new array on every render.
  const key = JSON.stringify(crumbs)
  useEffect(() => {
    setValue({ pathname, crumbs: JSON.parse(key) as Crumb[] })
  }, [key, pathname, setValue])
}
