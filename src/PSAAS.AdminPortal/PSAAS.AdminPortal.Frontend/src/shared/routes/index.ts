// Route paths, shared so widgets and pages can link to pages without importing them.
export const routes = {
  home: '/',
  tenants: '/tenants',
  tenantNew: '/tenants/new',
  tenantDetail: '/tenants/:tenantId',
  tenantEdit: '/tenants/:tenantId/edit',
  tenantOrganization: '/tenants/:tenantId/organization',
  users: '/users',
  userNew: '/users/new',
  profiles: '/profiles',
  settings: '/settings',
} as const

const id = (value: string) => encodeURIComponent(value)
export const tenantPath = (tenantId: string) => `/tenants/${id(tenantId)}`
export const tenantEditPath = (tenantId: string) => `/tenants/${id(tenantId)}/edit`
export const tenantOrganizationPath = (tenantId: string) => `/tenants/${id(tenantId)}/organization`
export const usersSearchPath = (search: string) => `/users?${new URLSearchParams({ search })}`

/** Navigation state set after UC6: the users page confirms the creation. */
export interface UserCreatedState {
  createdEmail: string
}

/** Navigation state: lets a detail page return to the list with its search and page. */
export interface ListReturnState {
  listSearch: string
}
