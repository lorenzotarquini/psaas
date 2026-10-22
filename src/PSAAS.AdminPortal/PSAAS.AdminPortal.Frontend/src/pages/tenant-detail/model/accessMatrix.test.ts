import { describe, expect, it } from 'vitest'
import { buildAccessMatrix } from './accessMatrix'

const profile = (id: string) => ({ id, name: id, description: null, userCount: 0 })
const unit = (id: string, profileIds: string[]) => ({ id, name: id, description: null, profileIds })

describe('buildAccessMatrix', () => {
  // UC2-S1: "Stabilimento" opened by "Operatore", "Magazzino" by nobody.
  it('marks the units each profile opens', () => {
    const matrix = buildAccessMatrix({
      profiles: [profile('op'), profile('osp')],
      productiveUnits: [unit('magazzino', []), unit('stabilimento', ['op'])],
    })
    expect(matrix.map((row) => [row.profile.id, row.opens])).toEqual([
      ['op', [false, true]],
      ['osp', [false, false]],
    ])
  })

  // UC2-S2 / R1: a tenant may have no organization at all.
  it('is empty without profiles', () => {
    expect(buildAccessMatrix({ profiles: [], productiveUnits: [unit('u', [])] })).toEqual([])
  })
})
