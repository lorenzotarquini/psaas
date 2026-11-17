import { useQuery } from '@tanstack/react-query'
import { apiDelete, apiGet, apiPost, apiPut, ledgerKey } from '@/shared/api'

// Mirrors SubscriptionView in FinHorizon.MySync.Application.
export interface Subscription {
  id: string
  name: string
  description: string | null
  accountId: string
  accountName: string
  categoryId: string
  categoryName: string
  /** Every month, in `currency`, the one of the account (UC16-R2). */
  amount: number
  currency: string
  /** `amount` in the display currency with today's rate (R16). */
  displayAmount: number
  firstChargeOn: string
  /** The last day, when cancelled (R13). */
  endsOn: string | null
  createdOn: string
  /** Not ended before today: it counts in the monthly total. */
  active: boolean
  nextChargeOn: string | null
  /** UC16-R7: the charge to register, if any. */
  dueOn: string | null
  hasPayments: boolean
}

export interface SubscriptionList {
  items: Subscription[]
  /** The active subscriptions, in `currency` (R16). */
  monthlyTotal: number
  currency: string
  /** UC16-R4: open subscriptions only; 0 turns subscriptions off. */
  max: number
}

export interface SubscriptionInput {
  name: string
  description?: string
  accountId: string
  categoryId: string
  amount: number
  firstChargeOn: string
}

/** Under the ledger key: a registered expense changes what is due. */
export const subscriptionsQueryKey = [ledgerKey, 'subscriptions'] as const

export const useSubscriptions = () =>
  useQuery({ queryKey: subscriptionsQueryKey, queryFn: ({ signal }) => apiGet<SubscriptionList>('/api/subscriptions', signal) })

export const createSubscription = (input: SubscriptionInput) => apiPost<Subscription>('/api/subscriptions', input)

export const updateSubscription = (id: string, input: SubscriptionInput) => apiPut<Subscription>(`/api/subscriptions/${id}`, input)

/** UC16-R13: the end can be in the future. */
export const cancelSubscription = (id: string, endsOn: string) => apiPost<Subscription>(`/api/subscriptions/${id}/cancel`, { endsOn })

export const resumeSubscription = (id: string) => apiPost<Subscription>(`/api/subscriptions/${id}/resume`, {})

/** UC16-R9: only the charge due now. */
export const skipCharge = (id: string, dueOn: string) => apiPost<Subscription>(`/api/subscriptions/${id}/skip`, { dueOn })

/** UC16-R15: only without linked expenses. */
export const deleteSubscription = (id: string) => apiDelete(`/api/subscriptions/${id}`)

/** Subscriptions with an end date are cancelled, even while still active until it. */
export const isCancelled = (subscription: Pick<Subscription, 'endsOn'>) => subscription.endsOn !== null

/** UC16-R4: the open subscriptions, the only ones in the limit. */
export const openCount = (list: SubscriptionList) => list.items.filter((item) => !isCancelled(item)).length
