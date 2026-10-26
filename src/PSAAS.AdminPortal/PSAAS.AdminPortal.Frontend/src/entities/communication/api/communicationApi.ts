import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiGet, apiPost, type PagedResult } from '@/shared/api'

// Mirrors PSAAS.Application/Contracts/Communications/CommunicationModels.cs (UC27, UC28).

export type Channel = 'InApp' | 'Email'
export type EmailStatus = 'Sent' | 'Failed' | 'Skipped'

/** Filters AND-ed, values OR-ed; tenantIds (a selection) wins over the filters. */
export interface CommunicationSegment {
  statuses?: string[]
  planIds?: string[]
  moduleId?: string
  tenantIds?: string[]
}

export interface Communication {
  id: string
  subject: string
  body: string
  channels: Channel[]
  segment: CommunicationSegment
  visibleUntil: string | null
  sentAt: string
  sentBy: string
  recipientCount: number
  emailsSent: number
  emailsFailed: number
  recipients: { tenantId: string; tenantName: string; email: string; emailStatus: EmailStatus | null; error: string | null }[]
}

export interface CommunicationRequest {
  subject: string
  body: string
  channels: Channel[]
  segment: CommunicationSegment
  visibleUntil: string | null
}

export interface Notification {
  kind: 'module.failed' | 'invoice.overdue' | 'trial.ending' | 'users.limit' | 'users.invites' | 'draft.stalled'
  severity: 'critical' | 'warning' | 'info'
  tenantId: string
  tenantName: string
  subject: string | null
  objectId: string | null
  at: string
  value: number | null
  limit: number | null
}

export const useCommunications = (page: number, tenantId?: string) =>
  useQuery({
    queryKey: ['communications', 'list', page, tenantId],
    queryFn: ({ signal }) =>
      apiGet<PagedResult<Communication>>(`/api/communications?page=${page}&pageSize=50${tenantId ? `&tenantId=${tenantId}` : ''}`, signal),
    placeholderData: keepPreviousData,
  })

export const useCommunication = (id: string | null) =>
  useQuery({
    queryKey: ['communications', 'detail', id],
    queryFn: ({ signal }) => apiGet<Communication>(`/api/communications/${encodeURIComponent(id!)}`, signal),
    enabled: Boolean(id),
  })

/** UC27 step 3: recount as the segment changes; a segment that reaches nobody answers 400, shown as 0. */
export const useCommunicationPreview = (segment: CommunicationSegment) =>
  useQuery({
    queryKey: ['communications', 'preview', segment],
    queryFn: () => apiPost<{ count: number; sample: string[] }>('/api/communications/preview', segment).catch(() => ({ count: 0, sample: [] })),
    placeholderData: keepPreviousData,
  })

export function useSendCommunication() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: CommunicationRequest) => apiPost<Communication>('/api/communications', body),
    onSuccess: () => {
      for (const key of [['communications'], ['audit']]) void queryClient.invalidateQueries({ queryKey: key })
    },
  })
}

// The bell is rechecked every minute; a solved item disappears on the next check (UC28-R2).
export const useNotifications = () =>
  useQuery({ queryKey: ['notifications'], queryFn: ({ signal }) => apiGet<Notification[]>('/api/notifications', signal), refetchInterval: 60_000 })
