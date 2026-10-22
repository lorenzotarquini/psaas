import i18next from 'i18next'
import { beforeAll, describe, expect, it } from 'vitest'
import italian from '../../../../public/locales/it/common.json'
import type { AuditEvent } from '../api/auditApi'
import { describeAuditEvent } from './describe'

const event = (action: string, details: AuditEvent['details']): AuditEvent => ({
  id: '1',
  occurredAt: '2026-10-22T09:00:00Z',
  actorId: 'kc-1',
  actorName: 'Marta Russo',
  action,
  targetType: 'tenant',
  targetId: 't1',
  targetName: 'Officine Rinaldi Spa',
  tenantId: 't1',
  details,
})

describe('describeAuditEvent', () => {
  beforeAll(() => i18next.init({ lng: 'it', resources: { it: { translation: italian } } }))

  // UC12-S2: only the changed fields, before → after.
  it('lists the changed registry fields with before and after', () => {
    const sentence = describeAuditEvent(
      event('tenant.registry.updated', { changes: { certifiedEmail: { before: 'a@pec.it', after: 'b@pec.it' } } }),
      i18next.t,
    )
    expect(sentence.what).toBe("ha modificato l'anagrafica di")
    expect(sentence.detail).toBe('PEC: a@pec.it → b@pec.it')
  })

  // UC12-S3 and mock 1b: "2 profili, 1 unità".
  it('summarizes an organization change', () => {
    const sentence = describeAuditEvent(
      event('tenant.organization.updated', {
        profilesAdded: ['Revisore'],
        profilesRemoved: ['Ispettore'],
        profilesRenamed: [],
        unitsAdded: ['Magazzino'],
        unitsRemoved: [],
        unitsRenamed: [],
        accessChanges: 0,
      }),
      i18next.t,
    )
    expect(sentence.detail).toBe('2 profili, 1 unità · aggiunto il profilo "Revisore" · tolto il profilo "Ispettore" · aggiunta l\'unità "Magazzino"')
  })

  it('keeps an unknown action readable', () => {
    expect(describeAuditEvent(event('module.installed', null), i18next.t).what).toBe('module.installed')
  })
})
