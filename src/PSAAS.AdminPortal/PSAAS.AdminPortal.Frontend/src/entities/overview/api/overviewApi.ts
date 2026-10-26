import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiGet, apiPost } from '@/shared/api'

// Mirrors PSAAS.Application/Contracts/Overview/OverviewModels.cs (UC29–UC31).

export interface OverviewKpis {
  activeTenants: number
  activeTenantsThisMonth: number
  trialTenants: number
  trialsEndingSoon: number
  users: number | null
  usersThisMonth: number | null
  modules: number
  modulesInBeta: number
}

export interface SearchResult {
  tenants: { id: string; companyName: string; completeVatNumber: string; certifiedEmail: string; status: string }[]
  /** The fields of UC7 the palette shows. */
  users: { id: string; firstName: string; lastName: string; email: string }[]
  usersUnavailable: boolean
}

export const useOverview = () =>
  useQuery({ queryKey: ['overview'], queryFn: ({ signal }) => apiGet<OverviewKpis>('/api/overview', signal) })

/** UC30: only from 2 characters (shorter text is not sent). */
export const useGlobalSearch = (text: string) =>
  useQuery({
    queryKey: ['search', text],
    queryFn: ({ signal }) => apiGet<SearchResult>(`/api/search?q=${encodeURIComponent(text)}`, signal),
    enabled: text.trim().length >= 2,
    placeholderData: keepPreviousData,
  })

export function useResendInvites() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => apiPost<{ resent: number }>('/api/users/invites/resend', undefined),
    onSuccess: () => {
      for (const key of [['notifications'], ['users'], ['audit']]) void queryClient.invalidateQueries({ queryKey: key })
    },
  })
}

export const useImpersonationStatus = () =>
  useQuery({ queryKey: ['impersonation'], queryFn: ({ signal }) => apiGet<{ configured: boolean }>('/api/impersonation', signal) })

export function useImpersonate() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ userId, reason, minutes }: { userId: string; reason: string; minutes: number }) =>
      apiPost<{ url: string; expiresAt: string }>(`/api/users/${encodeURIComponent(userId)}/impersonate`, { reason, minutes }),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['audit'] }),
  })
}
