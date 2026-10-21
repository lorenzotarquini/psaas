import { describe, expect, it } from 'vitest'
import { readTenantsQuery, writeTenantsQuery } from './searchParams'

describe('tenants list search params', () => {
  it('uses defaults for an empty URL', () => {
    expect(readTenantsQuery(new URLSearchParams())).toEqual({ search: '', page: 1, pageSize: 20 })
  })

  it('falls back to defaults for values the backend would reject', () => {
    expect(readTenantsQuery(new URLSearchParams('page=0&pageSize=101'))).toEqual({ search: '', page: 1, pageSize: 20 })
    expect(readTenantsQuery(new URLSearchParams('page=abc&pageSize=7'))).toEqual({ search: '', page: 1, pageSize: 20 })
  })

  it('round-trips a query, omitting defaults from the URL', () => {
    const query = { search: 'alfa', page: 3, pageSize: 50 }
    expect(readTenantsQuery(writeTenantsQuery(query))).toEqual(query)
    expect(writeTenantsQuery({ search: '', page: 1, pageSize: 20 }).toString()).toBe('')
  })
})
