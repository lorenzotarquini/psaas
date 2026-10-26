import { useQuery } from '@tanstack/react-query'
import { apiGet } from '@/shared/api'

// Mirrors PSAAS.Application/Contracts/Usage/UsageModels.cs (UC25) and Services/Health (UC26).

export interface UsageSeries {
  moduleId: string
  moduleName: string
  metric: string
  kind: 'counter' | 'gauge'
  /** Sum over the period for a counter, last value for a gauge. */
  total: number
  points: { day: string; value: number }[]
}

/** null counts: Keycloak could not count the users. */
export interface TenantUsage {
  days: number
  users: number | null
  maxUsers: number | null
  activeUsers: number | null
  productiveUnits: number
  maxProductiveUnits: number | null
  metrics: UsageSeries[]
}

export type ServiceStatus = 'Up' | 'Degraded' | 'Down' | 'NotConfigured'

export interface ServicesHealth {
  status: ServiceStatus
  checkedAt: string
  services: { key: string; status: ServiceStatus; latencyMs: number; detail: string | null }[]
}

export const useTenantUsage = (tenantId: string, days: number) =>
  useQuery({
    queryKey: ['usage', tenantId, days],
    queryFn: ({ signal }) => apiGet<TenantUsage>(`/api/tenants/${encodeURIComponent(tenantId)}/usage?days=${days}`, signal),
  })

// Rechecked every minute while a page shows it.
export const useServicesHealth = () =>
  useQuery({
    queryKey: ['system', 'services'],
    queryFn: ({ signal }) => apiGet<ServicesHealth>('/api/system/services', signal),
    refetchInterval: 60_000,
  })
