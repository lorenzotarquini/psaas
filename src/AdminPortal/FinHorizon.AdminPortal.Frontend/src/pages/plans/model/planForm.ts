import { z } from 'zod'
import type { Plan, PlanRequest } from '@/entities/catalog'

// Numbers stay strings in the form (empty = unlimited) and are converted on submit; the backend has the final word.
const count = z.string().trim().regex(/^\d*$/, 'InvalidValue')

export const planSchema = z.object({
  code: z.string().trim().min(1, 'Required').regex(/^[a-z0-9][a-z0-9-]{1,29}$/, 'InvalidFormat'),
  name: z.string().trim().min(1, 'Required'),
  description: z.string(),
  price: z.string().trim().min(1, 'Required').regex(/^\d+([.,]\d{1,2})?$/, 'InvalidValue'),
  billingPeriod: z.enum(['Monthly', 'Yearly']),
  trialDays: count,
  maxUsers: count,
  maxProductiveUnits: count,
  // UC16-R2: one module, kept as a list of one for the API.
  moduleIds: z.array(z.string()).length(1, 'Required'),
  active: z.boolean(),
  vatRate: z.enum(['22', '10', '5', '4', '0']),
  vatNature: z.string().trim(),
  priceIncludesVat: z.boolean(),
  // UC16-R6: required by the backend when the module declares tiers.
  tier: z.string(),
}).superRefine((plan, context) => {
  // UC16-R4: the nature of the VAT only, and always, with 0%.
  if (plan.vatRate === '0' && !/^N[1-7](\.[1-9])?$/i.test(plan.vatNature)) {
    context.addIssue({ code: 'custom', path: ['vatNature'], message: plan.vatNature ? 'InvalidValue' : 'Required' })
  }
})

export type PlanValues = z.input<typeof planSchema>

export const emptyPlan: PlanValues = {
  code: '',
  name: '',
  description: '',
  price: '',
  billingPeriod: 'Monthly',
  trialDays: '0',
  maxUsers: '',
  maxProductiveUnits: '',
  moduleIds: [],
  active: true,
  vatRate: '22',
  vatNature: '',
  priceIncludesVat: true,
  tier: '',
}

export const planToValues = (plan: Plan): PlanValues => ({
  code: plan.code,
  name: plan.name,
  description: plan.description ?? '',
  price: String(plan.price),
  billingPeriod: plan.billingPeriod,
  trialDays: String(plan.trialDays),
  maxUsers: plan.maxUsers?.toString() ?? '',
  maxProductiveUnits: plan.maxProductiveUnits?.toString() ?? '',
  moduleIds: plan.moduleIds.slice(0, 1),
  active: plan.active,
  vatRate: String(plan.vatRate) as PlanValues['vatRate'],
  vatNature: plan.vatNature ?? '',
  priceIncludesVat: plan.priceIncludesVat,
  tier: plan.tier?.toString() ?? '',
})

const optionalNumber = (value: string) => (value.trim() ? Number(value) : null)

export const toPlanRequest = (values: z.output<typeof planSchema>): PlanRequest => ({
  code: values.code,
  name: values.name,
  description: values.description.trim() || null,
  price: Number(values.price.replace(',', '.')),
  billingPeriod: values.billingPeriod,
  trialDays: optionalNumber(values.trialDays) ?? 0,
  maxUsers: optionalNumber(values.maxUsers),
  maxProductiveUnits: optionalNumber(values.maxProductiveUnits),
  moduleIds: values.moduleIds,
  active: values.active,
  vatRate: Number(values.vatRate),
  vatNature: values.vatRate === '0' ? values.vatNature.toUpperCase() : null,
  priceIncludesVat: values.priceIncludesVat,
  tier: values.tier ? Number(values.tier) : null,
})
