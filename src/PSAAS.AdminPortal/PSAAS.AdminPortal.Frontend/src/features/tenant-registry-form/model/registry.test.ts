import { describe, expect, it } from 'vitest'
import { emptyRegistry, registrySchema, toRegistryRequest, type RegistryValues } from './registry'

const business: RegistryValues = {
  ...emptyRegistry,
  companyName: 'Alfa Srl',
  completeVatNumber: 'IT12345678901',
  certifiedEmail: 'alfa@pec.it',
  street: 'Via Roma 1',
  city: 'Roma',
  postalCode: '00118',
}
const individual: RegistryValues = { ...business, kind: 'Individual', companyName: 'Mario Rossi', completeVatNumber: 'RSSMRA85T10A562S', certifiedEmail: 'mario@example.com' }

const errorsOf = (values: RegistryValues) => {
  const result = registrySchema.safeParse(values)
  return result.success ? {} : Object.fromEntries(result.error.issues.map((issue) => [issue.path.join('.'), issue.message]))
}

describe('registrySchema', () => {
  it('accepts a company and a private person', () => {
    expect(errorsOf(business)).toEqual({})
    expect(errorsOf(individual)).toEqual({})
  })

  // UC3-S4 and UC3-S11: the rules follow the kind.
  it('applies the identifier and name length of the kind', () => {
    expect(errorsOf({ ...business, completeVatNumber: 'RSSMRA85T10A562S', companyName: 'a'.repeat(21) })).toEqual({
      completeVatNumber: 'InvalidFormat',
      companyName: 'MaxLengthExceeded',
    })
    expect(errorsOf({ ...individual, completeVatNumber: 'IT12345678901', companyName: 'a'.repeat(21) })).toEqual({
      completeVatNumber: 'InvalidFormat',
    })
    expect(errorsOf({ ...individual, companyName: 'a'.repeat(51) })).toEqual({ companyName: 'MaxLengthExceeded' })
  })

  it('refuses a tax code with a month letter that does not exist', () => {
    expect(errorsOf({ ...individual, completeVatNumber: 'RSSMRA10N54D789W' })).toEqual({ completeVatNumber: 'InvalidFormat' })
    expect(errorsOf({ ...individual, completeVatNumber: 'rssmra85t10a562s' })).toEqual({})
  })

  it('refuses an SDI code for a private person', () => {
    expect(errorsOf({ ...individual, sdiCode: 'M5UXCR1' })).toEqual({ sdiCode: 'InvalidValue' })
  })

  // UC4: the kind is fixed, so an update never sends it.
  it('leaves the kind out of the registry request', () => {
    expect(toRegistryRequest(registrySchema.parse(individual))).not.toHaveProperty('kind')
  })
})
