import { describe, expect, it } from 'vitest'
import { groupProfilesByTenant, userSchema } from './user'

const profile = (id: string, tenantId: string, tenantCompanyName: string) => ({
  id,
  tenantId,
  tenantCompanyName,
  name: id,
  description: null,
})

describe('groupProfilesByTenant', () => {
  it('groups profiles by tenant, ordered by tenant name, keeping the profile order', () => {
    const groups = groupProfilesByTenant([
      profile('Amministrativo', 'beta', 'Beta Snc'),
      profile('Operatore', 'alfa', 'Alfa Srl'),
      profile('Revisore', 'beta', 'Beta Snc'),
    ])
    expect(groups.map((g) => [g.tenantName, g.profiles.map((p) => p.id)])).toEqual([
      ['Alfa Srl', ['Operatore']],
      ['Beta Snc', ['Amministrativo', 'Revisore']],
    ])
  })
})

describe('userSchema', () => {
  // UC6-S3 and UC6-S4 on the client side, with the backend codes.
  it('reports Required and InvalidFormat like the backend', () => {
    const result = userSchema.safeParse({ firstName: ' ', lastName: 'Rossi', email: 'non-una-email', birthDate: '', profileIds: [] })
    // Zod reports every failed check; the form (zodResolver) shows the first one per field.
    const messages = Object.fromEntries(
      (result.error?.issues ?? []).toReversed().map((issue) => [issue.path.join('.'), issue.message]),
    )
    expect(messages).toEqual({ firstName: 'Required', email: 'InvalidFormat', birthDate: 'Required' })
  })
})
