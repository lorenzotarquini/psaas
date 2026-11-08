// @vitest-environment jsdom
import i18next from 'i18next'
import { beforeAll, describe, expect, it } from 'vitest'
import italian from '../../../../public/locales/it/common.json'
import { isTyping, paletteEntries } from './entries'

describe('command palette', () => {
  beforeAll(() => i18next.init({ lng: 'it', resources: { it: { translation: italian } }, interpolation: { escapeValue: false } }))

  it('lists tenants, then users, then the matching quick actions', () => {
    const entries = paletteEntries(
      {
        tenants: [{ id: 't1', companyName: 'Ferri Srl', completeVatNumber: 'IT1', certifiedEmail: 'F@PEC.IT', status: 'Active' }],
        users: [{ id: 'u1', firstName: 'Giulia', lastName: 'Ferri', email: 'g@x.it' } as never],
        usersUnavailable: false,
      },
      'ferri',
      i18next.t,
    )
    expect(entries.map((e) => [e.group, e.label])).toEqual([
      ['tenants', 'Ferri Srl'],
      ['users', 'Giulia Ferri'],
    ])
    expect(entries[0].detail).toBe('IT1 · f@pec.it')
  })

  it('shows every quick action with an empty text', () => {
    expect(paletteEntries(undefined, '', i18next.t).every((e) => e.group === 'actions')).toBe(true)
  })

  it('treats inputs and editable elements as typing', () => {
    expect(isTyping(document.createElement('input'))).toBe(true)
    expect(isTyping(document.createElement('div'))).toBe(false)
  })
})
