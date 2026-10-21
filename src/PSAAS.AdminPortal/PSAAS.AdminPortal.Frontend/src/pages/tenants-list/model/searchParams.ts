import type { TenantsQuery } from '@/entities/tenant'

export const pageSizes = [10, 20, 50, 100] as const
const defaultPageSize = 20

const positiveInt = (value: string | null, fallback: number) => {
  const number = Number(value)
  return Number.isInteger(number) && number >= 1 ? number : fallback
}

// The URL is the state of the page: shareable, and the back button works. Invalid values fall back to defaults.
export function readTenantsQuery(params: URLSearchParams): TenantsQuery {
  const pageSize = positiveInt(params.get('pageSize'), defaultPageSize)
  return {
    search: params.get('search')?.trim() ?? '',
    page: positiveInt(params.get('page'), 1),
    pageSize: (pageSizes as readonly number[]).includes(pageSize) ? pageSize : defaultPageSize,
  }
}

export function writeTenantsQuery({ search, page, pageSize }: TenantsQuery): URLSearchParams {
  const params = new URLSearchParams()
  if (search) params.set('search', search)
  if (page !== 1) params.set('page', String(page))
  if (pageSize !== defaultPageSize) params.set('pageSize', String(pageSize))
  return params
}
