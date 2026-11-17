import type { ProjectionParameters } from '@/entities/projection'
import { amountText, fitsDecimals, parseAmount } from '@/shared/lib/form'

/** UC14-R9: the limits of the parameters, the same as the backend's. */
export const limits = { minHorizon: 1, maxHorizon: 360, maxEvents: 10, maxNote: 40, minReturn: -10, maxReturn: 20, minInflation: 0, maxInflation: 20 } as const

export interface EventValues {
  month: string
  amount: string
  note: string
}

/** The parameters as the user types them: text, so a half-typed "12," is not lost. */
export interface ParameterValues {
  horizonMonths: string
  income: string
  fixedExpenses: string
  variableExpenses: string
  saving: string
  annualReturn: string
  inflation: string
  inflationOn: boolean
  payments: Record<string, string>
  events: EventValues[]
  goal: string
}

/** Field → error code (the backend's codes, RNF5); nested fields as "events.0.month" and "payments.<debtId>". */
export type ParameterErrors = Record<string, string>

const percentText = (value: number) => String(value).replace('.', ',')

export function toValues(parameters: ProjectionParameters): ParameterValues {
  return {
    horizonMonths: String(parameters.horizonMonths),
    income: amountText(parameters.income),
    fixedExpenses: amountText(parameters.fixedExpenses),
    variableExpenses: amountText(parameters.variableExpenses),
    saving: amountText(parameters.saving),
    annualReturn: percentText(parameters.annualReturn),
    inflation: percentText(parameters.inflation),
    inflationOn: parameters.inflationOn,
    payments: Object.fromEntries(parameters.payments.map((payment) => [payment.debtId, amountText(payment.amount)])),
    events: parameters.events.map((item) => ({ month: String(item.month), amount: amountText(item.amount), note: item.note })),
    goal: parameters.goal === null ? '' : amountText(parameters.goal),
  }
}

const isInteger = (text: string) => /^\d+$/.test(text.trim())
const isNumber = (text: string) => /^-?\d+(?:[.,]\d+)?$/.test(text.trim())

function monthlyAmount(text: string): string | undefined {
  if (!text.trim()) return 'Required'
  if (!fitsDecimals(text, 2)) return 'InvalidFormat'
  return parseAmount(text) < 0 ? 'InvalidValue' : undefined
}

function percent(text: string, min: number, max: number): string | undefined {
  if (!text.trim()) return 'Required'
  if (!isNumber(text)) return 'InvalidFormat'
  const value = parseAmount(text)
  return value < min || value > max ? 'InvalidValue' : undefined
}

/** UC14-R9 in the browser: the parameters when every field is valid, otherwise the errors field by field. */
export function parse(values: ParameterValues): { parameters: ProjectionParameters; errors: null } | { parameters: null; errors: ParameterErrors } {
  const errors: ParameterErrors = {}
  const set = (field: string, code: string | undefined) => {
    if (code) errors[field] = code
  }
  const horizon = Number(values.horizonMonths)
  set('horizonMonths', !values.horizonMonths.trim() ? 'Required' : !isInteger(values.horizonMonths) || horizon < limits.minHorizon || horizon > limits.maxHorizon ? 'InvalidValue' : undefined)
  for (const field of ['income', 'fixedExpenses', 'variableExpenses', 'saving'] as const) set(field, monthlyAmount(values[field]))
  set('annualReturn', percent(values.annualReturn, limits.minReturn, limits.maxReturn))
  set('inflation', percent(values.inflation, limits.minInflation, limits.maxInflation))
  for (const [debtId, text] of Object.entries(values.payments)) set(`payments.${debtId}`, monthlyAmount(text))
  if (values.goal.trim()) set('goal', !fitsDecimals(values.goal, 2) ? 'InvalidFormat' : parseAmount(values.goal) <= 0 ? 'InvalidValue' : undefined)
  values.events.forEach((item, index) => {
    const month = Number(item.month)
    set(`events.${index}.month`, !item.month.trim() ? 'Required' : !isInteger(item.month) || month < 1 || (Number.isFinite(horizon) && month > horizon) ? 'InvalidValue' : undefined)
    set(`events.${index}.amount`, !item.amount.trim() ? 'Required' : !fitsDecimals(item.amount, 2) ? 'InvalidFormat' : parseAmount(item.amount) === 0 ? 'InvalidValue' : undefined)
    set(`events.${index}.note`, !item.note.trim() ? 'Required' : item.note.trim().length > limits.maxNote ? 'MaxLengthExceeded' : undefined)
  })
  if (values.events.length > limits.maxEvents) errors.events = 'InvalidValue'
  if (Object.keys(errors).length > 0) return { parameters: null, errors }

  return {
    errors: null,
    parameters: {
      horizonMonths: horizon,
      income: parseAmount(values.income),
      fixedExpenses: parseAmount(values.fixedExpenses),
      variableExpenses: parseAmount(values.variableExpenses),
      saving: parseAmount(values.saving),
      annualReturn: parseAmount(values.annualReturn),
      inflation: parseAmount(values.inflation),
      inflationOn: values.inflationOn,
      payments: Object.entries(values.payments).map(([debtId, text]) => ({ debtId, amount: parseAmount(text) })),
      events: values.events.map((item) => ({ month: Number(item.month), amount: parseAmount(item.amount), note: item.note.trim() })),
      goal: values.goal.trim() ? parseAmount(values.goal) : null,
    },
  }
}
