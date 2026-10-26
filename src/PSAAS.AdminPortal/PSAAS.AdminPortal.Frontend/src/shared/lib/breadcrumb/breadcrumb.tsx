import { useState, type ReactNode } from 'react'
import { CrumbsContext, SetCrumbsContext, type PageCrumbs } from './context'

// Pages declare where they are (through PageHeader); the topbar of the layout shows it.
export function BreadcrumbProvider({ children }: { children: ReactNode }) {
  const [value, setValue] = useState<PageCrumbs>({ pathname: '', crumbs: [] })
  return (
    <SetCrumbsContext.Provider value={setValue}>
      <CrumbsContext.Provider value={value}>{children}</CrumbsContext.Provider>
    </SetCrumbsContext.Provider>
  )
}
