import { keepPreviousData, useMutation, useQuery, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { apiDelete, apiGet, apiPost, apiPut, type PagedResult } from '@/shared/api'

/** EXP-04 statuses (UC13). */
export type TenantStatus = 'Onboarding' | 'Trial' | 'Active' | 'Suspended' | 'Cancelling' | 'Ceased'

export interface TenantLifecycle {
  status: TenantStatus
  reason: string | null
  changedAt: string
  createdAt: string
}

// Mirrors TenantSummaryDto in PSAAS.AdminPortal.Backend.
export interface TenantSummary {
  id: string
  companyName: string
  vatNumber: string
  completeVatNumber: string
  certifiedEmail: string
  city: string
  code: string
  status: TenantStatus
  createdAt: string
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
  /** userCount: users with the profile (UC2-R2), null when Keycloak could not count them. */
  profiles: { id: string; name: string; description: string | null; userCount: number | null }[]
  productiveUnits: { id: string; name: string; description: string | null; profileIds: string[] }[]
  /** Short code next to the VAT number, e.g. tn_8f2c41a9. */
  code: string
  lifecycle: TenantLifecycle
  sdiCode: string | null
  accountManager: string | null
}

/** UC3/UC4 registry fields; SDI code and account manager are optional (UC14). */
export interface TenantRegistryRequest {
  companyName: string
  completeVatNumber: string
  certifiedEmail: string
  street: string
  city: string
  postalCode: string
  sdiCode: string | null
  accountManager: string | null
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
  /** UC14: save as a draft (Onboarding) to complete step by step. */
  draft?: boolean
}

/** UC11: the fields that can be checked one at a time. */
export type UniqueTenantField = 'companyName' | 'completeVatNumber' | 'certifiedEmail'

/** A tenant to name and link to, e.g. the one already using a unique value (UC11-R4). */
export interface TenantReference {
  id: string
  companyName: string
}

export interface FieldValidation {
  /** null when valid, otherwise the code saving would return (NotUnique, InvalidFormat…). */
  errorCode: string | null
  conflictingTenant: TenantReference | null
}

/** UC11: checks one unique field with the rules of saving. */
export function validateTenantField(field: UniqueTenantField, value: string, excludeTenantId?: string) {
  const query = new URLSearchParams({ field, value })
  if (excludeTenantId) query.set('excludeTenantId', excludeTenantId)
  return apiGet<FieldValidation>(`/api/tenants/field-validation?${query}`)
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

export const useTenant = (id: string, enabled = true) =>
  useQuery({ queryKey: keys.detail(id), queryFn: ({ signal }) => getTenant(id, signal), enabled: enabled && Boolean(id) })

// Every save returns the updated detail: cache it, and refresh what may show stale names or profiles.
function onTenantSaved(queryClient: QueryClient, tenant: TenantDetail) {
  queryClient.setQueryData(keys.detail(tenant.id), tenant)
  void queryClient.invalidateQueries({ queryKey: keys.list })
  void queryClient.invalidateQueries({ queryKey: ['profiles'] })
  void queryClient.invalidateQueries({ queryKey: ['users'] })
  void queryClient.invalidateQueries({ queryKey: ['audit'] })
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

/** UC13: change status (with a reason when suspending, giving notice or ceasing); UC14: activate a draft. */
export function useChangeTenantStatus() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...body }: { id: string; status: TenantStatus; reason?: string }) =>
      apiPost<TenantDetail>(`/api/tenants/${encodeURIComponent(id)}/status`, body),
    onSuccess: (tenant) => onTenantSaved(queryClient, tenant),
  })
}

/** UC14: abandon a draft; it disappears with its organization. */
export function useDiscardDraft() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => apiDelete(`/api/tenants/${encodeURIComponent(id)}`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: keys.list })
      void queryClient.invalidateQueries({ queryKey: ['audit'] })
    },
  })
}
