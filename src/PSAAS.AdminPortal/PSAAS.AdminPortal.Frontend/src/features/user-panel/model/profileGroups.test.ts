import { describe, expect, it } from 'vitest'
import type { Profile } from '@/entities/profile'
import { groupProfilesByTenant, toggle } from './profileGroups'

const profile = (id: string, name: string, tenantId: string, tenantCompanyName: string): Profile => ({ id, name, tenantId, tenantCompanyName, description: null })
const all = [
  profile('p2', 'Operatore', 't1', 'Officine Rinaldi'),
  profile('p1', 'Capoturno', 't1', 'Officine Rinaldi'),
  profile('p3', 'Operatore', 't2', 'Ceramiche Bassi'),
  profile('p4', 'Revisore', 't3', 'Pastificio Lucani'),
]

describe('groupProfilesByTenant', () => {
  it('shows every profile of the tenants the user is in, ticking those it has', () => {
    expect(groupProfilesByTenant(all, ['p1', 'p3'], [])).toEqual([
      { tenantId: 't1', tenantCompanyName: 'Officine Rinaldi', profiles: [{ id: 'p1', name: 'Capoturno', selected: true }, { id: 'p2', name: 'Operatore', selected: false }] },
      { tenantId: 't2', tenantCompanyName: 'Ceramiche Bassi', profiles: [{ id: 'p3', name: 'Operatore', selected: true }] },
    ])
  })

  it('adds a tenant chosen with "Aggiungi profilo di un altro cliente" once', () => {
    expect(groupProfilesByTenant(all, ['p1'], ['t3', 't1']).map((g) => g.tenantId)).toEqual(['t1', 't3'])
  })

  it('toggles a profile', () => {
    expect(toggle(['p1'], 'p2')).toEqual(['p1', 'p2'])
    expect(toggle(['p1', 'p2'], 'p1')).toEqual(['p2'])
  })
})
