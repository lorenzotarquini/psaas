import { userStatuses, type UsersQuery, type UserStatus } from '@/entities/user'
import { userPageSizes } from '@/widgets/users-table'

const defaultPageSize = 50

const positiveInt = (value: string | null, fallback: number) => {
  const number = Number(value)
  return Number.isInteger(number) && number >= 1 ? number : fallback
}

// The URL is the state of the page (filters, page, open user): shareable, and the back button works.
export function readUsersQuery(params: URLSearchParams): UsersQuery {
  const pageSize = positiveInt(params.get('pageSize'), defaultPageSize)
  const status = params.get('status') as UserStatus | null
  return {
    search: params.get('search')?.trim() ?? '',
    tenantId: params.get('tenantId') || undefined,
    profileId: params.get('profileId') || undefined,
    status: status && userStatuses.includes(status) ? status : undefined,
    page: positiveInt(params.get('page'), 1),
    pageSize: (userPageSizes as readonly number[]).includes(pageSize) ? pageSize : defaultPageSize,
  }
}

export function writeUsersQuery(query: UsersQuery, user: string | null): URLSearchParams {
  const params = new URLSearchParams()
  if (query.search) params.set('search', query.search)
  if (query.tenantId) params.set('tenantId', query.tenantId)
  if (query.profileId) params.set('profileId', query.profileId)
  if (query.status) params.set('status', query.status)
  if (query.page !== 1) params.set('page', String(query.page))
  if (query.pageSize !== defaultPageSize) params.set('pageSize', String(query.pageSize))
  if (user) params.set('user', user)
  return params
}
