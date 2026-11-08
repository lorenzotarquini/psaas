import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { apiGet, type PagedResult } from '@/shared/api'

// Mirrors AuditEventReadModel (UC12). details depends on the action, see model/describe.ts.
export interface AuditEvent {
  id: string
  occurredAt: string
  actorId: string
  actorName: string
  action: string
  targetType: 'tenant' | 'user' | string
  targetId: string
  targetName: string
  tenantId: string | null
  details: Record<string, unknown> | null
}

export interface AuditQuery {
  tenantId?: string
  /** An action code or its prefix, e.g. "tenant." */
  action?: string
  search?: string
  from?: string
  to?: string
  page?: number
  pageSize?: number
}

const toSearchParams = (query: AuditQuery) =>
  new URLSearchParams(
    Object.entries(query)
      .filter(([, value]) => value !== undefined && value !== '')
      .map(([key, value]) => [key, String(value)]),
  )

export const useAudit = (query: AuditQuery) =>
  useQuery({
    queryKey: ['audit', query],
    queryFn: ({ signal }) => apiGet<PagedResult<AuditEvent>>(`/api/audit?${toSearchParams(query)}`, signal),
    placeholderData: keepPreviousData,
  })
