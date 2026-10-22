import { describe, expect, it } from 'vitest'
import { readUsersQuery, writeUsersQuery } from './searchParams'

describe('users URL', () => {
  it('round-trips filters, page and the open user', () => {
    const params = new URLSearchParams('search=ferri&tenantId=t1&status=Invited&page=2&user=u1')
    expect(writeUsersQuery(readUsersQuery(params), params.get('user')).toString()).toBe(params.toString())
  })

  it('drops an unknown status and a page size that is not offered', () => {
    expect(readUsersQuery(new URLSearchParams('status=Gone&pageSize=7'))).toMatchObject({ status: undefined, pageSize: 50 })
  })
})
