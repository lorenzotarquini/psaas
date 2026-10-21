import { describe, expect, it } from 'vitest'
import type { TenantDetail } from '@/entities/tenant'
import { organizationFromTenant, removedSavedProfiles, toOrganizationRequest } from './organization'

const tenant = {
  profiles: [
    { id: 'p1', name: 'Operatore', description: null },
    { id: 'p2', name: 'Magazziniere', description: 'Solo magazzino' },
  ],
  productiveUnits: [{ id: 'u1', name: 'Stabilimento', description: null, profileIds: ['p1'] }],
} as TenantDetail

describe('organization editor model', () => {
  // UC5-S2: renaming keeps identities and links.
  it('round-trips a saved organization keeping ids and links', () => {
    const values = organizationFromTenant(tenant)
    values.profiles[0].name = ' Addetto '
    expect(toOrganizationRequest(values)).toEqual({
      profiles: [
        { id: 'p1', key: 'p1', name: 'Addetto', description: null },
        { id: 'p2', key: 'p2', name: 'Magazziniere', description: 'Solo magazzino' },
      ],
      productiveUnits: [{ id: 'u1', name: 'Stabilimento', description: null, profileKeys: ['p1'] }],
    })
  })

  // UC5-S4 / R4: removed saved profiles are reported for confirmation.
  it('lists the saved profiles that were removed', () => {
    const values = organizationFromTenant(tenant)
    values.profiles = values.profiles.filter((p) => p.id !== 'p2')
    expect(removedSavedProfiles(tenant, values)).toEqual(['Magazziniere'])
  })
})
