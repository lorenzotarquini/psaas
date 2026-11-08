import i18next from 'i18next'
import { beforeAll, describe, expect, it } from 'vitest'
import italian from '../../../../public/locales/it/common.json'
import type { Notification } from '../api/communicationApi'
import { describeNotification } from './notification'

const item = (kind: Notification['kind'], extra: Partial<Notification> = {}): Notification => ({
  kind,
  severity: 'critical',
  tenantId: 't1',
  tenantName: 'Alfa Srl',
  subject: null,
  objectId: null,
  at: '2026-10-20T09:00:00Z',
  value: null,
  limit: null,
  ...extra,
})

describe('describeNotification', () => {
  // As the app: React escapes on render, i18next must not (src/shared/i18n/i18n.ts).
  beforeAll(() => i18next.init({ lng: 'it', resources: { it: { translation: italian } }, interpolation: { escapeValue: false } }))

  it('links an overdue invoice to the invoice panel', () => {
    const n = describeNotification(item('invoice.overdue', { subject: '2026/0001', objectId: 'i1' }), i18next.t)
    expect(n.text).toBe('Fattura 2026/0001 di Alfa Srl scaduta')
    expect(n.to).toBe('/billing?invoice=i1')
  })

  it('tells the users against the plan limit', () => {
    expect(describeNotification(item('users.limit', { value: 9, limit: 10 }), i18next.t).text).toBe('Alfa Srl ha 9 utenti su 10')
  })
})
