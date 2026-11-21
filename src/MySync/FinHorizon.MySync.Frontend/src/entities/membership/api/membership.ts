import { useQuery } from '@tanstack/react-query'
import { apiGet, apiPost } from '@/shared/api'

/** UC17-R3. */
export type MembershipStatus = 'Active' | 'PastDue' | 'Expired'

/** Contract C2 (docs/architettura-moduli.md): a level of MySync, with its features and limits. */
export interface Tier {
  level: number
  name: string
  features: string[]
  limits: Record<string, number>
}

// Mirrors MembershipView in FinHorizon.MySync.Application.
export interface Membership {
  status: MembershipStatus
  /** UC17-R4: false = MySync is read-only. */
  canWrite: boolean
  /** The plan of the current or last subscription, as when the user subscribed. */
  plan: { name: string; amount: number; currency: string; interval: 'Month' | 'Year'; tier: number | null } | null
  /** The tier paid (C3); null when nothing is paid. */
  tier: Tier | null
  currentPeriodEnd: string | null
  cancelAtPeriodEnd: boolean
  card: { brand: string; last4: string; expMonth: number; expYear: number } | null
  /** The last payment that did not go through: Stripe's decline code (e.g. insufficient_funds). */
  lastFailure: { code: string | null; message: string | null; failedAt: string } | null
  /** Stripe's page to pay the open invoice, while Stripe retries. */
  payUrl: string | null
  /** A Stripe customer exists: the portal can open. */
  hasCustomer: boolean
}

/** UC17 step 2: a plan of the MySync module, defined in the Admin Portal (UC16). */
export interface MembershipPlan {
  code: string
  name: string
  description: string | null
  tier: Tier | null
  price: number
  currency: string
  interval: 'Month' | 'Year'
  priceIncludesVat: boolean
  vatRate: number | null
  trialDays: number
  /** UC17-R10: the running subscription can move to this plan (a higher tier). */
  canUpgradeTo: boolean
}

export const membershipQueryKey = ['membership'] as const

export const useMembership = () => useQuery({ queryKey: membershipQueryKey, queryFn: ({ signal }) => apiGet<Membership>('/api/membership', signal) })

export const useMembershipPlans = (enabled: boolean) =>
  useQuery({ queryKey: ['membership', 'plans'], queryFn: ({ signal }) => apiGet<MembershipPlan[]>('/api/membership/plans', signal), enabled })

/** UC17 step 2: Stripe's payment page for the chosen plan. */
export const startCheckout = (planCode: string) => apiPost<{ url: string }>('/api/membership/checkout', { planCode })

/** UC17 flow 1b: to a plan of a higher tier; the difference is charged at once on the card of the subscription. */
export const changePlan = (planCode: string) => apiPost<Membership>('/api/membership/change', { planCode })

/** UC17 flow 1a: Stripe's customer portal. */
export const openBillingPortal = () => apiPost<{ url: string }>('/api/membership/portal', {})

/** UC17 step 4: back from Stripe, read the state at once. */
export const refreshMembership = () => apiPost<Membership>('/api/membership/refresh', {})

/** The browser leaves MySync for a page of Stripe. A function so the tests can replace it. */
export const goTo = {
  url: (url: string) => {
    window.location.assign(url)
  },
}
