import { describe, expect, it } from 'vitest'
import { initials } from './initials'

describe('initials', () => {
  it('takes given and family name', () => {
    expect(initials('Marta', 'russo')).toBe('MR')
  })

  it('falls back to the first two letters of the username', () => {
    expect(initials(undefined, 'Russo', 'admin.psaas')).toBe('AD')
  })
})
