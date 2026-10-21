import { keepPreviousData, useMutation, useQueries, useQuery, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { apiGet, apiPost, apiPut, type PagedResult } from '@/shared/api'

// Mirrors TenantSummaryDto in PSAAS.AdminPortal.Backend.
export interface TenantSummary {
  id: string
  companyName: string
  vatNumber: string
  completeVatNumber: string
  certifiedEmail: string
  city: string
}

export interface TenantsQuery {
  search: string
  page: number
  pageSize: number
}

// Mirrors TenantDetailDto in PSAAS.AdminPortal.Backend. Profiles and units come ordered by name.
export interface TenantDetail {
  id: string
  companyName: string
  vatNumber: string
  completeVatNumber: string
  certifiedEmail: string
  address: { street: string; city: string; postalCode: string; country: string }
  profiles: { id: string; name: string; description: string | null }[]
  productiveUnits: { id: string; name: string; description: string | null; profileIds: string[] }[]
}

/** UC3/UC4 registry fields. */
export interface TenantRegistryRequest {
  companyName: string
  completeVatNumber: string
  certifiedEmail: string
  street: string
  city: string
  postalCode: string
}

/** UC5 body: replaces the whole organization. id null = new; key links units to the profiles of this request. */
export interface OrganizationRequest {
  profiles: { id: string | null; key: string; name: string; description: string | null }[]
  productiveUnits: { id: string | null; name: string; description: string | null; profileKeys: string[] }[]
}

/** UC3 body: registry plus an optional organization (no ids, everything is new). */
export type TenantCreateRequest = TenantRegistryRequest & {
  profiles: Omit<OrganizationRequest['profiles'][number], 'id'>[]
  productiveUnits: Omit<OrganizationRequest['productiveUnits'][number], 'id'>[]
}

/** UC11: the fields that can be checked one at a time. */
export type UniqueTenantField = 'companyName' | 'completeVatNumber' | 'certifiedEmail'

/** UC11: null when valid, otherwise the code saving would return (NotUnique, InvalidFormat…). */
export async function validateTenantField(field: UniqueTenantField, value: string, excludeTenantId?: string) {
  const query = new URLSearchParams({ field, value })
  if (excludeTenantId) query.set('excludeTenantId', excludeTenantId)
  const { errorCode } = await apiGet<{ errorCode: string | null }>(`/api/tenants/field-validation?${query}`)
  return errorCode
}

const keys = {
  list: ['tenants', 'list'] as const,
  detail: (id: string) => ['tenants', 'detail', id] as const,
}

const getTenants = ({ search, page, pageSize }: TenantsQuery, signal: AbortSignal) => {
  const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  if (search) query.set('search', search)
  return apiGet<PagedResult<TenantSummary>>(`/api/tenants?${query}`, signal)
}

const getTenant = (id: string, signal: AbortSignal) =>
  apiGet<TenantDetail>(`/api/tenants/${encodeURIComponent(id)}`, signal)

// keepPreviousData: the table keeps the current page while the next one loads.
export const useTenants = (query: TenantsQuery) =>
  useQuery({
    queryKey: [...keys.list, query],
    queryFn: ({ signal }) => getTenants(query, signal),
    placeholderData: keepPreviousData,
  })

export const useTenant = (id: string) =>
  useQuery({ queryKey: keys.detail(id), queryFn: ({ signal }) => getTenant(id, signal) })

/**
 * Company names by tenant id, for pages that only receive tenantId (GET /api/profiles).
 * shortcut: one detail request per distinct tenant; drop it once /api/profiles returns tenantCompanyName.
 */
export function useTenantNames(ids: string[]): Map<string, string> {
  const distinct = [...new Set(ids)]
  const results = useQueries({
    queries: distinct.map((id) => ({ queryKey: keys.detail(id), queryFn: ({ signal }: { signal: AbortSignal }) => getTenant(id, signal) })),
  })
  return new Map(distinct.flatMap((id, i) => (results[i].data ? [[id, results[i].data.companyName]] : [])))
}

// Every save returns the updated detail: cache it, and refresh what may show stale names or profiles.
function onTenantSaved(queryClient: QueryClient, tenant: TenantDetail) {
  queryClient.setQueryData(keys.detail(tenant.id), tenant)
  void queryClient.invalidateQueries({ queryKey: keys.list })
  void queryClient.invalidateQueries({ queryKey: ['profiles'] })
  void queryClient.invalidateQueries({ queryKey: ['users'] })
}

export function useCreateTenant() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: TenantCreateRequest) => apiPost<TenantDetail>('/api/tenants', body),
    onSuccess: (tenant) => onTenantSaved(queryClient, tenant),
  })
}

export function useUpdateTenantRegistry(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: TenantRegistryRequest) => apiPut<TenantDetail>(`/api/tenants/${encodeURIComponent(id)}`, body),
    onSuccess: (tenant) => onTenantSaved(queryClient, tenant),
  })
}

export function useUpdateTenantOrganization(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: OrganizationRequest) =>
      apiPut<TenantDetail>(`/api/tenants/${encodeURIComponent(id)}/organization`, body),
    onSuccess: (tenant) => onTenantSaved(queryClient, tenant),
  })
}
