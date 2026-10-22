import type { Module, Plan, Subscription, SubscriptionRequest } from '@/entities/catalog'

export type SubscriptionChoice = SubscriptionRequest

/** Add-ons: modules the plan does not include, minus the core and the deprecated ones (UC15-R1, R4). */
export const addOnCandidates = (plan: Plan | undefined, modules: Module[]) =>
  plan ? modules.filter((m) => !m.isCore && m.status !== 'Deprecated' && !plan.moduleIds.includes(m.id)) : []

/** Nothing to save: no plan chosen, or the same plan, add-ons and renewal as now. */
export function isUnchanged(choice: SubscriptionChoice, current: Subscription | null) {
  if (!choice.planId) return true
  if (!current) return false
  const same = (a: string[], b: string[]) => a.length === b.length && a.every((x) => b.includes(x))
  return choice.planId === current.planId && choice.autoRenew === current.autoRenew && same(choice.addOnModuleIds, current.addOnModuleIds)
}
