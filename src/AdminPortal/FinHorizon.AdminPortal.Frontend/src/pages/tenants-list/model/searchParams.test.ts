import { describe, expect, it } from 'vitest'
import { filtersOf, readTenantsQuery, renewalLabel, writeTenantsQuery } from './searchParams'

describe('tenants list search params', () => {
  it('uses defaults for an empty URL', () => {
    expect(readTenantsQuery(new URLSearchParams())).toEqual({
      search: '',
      statuses: [],
      planIds: [],
      moduleId: undefined,
      province: undefined,
      renewalWithinDays: undefined,
      sort: 'companyName',
      page: 1,
      pageSize: 20,
    })
  })

  it('drops values the backend would reject', () => {
    const query = readTenantsQuery(new URLSearchParams('page=0&pageSize=7&status=Happy&sort=colour&renewalWithinDays=-3'))
    expect(query).toMatchObject({ page: 1, pageSize: 20, statuses: [], sort: 'companyName', renewalWithinDays: undefined })
  })

  it('round-trips filters, sort and page, omitting defaults', () => {
    const url = 'search=alfa&status=Active&status=Trial&planId=p1&planId=p2&moduleId=warehouse&province=MI&renewalWithinDays=30&sort=-renewsAt&page=3&pageSize=50'
    expect(writeTenantsQuery(readTenantsQuery(new URLSearchParams(url))).toString()).toBe(url)
    expect(writeTenantsQuery(readTenantsQuery(new URLSearchParams())).toString()).toBe('')
  })

  it('keeps only the filters for a saved view', () => {
    expect(filtersOf(readTenantsQuery(new URLSearchParams('province=mi&page=4')))).toBe('province=MI')
  })
})

describe('renewalLabel', () => {
  const now = new Date('2026-10-22T10:00:00Z')
  it('tells overdue, soon and later renewals apart', () => {
    expect(renewalLabel(null, now).kind).toBe('none')
    expect(renewalLabel('2026-10-20T00:00:00Z', now).kind).toBe('expired')
    expect(renewalLabel('2026-10-25T00:00:00Z', now)).toEqual({ kind: 'soon', days: 3 })
    expect(renewalLabel('2027-01-14T00:00:00Z', now).kind).toBe('date')
  })
})
