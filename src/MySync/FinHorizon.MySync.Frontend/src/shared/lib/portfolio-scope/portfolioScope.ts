import { storageKeys, useSessionState } from '@/shared/lib/session-state'

/** Mirrors PortfolioGroup in FinHorizon.MySync.Domain: every account and debt belongs to one (UC12). */
export type PortfolioGroup = 'Everyday' | 'Stable' | 'Investments'

/** Which optional groups a view shows; day-to-day items are always shown. */
export interface PortfolioScope {
  stable: boolean
  investments: boolean
}

export const inScope = (group: PortfolioGroup, scope: PortfolioScope) =>
  group === 'Everyday' || (group === 'Stable' && scope.stable) || (group === 'Investments' && scope.investments)

/** Query string of the API (`includeStable`, `includeInvestments`). */
export const scopeQuery = (scope: PortfolioScope) => `includeStable=${scope.stable}&includeInvestments=${scope.investments}`

/** The two switches of UC12, off at the start of every working session and shared by dashboard and trend (R2). */
export function usePortfolioScope() {
  const [stable, setStable] = useSessionState(storageKeys.showStable, false)
  const [investments, setInvestments] = useSessionState(storageKeys.showInvestments, false)
  const scope: PortfolioScope = { stable, investments }
  const setScope = (next: PortfolioScope) => {
    if (next.stable !== stable) setStable(next.stable)
    if (next.investments !== investments) setInvestments(next.investments)
  }
  return [scope, setScope] as const
}
