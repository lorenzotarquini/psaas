import { keepPreviousData, useMutation, useQuery, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { apiGet, apiPost, apiPut, type PagedResult } from '@/shared/api'

/** UC19-R1. */
export type UserStatus = 'Invited' | 'Active' | 'Suspended' | 'Disabled'

export const userStatuses: UserStatus[] = ['Active', 'Invited', 'Suspended', 'Disabled']

// UC7/UC19 contract (docs/use-cases/API.md). Profiles that no longer exist are already omitted by the backend.
export interface User {
  id: string
  firstName: string
  lastName: string
  email: string
  /** null for users created outside the portal (directly in Keycloak). */
  birthDate: string | null
  profiles: { id: string; name: string; tenantId: string; tenantCompanyName: string }[]
  status: UserStatus
  mfaRequired: boolean
  lastLoginAt: string | null
  createdAt: string | null
  createdBy: string | null
  invitesSent: number
  lastInviteAt: string | null
}

/** UC6 body. birthDate is "YYYY-MM-DD"; Keycloak emails the user to set the password. */
export interface CreateUserRequest {
  firstName: string
  lastName: string
  email: string
  birthDate: string
  profileIds: string[]
}

/** UC19 body: the email is the username and never changes. */
export interface UpdateUserRequest {
  firstName: string
  lastName: string
  birthDate: string
  profileIds: string[]
  mfaRequired: boolean
}

export interface UsersQuery {
  search: string
  tenantId?: string
  profileId?: string
  status?: UserStatus
  page: number
  pageSize: number
}

/** UC20 row as read from the file; profiles = "Cliente:Profilo|Cliente:Profilo". */
export interface UserImportRow {
  firstName: string
  lastName: string
  email: string
  birthDate: string
  profiles: string
}

export interface UserImportResult {
  row: number
  email: string
  errors: Record<string, string>
  created: User | null
}

export type UserAction = 'invite' | 'reset-password' | 'disable' | 'enable'

const getUsers = ({ search, tenantId, profileId, status, page, pageSize }: UsersQuery, signal: AbortSignal) => {
  const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  if (search) query.set('search', search)
  if (tenantId) query.set('tenantId', tenantId)
  if (profileId) query.set('profileId', profileId)
  if (status) query.set('status', status)
  return apiGet<PagedResult<User>>(`/api/users?${query}`, signal)
}

export const useUsers = (query: UsersQuery) =>
  useQuery({
    queryKey: ['users', 'list', query],
    queryFn: ({ signal }) => getUsers(query, signal),
    placeholderData: keepPreviousData,
  })

export const useUser = (id: string | null) =>
  useQuery({
    queryKey: ['users', 'detail', id],
    queryFn: ({ signal }) => apiGet<User>(`/api/users/${encodeURIComponent(id!)}`, signal),
    enabled: Boolean(id),
  })

// Users change counts on tenants (UC2) and the history: refresh what shows them.
function onUsersChanged(queryClient: QueryClient, user?: User) {
  if (user) queryClient.setQueryData(['users', 'detail', user.id], user)
  for (const key of [['users', 'list'], ['audit'], ['tenants'], ['profiles']]) void queryClient.invalidateQueries({ queryKey: key })
}

export function useCreateUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: CreateUserRequest) => apiPost<User>('/api/users', body),
    onSuccess: (user) => onUsersChanged(queryClient, user),
  })
}

export function useUpdateUser(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: UpdateUserRequest) => apiPut<User>(`/api/users/${encodeURIComponent(id)}`, body),
    onSuccess: (user) => onUsersChanged(queryClient, user),
  })
}

export function useUserAction(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (action: UserAction) => apiPost<User>(`/api/users/${encodeURIComponent(id)}/${action}`, undefined),
    onSuccess: (user) => onUsersChanged(queryClient, user),
  })
}

export function useImportUsers() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: { rows: UserImportRow[]; confirm: boolean }) => apiPost<UserImportResult[]>('/api/users/import', body),
    onSuccess: (_, { confirm }) => {
      if (confirm) onUsersChanged(queryClient)
    },
  })
}
