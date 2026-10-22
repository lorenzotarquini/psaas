import { tenantsParams, type TenantsQuery, type TenantStatus } from '@/entities/tenant'

export const pageSizes = [10, 20, 50, 100] as const
const defaultPageSize = 20
const statuses: TenantStatus[] = ['Onboarding', 'Trial', 'Active', 'Suspended', 'Cancelling', 'Ceased']
const sorts = ['companyName', 'city', 'status', 'plan', 'renewsAt']

const positiveInt = (value: string | null, fallback: number) => {
  const number = Number(value)
  return Number.isInteger(number) && number >= 1 ? number : fallback
}

// The URL is the state of the page: shareable, savable as a view (UC22), and the back button works.
// Invalid values fall back to defaults instead of reaching the backend.
export function readTenantsQuery(params: URLSearchParams): TenantsQuery {
  const pageSize = positiveInt(params.get('pageSize'), defaultPageSize)
  const sort = params.get('sort') ?? 'companyName'
  const renewal = positiveInt(params.get('renewalWithinDays'), 0)
  return {
    search: params.get('search')?.trim() ?? '',
    statuses: params.getAll('status').filter((s): s is TenantStatus => statuses.includes(s as TenantStatus)),
    planIds: params.getAll('planId'),
    moduleId: params.get('moduleId') || undefined,
    province: params.get('province')?.trim().toUpperCase() || undefined,
    renewalWithinDays: renewal || undefined,
    sort: sorts.includes(sort.replace(/^-/, '')) ? sort : 'companyName',
    page: positiveInt(params.get('page'), 1),
    pageSize: (pageSizes as readonly number[]).includes(pageSize) ? pageSize : defaultPageSize,
  }
}

export function writeTenantsQuery(query: TenantsQuery): URLSearchParams {
  const params = tenantsParams(query)
  if (query.page !== 1) params.set('page', String(query.page))
  if (query.pageSize !== defaultPageSize) params.set('pageSize', String(query.pageSize))
  return params
}

/** The filters only (what a saved view stores and a tab sets): no page. */
export const filtersOf = (query: TenantsQuery) => tenantsParams(query).toString()

/** Tabs of mock 1c: each one is a set of filters; the active tab is the one whose filters equal the URL's. */
export const statusTabs: { key: string; filters: Partial<TenantsQuery> }[] = [
  { key: 'all', filters: {} },
  { key: 'Active', filters: { statuses: ['Active'] } },
  { key: 'Trial', filters: { statuses: ['Trial'] } },
  { key: 'Onboarding', filters: { statuses: ['Onboarding'] } },
  { key: 'Suspended', filters: { statuses: ['Suspended'] } },
  { key: 'renewingSoon', filters: { renewalWithinDays: 30 } },
]

export const emptyFilters: Omit<TenantsQuery, 'page' | 'pageSize'> = {
  search: '',
  statuses: [],
  planIds: [],
  moduleId: undefined,
  province: undefined,
  renewalWithinDays: undefined,
  sort: 'companyName',
}

/** "tra 3 gg" within a week, "scaduto" once past, otherwise the date (mock 1c). */
export function renewalLabel(iso: string | null, now = new Date()): { kind: 'none' | 'expired' | 'soon' | 'date'; days: number } {
  if (!iso) return { kind: 'none', days: 0 }
  const days = Math.ceil((new Date(iso).getTime() - now.getTime()) / 86_400_000)
  if (days < 0) return { kind: 'expired', days }
  return { kind: days <= 7 ? 'soon' : 'date', days }
}

/** Columns of mock 1c that can be hidden ("Colonne"); the company name always shows. */
export const optionalColumns = ['certifiedEmail', 'city', 'status', 'plan', 'modules', 'users', 'renewal'] as const
export type OptionalColumn = (typeof optionalColumns)[number]
