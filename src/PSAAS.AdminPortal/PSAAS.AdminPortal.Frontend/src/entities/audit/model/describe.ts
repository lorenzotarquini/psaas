import type { TFunction } from 'i18next'
import type { AuditEvent } from '../api/auditApi'

interface Rename {
  before: string
  after: string
}

/** The sentence of mock 1b "Attività recente": who (bold) + what + object (bold), and a detail line. */
export interface AuditSentence {
  what: string
  detail: string
}

// "2 profili, 1 unità" (mock 1b).
const counts = (t: TFunction, profiles: number, units: number) =>
  `${t('audit.details.profiles', { count: profiles })}, ${t('audit.details.units', { count: units })}`

const list = (value: unknown) => (Array.isArray(value) ? (value as unknown[]) : [])

// One function per action code; an unknown code still reads as a sentence with the code itself.
export function describeAuditEvent(event: AuditEvent, t: TFunction): AuditSentence {
  const d = event.details ?? {}
  switch (event.action) {
    case 'tenant.created':
      return {
        what: t('audit.actions.tenant.created'),
        detail: counts(t, Number(d.profiles ?? 0), Number(d.productiveUnits ?? 0)),
      }
    case 'tenant.registry.updated': {
      const changes = (d.changes ?? {}) as Record<string, { before: string | null; after: string | null }>
      return {
        what: t('audit.actions.tenant.registryUpdated'),
        detail: Object.entries(changes)
          .map(([field, change]) => `${t(`audit.fields.${field}`, { defaultValue: field })}: ${change.before ?? '–'} → ${change.after ?? '–'}`)
          .join(' · '),
      }
    }
    case 'tenant.organization.updated': {
      const profiles = list(d.profilesAdded).length + list(d.profilesRemoved).length + list(d.profilesRenamed).length
      const units = list(d.unitsAdded).length + list(d.unitsRemoved).length + list(d.unitsRenamed).length
      const parts = [
        ...list(d.profilesAdded).map((name) => t('audit.details.profileAdded', { name })),
        ...list(d.profilesRemoved).map((name) => t('audit.details.profileRemoved', { name })),
        ...(list(d.profilesRenamed) as Rename[]).map((r) => t('audit.details.renamed', { before: r.before, after: r.after })),
        ...list(d.unitsAdded).map((name) => t('audit.details.unitAdded', { name })),
        ...list(d.unitsRemoved).map((name) => t('audit.details.unitRemoved', { name })),
        ...(list(d.unitsRenamed) as Rename[]).map((r) => t('audit.details.renamed', { before: r.before, after: r.after })),
      ]
      const accesses = Number(d.accessChanges ?? 0)
      if (accesses) parts.push(t('audit.details.accessChanges', { count: accesses }))
      return {
        what: t('audit.actions.tenant.organizationUpdated'),
        detail: [counts(t, profiles, units), ...parts].join(' · '),
      }
    }
    case 'tenant.onboarding.started':
      return { what: t('audit.actions.tenant.onboardingStarted'), detail: '' }
    case 'tenant.onboarding.discarded':
      return { what: t('audit.actions.tenant.onboardingDiscarded'), detail: '' }
    case 'tenant.status.changed': {
      // Same target status reads differently by origin: Onboarding → Active is an activation.
      const to = String(d.to)
      const verb = to === 'Active' && (d.from === 'Onboarding' || d.from === 'Trial') ? 'Activated' : to
      return {
        what: t(`audit.actions.tenant.status.${verb}`, { defaultValue: t('audit.actions.tenant.status.other') }),
        detail: d.reason ? t('audit.details.reason', { reason: String(d.reason) }) : '',
      }
    }
    case 'user.created':
      return {
        what: t('audit.actions.user.created'),
        detail: [d.tenant && t('audit.details.forTenant', { tenant: d.tenant }), list(d.profiles).join(', ')]
          .filter(Boolean)
          .join(' · '),
      }
    case 'user.updated': {
      const changes = (d.changes ?? {}) as Record<string, unknown>
      const parts = Object.entries(changes).flatMap(([field, change]) => {
        if (field === 'profilesAdded' || field === 'profilesRemoved')
          return [t(`audit.details.${field}`, { names: list(change).join(', ') })]
        const { before, after } = change as { before: unknown; after: unknown }
        return [`${t(`audit.fields.${field}`, { defaultValue: field })}: ${String(before ?? '–')} → ${String(after ?? '–')}`]
      })
      return { what: t('audit.actions.user.updated'), detail: parts.join(' · ') }
    }
    case 'user.invited':
    case 'user.password.reset':
    case 'user.disabled':
    case 'user.enabled': {
      const keys: Record<string, string> = {
        'user.invited': 'invited',
        'user.password.reset': 'passwordReset',
        'user.disabled': 'disabled',
        'user.enabled': 'enabled',
      }
      return { what: t(`audit.actions.user.${keys[event.action]}`), detail: String(d.email ?? '') }
    }
    case 'subscription.changed': {
      const added = list(d.modulesAdded)
      const removed = list(d.modulesRemoved)
      return {
        what: t(d.previousPlan ? 'audit.actions.subscription.changed' : 'audit.actions.subscription.created'),
        detail: [
          d.previousPlan ? `${String(d.previousPlan)} → ${String(d.plan)}` : String(d.plan) + (d.trial ? ` · ${t('audit.details.trial')}` : ''),
          ...(added.length ? [t('audit.details.modulesAdded', { names: added.join(', ') })] : []),
          ...(removed.length ? [t('audit.details.modulesRemoved', { names: removed.join(', ') })] : []),
        ].join(' · '),
      }
    }
    case 'module.provisioned':
      return { what: t('audit.actions.module.provisioned'), detail: `${String(d.module)} v${String(d.version)}` }
    case 'module.provisioning.failed':
      return { what: t('audit.actions.module.provisioningFailed'), detail: `${String(d.module)} v${String(d.version)} · ${String(d.error ?? '')}` }
    case 'module.registered':
      return { what: t('audit.actions.module.registered'), detail: `v${String(d.version)}` }
    case 'module.updated':
      return {
        what: t('audit.actions.module.updated'),
        detail: d.status ? t(`catalog.moduleStatus.${String(d.status)}`) : `v${String(d.version)}`,
      }
    case 'plan.created':
    case 'plan.updated':
      return {
        what: t(event.action === 'plan.created' ? 'audit.actions.plan.created' : 'audit.actions.plan.updated'),
        detail: `${String(d.price)} € · ${t(`catalog.period.${String(d.billingPeriod)}`)}`,
      }
    default:
      return { what: event.action, detail: '' }
  }
}
