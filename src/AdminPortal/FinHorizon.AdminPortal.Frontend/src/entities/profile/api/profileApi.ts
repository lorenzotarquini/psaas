import { useMutation, useQuery, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { apiDelete, apiGet, apiPost } from '@/shared/api'

// Mirrors ProfileReadModel in FinHorizon.AdminPortal.Application. The backend orders profiles by name.
export interface Profile {
  id: string
  tenantId: string
  tenantCompanyName: string
  name: string
  description: string | null
}

/** UC9: a new profile of an organization, with no productive units yet. */
export interface ProfileCreateRequest {
  tenantId: string
  name: string
  description: string | null
}

export const useProfiles = () =>
  useQuery({ queryKey: ['profiles'], queryFn: ({ signal }) => apiGet<Profile[]>('/api/profiles', signal) })

// A profile change is an organization change (UC5): the tenant, its users and the audit show it too.
function onProfilesChanged(queryClient: QueryClient) {
  for (const queryKey of [['profiles'], ['tenants'], ['users'], ['audit']]) void queryClient.invalidateQueries({ queryKey })
}

export function useCreateProfile() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: ProfileCreateRequest) => apiPost<Profile>('/api/profiles', body),
    onSuccess: () => onProfilesChanged(queryClient),
  })
}

/** UC9: always allowed; the users that had the profile lose it. */
export function useDeleteProfile() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => apiDelete(`/api/profiles/${encodeURIComponent(id)}`),
    onSuccess: () => onProfilesChanged(queryClient),
  })
}
