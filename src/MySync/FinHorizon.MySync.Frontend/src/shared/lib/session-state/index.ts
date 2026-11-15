export { useSessionState } from './useSessionState'
export type { StateStorage } from './useSessionState'

/** Every key the app keeps in the browser storage. */
export const storageKeys = {
  /** sessionStorage (UC12-R2). */
  showStable: 'mysync.showStable',
  /** sessionStorage (UC12-R2). */
  showInvestments: 'mysync.showInvestments',
  /** sessionStorage (UC12-R2). */
  amountsHidden: 'mysync.amountsHidden',
  /** localStorage: a layout choice of the device. */
  sidebarExpanded: 'mysync.sidebarExpanded',
} as const
