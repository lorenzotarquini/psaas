import type { TenantDetail } from '@/entities/tenant'

/** The five steps of mock 1e, in order. */
export const steps = ['registry', 'plan', 'organization', 'users', 'summary'] as const
export type Step = (typeof steps)[number]

export const stepIndex = (step: Step) => steps.indexOf(step)
export const nextStep = (step: Step): Step | undefined => steps[stepIndex(step) + 1]

/** Step from the URL; anything unknown (or a missing draft) starts from the registry. */
export function readStep(value: string | null, hasDraft: boolean): Step {
  if (!hasDraft) return 'registry'
  return (steps as readonly string[]).includes(value ?? '') ? (value as Step) : 'registry'
}

export type CheckState = 'ok' | 'error' | 'pending'

export interface Check {
  key: 'companyName' | 'completeVatNumber' | 'certifiedEmail' | 'address' | 'plan'
  state: CheckState
  /** i18n key of the second line, with its values. */
  detail: { key: string; values?: Record<string, string> }
}

interface UniqueStatus {
  verified: boolean
  conflictingTenant: { companyName: string } | null
}

/**
 * The "Controlli" panel of mock 1e: one line per rule of saving (UC3), green when the backend confirmed it,
 * red with the reason, grey while there is nothing to check yet.
 */
export function buildChecks(input: {
  values: { companyName: string; completeVatNumber: string; certifiedEmail: string; city: string; postalCode: string }
  errors: Partial<Record<'companyName' | 'completeVatNumber' | 'certifiedEmail' | 'city' | 'postalCode', string>>
  unique: (field: 'companyName' | 'completeVatNumber' | 'certifiedEmail') => UniqueStatus
  planChosen: boolean
}): Check[] {
  const { values, errors, unique, planChosen } = input
  const uniqueCheck = (key: 'companyName' | 'completeVatNumber' | 'certifiedEmail'): Check => {
    const { verified, conflictingTenant } = unique(key)
    if (conflictingTenant) return { key, state: 'error', detail: { key: 'onboarding.checks.usedBy', values: { name: conflictingTenant.companyName } } }
    if (errors[key]) return { key, state: 'error', detail: { key: `validation.${errors[key]}` } }
    if (verified) return { key, state: 'ok', detail: { key: 'onboarding.checks.verified' } }
    return { key, state: 'pending', detail: { key: values[key].trim() ? 'onboarding.checks.toVerify' : 'onboarding.checks.empty' } }
  }
  const addressError = errors.city ?? errors.postalCode
  const address: Check = addressError
    ? { key: 'address', state: 'error', detail: { key: `validation.${addressError}` } }
    : values.city.trim() && values.postalCode.trim()
      ? { key: 'address', state: 'ok', detail: { key: 'onboarding.checks.addressOk', values: { city: values.city, postalCode: values.postalCode } } }
      : { key: 'address', state: 'pending', detail: { key: 'onboarding.checks.waitingCity' } }
  return [
    uniqueCheck('companyName'),
    uniqueCheck('completeVatNumber'),
    uniqueCheck('certifiedEmail'),
    address,
    planChosen
      ? { key: 'plan', state: 'ok', detail: { key: 'onboarding.checks.planChosen' } }
      : { key: 'plan', state: 'pending', detail: { key: 'onboarding.checks.planStep' } },
  ]
}

/** A draft is resumed where it makes sense: an organization or users already there means further on. */
export const isDraft = (tenant: TenantDetail | undefined) => tenant?.lifecycle.status === 'Onboarding'
