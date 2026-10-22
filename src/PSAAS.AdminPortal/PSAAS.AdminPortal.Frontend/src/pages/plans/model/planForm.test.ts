import { describe, expect, it } from 'vitest'
import { emptyPlan, planSchema, toPlanRequest } from './planForm'

describe('plan form', () => {
  it('turns the strings of the form into the request, empty limits being unlimited', () => {
    const values = planSchema.parse({ ...emptyPlan, code: 'pro', name: 'Pro', price: '99,50', maxUsers: '50' })
    expect(toPlanRequest(values)).toMatchObject({ price: 99.5, trialDays: 0, maxUsers: 50, maxProductiveUnits: null, description: null })
  })

  it('rejects a code with capitals and a negative price', () => {
    const result = planSchema.safeParse({ ...emptyPlan, code: 'Pro', name: 'Pro', price: '-1' })
    expect(result.success).toBe(false)
    expect(result.error?.issues.map((i) => [i.path[0], i.message])).toEqual([
      ['code', 'InvalidFormat'],
      ['price', 'InvalidValue'],
    ])
  })
})
