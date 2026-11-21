import { describe, expect, it } from 'vitest'
import { emptyPlan, planSchema, toPlanRequest } from './planForm'

describe('plan form', () => {
  it('turns the strings of the form into the request, empty limits being unlimited', () => {
    const values = planSchema.parse({ ...emptyPlan, code: 'pro', name: 'Pro', price: '99,50', maxUsers: '50', moduleIds: ['mysync'] })
    expect(toPlanRequest(values)).toMatchObject({
      price: 99.5,
      trialDays: 0,
      maxUsers: 50,
      maxProductiveUnits: null,
      description: null,
      moduleIds: ['mysync'],
      vatRate: 22,
      vatNature: null,
      priceIncludesVat: true,
      tier: null,
    })
  })

  it('rejects a code with capitals and a negative price', () => {
    const result = planSchema.safeParse({ ...emptyPlan, code: 'Pro', name: 'Pro', price: '-1', moduleIds: ['mysync'] })
    expect(result.success).toBe(false)
    expect(result.error?.issues.map((i) => [i.path[0], i.message])).toEqual([
      ['code', 'InvalidFormat'],
      ['price', 'InvalidValue'],
    ])
  })

  it('UC16-R2, R4: needs one module, and the nature of the VAT with 0%', () => {
    const result = planSchema.safeParse({ ...emptyPlan, code: 'pro', name: 'Pro', price: '10', vatRate: '0' })
    expect(result.error?.issues.map((i) => [i.path[0], i.message])).toEqual([
      ['moduleIds', 'Required'],
      ['vatNature', 'Required'],
    ])
    const exempt = planSchema.parse({ ...emptyPlan, code: 'pro', name: 'Pro', price: '10', moduleIds: ['mysync'], vatRate: '0', vatNature: 'n2.1' })
    expect(toPlanRequest(exempt)).toMatchObject({ vatRate: 0, vatNature: 'N2.1' })
    expect(toPlanRequest({ ...exempt, tier: '2' })).toMatchObject({ tier: 2 })
  })
})
