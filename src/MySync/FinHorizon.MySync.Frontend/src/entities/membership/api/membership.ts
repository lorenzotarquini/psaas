import { useQuery } from '@tanstack/react-query'
import { apiGet, apiPost } from '@/shared/api'

/** UC17-R3. */
export type MembershipStatus = 'Active' | 'PastDue' | 'Expired'

// Mirrors MembershipView in FinHorizon.MySync.Application.
export interface Membership {
  status: MembershipStatus
  /** UC17-R4: false = MySync is read-only. */
  canWrite: boolean
  price: number
  currency: string
  interval: 'Month'
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

export const membershipQueryKey = ['membership'] as const

export const useMembership = () => useQuery({ queryKey: membershipQueryKey, queryFn: ({ signal }) => apiGet<Membership>('/api/membership', signal) })

/** UC17 step 2: Stripe's payment page. */
export const startCheckout = () => apiPost<{ url: string }>('/api/membership/checkout', {})

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
