import { useMutation, useQuery, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { apiGet, apiPost, apiPut } from '@/shared/api'

// Mirror the records of PSAAS.Application/Contracts/Catalog/CatalogModels.cs.

export type ModuleStatus = 'Beta' | 'Available' | 'Deprecated'
export type BillingPeriod = 'Monthly' | 'Yearly'
export type SubscriptionStatus = 'Trial' | 'Active' | 'Expired' | 'Cancelled'
export type ProvisioningStatus = 'Provisioning' | 'Active' | 'Failed'

/** UC15: a module of the catalog; tenantCount = tenants that have it. */
export interface Module {
  id: string
  name: string
  description: string | null
  icon: string | null
  status: ModuleStatus
  version: string
  isCore: boolean
  requires: string[]
  permissions: string[]
  provisionUrl: string | null
  updatedAt: string
  tenantCount: number
}

/** The manifest a module ships with (§6.2 of ADMIN-PORTAL.md). */
export interface ModuleManifest {
  id: string
  name: string
  description?: string | null
  version: string
  icon?: string | null
  status?: ModuleStatus | null
  requires?: string[]
  permissions?: string[]
  provisionUrl?: string | null
  deprovisionUrl?: string | null
}

/** UC16: maxUsers/maxProductiveUnits null = unlimited. */
export interface Plan {
  id: string
  code: string
  name: string
  description: string | null
  price: number
  billingPeriod: BillingPeriod
  trialDays: number
  maxUsers: number | null
  maxProductiveUnits: number | null
  moduleIds: string[]
  active: boolean
  subscriberCount: number
}

export type PlanRequest = Omit<Plan, 'id' | 'subscriberCount'>

export interface TenantModule {
  moduleId: string
  name: string
  icon: string | null
  version: string
  status: ProvisioningStatus
  attempts: number
  lastError: string | null
  isAddOn: boolean
  updatedAt: string
}

/** UC17: price, period and limits are copied from the plan when subscribing (UC17-R1). */
export interface Subscription {
  tenantId: string
  tenantName: string
  planId: string
  planName: string
  status: SubscriptionStatus
  price: number
  billingPeriod: BillingPeriod
  maxUsers: number | null
  maxProductiveUnits: number | null
  startsAt: string
  trialEndsAt: string | null
  renewsAt: string
  autoRenew: boolean
  addOnModuleIds: string[]
  modules: TenantModule[]
}

export interface SubscriptionRequest {
  planId: string | null
  trial: boolean
  addOnModuleIds: string[]
  autoRenew: boolean
}

const keys = {
  modules: ['modules'] as const,
  plans: ['plans'] as const,
  subscriptions: ['subscriptions'] as const,
  tenant: (tenantId: string) => ['subscriptions', 'tenant', tenantId] as const,
}

export const useModules = () => useQuery({ queryKey: keys.modules, queryFn: ({ signal }) => apiGet<Module[]>('/api/modules', signal) })

export const usePlans = () => useQuery({ queryKey: keys.plans, queryFn: ({ signal }) => apiGet<Plan[]>('/api/plans', signal) })

export const useSubscriptions = () =>
  useQuery({ queryKey: keys.subscriptions, queryFn: ({ signal }) => apiGet<Subscription[]>('/api/subscriptions', signal) })

/** undefined data (204) = the tenant has no subscription yet. */
export const useTenantSubscription = (tenantId: string | undefined) =>
  useQuery({
    queryKey: keys.tenant(tenantId ?? ''),
    queryFn: async ({ signal }) => (await apiGet<Subscription | undefined>(`/api/tenants/${encodeURIComponent(tenantId!)}/subscription`, signal)) ?? null,
    enabled: Boolean(tenantId),
  })

// Catalog changes move counts on plans, modules and subscriptions: refresh them all, and the audit.
function onCatalogSaved(queryClient: QueryClient) {
  for (const key of [keys.modules, keys.plans, keys.subscriptions, ['audit'], ['tenants']]) void queryClient.invalidateQueries({ queryKey: key })
}

export function useRegisterModule() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (manifest: ModuleManifest) => apiPost<Module>('/api/modules', manifest),
    onSuccess: () => onCatalogSaved(queryClient),
  })
}

export function useSetModuleStatus() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: ModuleStatus }) =>
      apiPut<Module>(`/api/modules/${encodeURIComponent(id)}/status`, { status }),
    onSuccess: () => onCatalogSaved(queryClient),
  })
}

export function useSavePlan() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, plan }: { id: string | null; plan: PlanRequest }) =>
      id ? apiPut<Plan>(`/api/plans/${encodeURIComponent(id)}`, plan) : apiPost<Plan>('/api/plans', plan),
    onSuccess: () => onCatalogSaved(queryClient),
  })
}

export function useSubscribe(tenantId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: SubscriptionRequest) => apiPut<Subscription>(`/api/tenants/${encodeURIComponent(tenantId)}/subscription`, body),
    onSuccess: (subscription) => {
      queryClient.setQueryData(keys.tenant(tenantId), subscription)
      onCatalogSaved(queryClient)
    },
  })
}

export function useRetryProvisioning(tenantId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (moduleId: string) =>
      apiPost<Subscription>(`/api/tenants/${encodeURIComponent(tenantId)}/modules/${encodeURIComponent(moduleId)}/retry`, undefined),
    onSuccess: (subscription) => {
      queryClient.setQueryData(keys.tenant(tenantId), subscription)
      onCatalogSaved(queryClient)
    },
  })
}
