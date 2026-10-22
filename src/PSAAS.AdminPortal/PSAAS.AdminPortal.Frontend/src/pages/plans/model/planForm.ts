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
  moduleIds: z.array(z.string()),
  active: z.boolean(),
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
  moduleIds: plan.moduleIds.filter((id) => id !== 'core'),
  active: plan.active,
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
})
