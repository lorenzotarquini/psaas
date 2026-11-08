import { describe, expect, it, vi } from 'vitest'
import { applyServerErrors, toFormPath } from './serverErrors'

describe('toFormPath', () => {
  it('converts indexed ASP.NET paths', () => {
    expect(toFormPath('profiles[1].name')).toBe('profiles.1.name')
    expect(toFormPath('productiveUnits[0].profileKeys[2]')).toBe('productiveUnits.0.profileKeys.2')
  })

  it('drops the JSON root prefix', () => {
    expect(toFormPath('$.companyName')).toBe('companyName')
  })
})

describe('applyServerErrors', () => {
  it('sets the first code of each field as its message', () => {
    const setError = vi.fn()
    applyServerErrors({ companyName: ['NotUnique', 'Required'], 'profiles[1].name': ['NotUnique'] }, setError)
    expect(setError).toHaveBeenCalledWith('companyName', { type: 'server', message: 'NotUnique' })
    expect(setError).toHaveBeenCalledWith('profiles.1.name', { type: 'server', message: 'NotUnique' })
  })
})
