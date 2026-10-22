// Route paths, shared so widgets and pages can link to pages without importing them.
export const routes = {
  home: '/',
  tenants: '/tenants',
  tenantNew: '/tenants/new',
  tenantDetail: '/tenants/:tenantId',
  users: '/users',
  userNew: '/users/new',
  profiles: '/profiles',
  modules: '/modules',
  plans: '/plans',
  subscriptions: '/subscriptions',
  billing: '/billing',
  communications: '/communications',
  audit: '/audit',
  settings: '/settings',
} as const

const id = (value: string) => encodeURIComponent(value)
/** Tabs of the tenant page (mock 1d), kept in the URL so each one can be linked. */
export type TenantTab = 'overview' | 'registry' | 'organization' | 'subscription' | 'users' | 'usage' | 'invoices' | 'history'
export const tenantPath = (tenantId: string, tab?: TenantTab) =>
  `/tenants/${id(tenantId)}${tab && tab !== 'overview' ? `?tab=${tab}` : ''}`
export const usersSearchPath = (search: string) => `/users?${new URLSearchParams({ search })}`

/** Navigation state set after UC6: the users page confirms the creation. */
export interface UserCreatedState {
  createdEmail: string
}

/** Navigation state: lets a detail page return to the list with its search and page. */
export interface ListReturnState {
  listSearch: string
}
