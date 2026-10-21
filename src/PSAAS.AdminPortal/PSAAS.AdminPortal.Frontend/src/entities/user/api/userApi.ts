import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiGet, apiPost } from '@/shared/api'

// UC7 contract (docs/use-cases/API.md). Profiles that no longer exist are already omitted by the backend.
export interface User {
  id: string
  firstName: string
  lastName: string
  email: string
  birthDate: string
  profiles: { id: string; name: string; tenantId: string; tenantCompanyName: string }[]
}

/** UC6 body. birthDate is "YYYY-MM-DD"; Keycloak emails the user to set the password. */
export interface CreateUserRequest {
  firstName: string
  lastName: string
  email: string
  birthDate: string
  profileIds: string[]
}

export const useUsers = (search: string) =>
  useQuery({
    queryKey: ['users', search],
    queryFn: ({ signal }) => apiGet<User[]>(`/api/users?search=${encodeURIComponent(search)}`, signal),
    placeholderData: keepPreviousData,
  })

export function useCreateUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateUserRequest) => apiPost<User>('/api/users', body),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['users'] }),
  })
}
