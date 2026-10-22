import { describe, expect, it } from 'vitest'
import { buildChecks, nextStep, readStep } from './onboarding'

const values = { companyName: 'Rinaldi Srl', completeVatNumber: 'IT123', certifiedEmail: 'a@pec.it', city: '', postalCode: '' }
const free = { verified: false, conflictingTenant: null }

describe('onboarding steps', () => {
  it('follows the five steps of mock 1e and starts from the registry without a draft', () => {
    expect(nextStep('registry')).toBe('plan')
    expect(nextStep('summary')).toBeUndefined()
    expect(readStep('organization', true)).toBe('organization')
    expect(readStep('organization', false)).toBe('registry')
    expect(readStep('nope', true)).toBe('registry')
  })
})

describe('buildChecks', () => {
  // Mock 1e: duplicate name, VAT format error, PEC verified, city pending, plan on step 2.
  it('reports each rule of saving like the Controlli panel', () => {
    const checks = buildChecks({
      values,
      errors: { completeVatNumber: 'InvalidFormat' },
      unique: (field) =>
        field === 'companyName'
          ? { verified: false, conflictingTenant: { companyName: 'Rinaldi Srl' } }
          : field === 'certifiedEmail'
            ? { verified: true, conflictingTenant: null }
            : free,
      planChosen: false,
    })
    expect(checks.map((c) => [c.key, c.state, c.detail.key])).toEqual([
      ['companyName', 'error', 'onboarding.checks.usedBy'],
      ['completeVatNumber', 'error', 'validation.InvalidFormat'],
      ['certifiedEmail', 'ok', 'onboarding.checks.verified'],
      ['address', 'pending', 'onboarding.checks.waitingCity'],
      ['plan', 'pending', 'onboarding.checks.planStep'],
    ])
  })

  it('confirms city and postal code once both are there', () => {
    const [, , , address] = buildChecks({
      values: { ...values, city: 'Bologna', postalCode: '40121' },
      errors: {},
      unique: () => free,
      planChosen: true,
    })
    expect(address).toEqual({ key: 'address', state: 'ok', detail: { key: 'onboarding.checks.addressOk', values: { city: 'Bologna', postalCode: '40121' } } })
  })
})
