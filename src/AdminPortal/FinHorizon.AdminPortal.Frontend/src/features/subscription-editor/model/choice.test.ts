import { describe, expect, it } from 'vitest'
import type { Module, Plan, Subscription } from '@/entities/catalog'
import { addOnCandidates, isUnchanged } from './choice'

const module = (id: string, extra: Partial<Module> = {}) => ({ id, isCore: false, status: 'Available', ...extra }) as Module
const plan = { id: 'p1', moduleIds: ['core', 'warehouse'] } as Plan

describe('addOnCandidates', () => {
  it('offers only the modules outside the plan, never the core or a deprecated one', () => {
    const modules = [module('core', { isCore: true }), module('warehouse'), module('hr'), module('old', { status: 'Deprecated' })]
    expect(addOnCandidates(plan, modules).map((m) => m.id)).toEqual(['hr'])
    expect(addOnCandidates(undefined, modules)).toEqual([])
  })
})

describe('isUnchanged', () => {
  const current = { planId: 'p1', autoRenew: true, addOnModuleIds: ['hr'] } as Subscription
  it('skips saving when no plan is chosen or nothing differs', () => {
    expect(isUnchanged({ planId: null, trial: false, addOnModuleIds: [], autoRenew: true }, null)).toBe(true)
    expect(isUnchanged({ planId: 'p1', trial: false, addOnModuleIds: ['hr'], autoRenew: true }, current)).toBe(true)
  })
  it('saves a first plan or a different add-on', () => {
    expect(isUnchanged({ planId: 'p1', trial: false, addOnModuleIds: [], autoRenew: true }, null)).toBe(false)
    expect(isUnchanged({ planId: 'p1', trial: false, addOnModuleIds: [], autoRenew: true }, current)).toBe(false)
  })
})
