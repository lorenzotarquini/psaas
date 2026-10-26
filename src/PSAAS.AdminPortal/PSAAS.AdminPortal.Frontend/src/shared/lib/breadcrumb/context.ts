import { createContext, useContext, useEffect } from 'react'
import { useLocation } from 'react-router'

/** One step of the topbar breadcrumb; the last one is the current page and has no link. */
export interface Crumb {
  label: string
  to?: string
}

export interface PageCrumbs {
  pathname: string
  crumbs: Crumb[]
  /** Short status next to the breadcrumb, e.g. "Bozza salvata alle 10:42" (mock 1e). */
  note?: string
}

export const CrumbsContext = createContext<PageCrumbs>({ pathname: '', crumbs: [] })
export const SetCrumbsContext = createContext<(value: PageCrumbs) => void>(() => {})

/** Crumbs (and note) of the current page only: a page that declares none must not show the previous page's. */
export function useCrumbs() {
  const value = useContext(CrumbsContext)
  return value.pathname === useLocation().pathname ? value : { pathname: '', crumbs: [] as Crumb[], note: undefined }
}

export function useBreadcrumb(crumbs: Crumb[], note?: string) {
  const setValue = useContext(SetCrumbsContext)
  const { pathname } = useLocation()
  // Compared by content: callers build a new array on every render.
  const key = JSON.stringify(crumbs)
  useEffect(() => {
    setValue({ pathname, crumbs: JSON.parse(key) as Crumb[], note })
  }, [key, note, pathname, setValue])
}
