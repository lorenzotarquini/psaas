import { describe, expect, it } from 'vitest'
import type { TenantDetail } from '@/entities/tenant'
import { countChanges, newProfile, organizationFromTenant, removedSavedProfiles, toOrganizationRequest } from './organization'

const tenant = {
  profiles: [
    { id: 'p1', name: 'Operatore', description: null, userCount: 18 },
    { id: 'p2', name: 'Magazziniere', description: 'Solo magazzino', userCount: 3 },
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

  // UC5-S4 / R4: a profile marked as removed leaves the request and the units that opened it.
  it('leaves removed profiles out of the request and of the units', () => {
    const values = organizationFromTenant(tenant)
    values.profiles[0].removed = true
    const request = toOrganizationRequest(values)
    expect(request.profiles.map((p) => p.id)).toEqual(['p2'])
    expect(request.productiveUnits[0].profileKeys).toEqual([])
    expect(removedSavedProfiles(values).map((p) => p.name)).toEqual(['Operatore'])
  })

  it('counts added, renamed, removed items and changed accesses', () => {
    const original = organizationFromTenant(tenant)
    const values = organizationFromTenant(tenant)
    expect(countChanges(original, values)).toBe(0)

    values.profiles[0].name = 'Addetto' // renamed
    values.profiles[1].removed = true // removed
    values.productiveUnits[0].profileKeys = [] // p1 no longer opens u1
    values.profiles.push({ ...newProfile(), name: 'Nuovo' }) // added
    expect(countChanges(original, values)).toBe(4)
  })

  it('does not count a new item removed before saving', () => {
    const original = organizationFromTenant(tenant)
    const values = organizationFromTenant(tenant)
    values.profiles.push({ ...newProfile(), name: 'Nuovo', removed: true })
    expect(countChanges(original, values)).toBe(0)
  })
})
