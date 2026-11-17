import type { ProjectionDebt, ProjectionParameters, ProjectionStart } from '../api/projection'

export interface ProjectionPoint {
  /** 0 is today. */
  month: number
  /** The calendar month, yyyy-MM. */
  date: string
  liquidity: number
  investments: number
  stable: number
  netWorth: number
  debts: number
}

export type GoalOutcome = { status: 'none' } | { status: 'already' } | { status: 'reached'; date: string } | { status: 'notReached' }

export interface DebtPayoff {
  debtId: string
  name: string
  /** yyyy-MM, or null when the debt is not paid off within the horizon. */
  date: string | null
  /** UC14-3b: no payment, so the residual never goes down. */
  withoutPayment: boolean
}

export interface ProjectionResult {
  points: ProjectionPoint[]
  last: ProjectionPoint
  /** Net worth at the end minus today. */
  change: number
  goal: GoalOutcome
  /** UC14-5a: the first month the liquidity goes below zero. */
  negativeLiquidity: string | null
  payoffs: DebtPayoff[]
}

export interface ProjectionInput {
  /** yyyy-MM-dd. */
  today: string
  start: ProjectionStart
  debts: ProjectionDebt[]
  parameters: ProjectionParameters
}

/** yyyy-MM of the month `offset` months after the one of `today`. */
export function monthAfter(today: string, offset: number): string {
  const [year, month] = today.split('-').map(Number)
  const index = year * 12 + (month - 1) + offset
  return `${Math.floor(index / 12)}-${String((index % 12) + 1).padStart(2, '0')}`
}

/**
 * UC14-R4..R8: the projection month by month. Nothing is rounded here (R6): values are rounded to cents only when shown.
 * With the inflation switch on, every value of month m is shown divided by (1 + inflation)^(m/12); the computation does not change (R8).
 */
export function project({ today, start, debts, parameters }: ProjectionInput): ProjectionResult {
  const { horizonMonths, income, fixedExpenses, variableExpenses, saving, annualReturn, inflation, inflationOn, payments, events, goal } = parameters
  const monthlyReturn = annualReturn / 100 / 12
  const residuals = new Map(debts.map((debt) => [debt.id, debt.residual]))
  const paymentOf = (debtId: string) => payments.find((payment) => payment.debtId === debtId)?.amount ?? 0
  const payoffMonth = new Map<string, number>()
  let liquidity = start.liquidity
  let investments = start.investments
  const raw: Omit<ProjectionPoint, 'date'>[] = [point(0)]

  function point(month: number) {
    const debtsLeft = [...residuals.values()].reduce((sum, value) => sum + value, 0)
    return { month, liquidity, investments, stable: start.stable, netWorth: liquidity + investments + start.stable, debts: debtsLeft }
  }

  for (let month = 1; month <= horizonMonths; month++) {
    investments = investments * (1 + monthlyReturn) + saving
    let paid = 0
    for (const debt of debts) {
      const residual = residuals.get(debt.id) ?? 0
      if (residual <= 0) continue
      // R5: an extinguished debt is not paid any more, so its payment stays in the liquidity.
      const payment = Math.min(paymentOf(debt.id), residual)
      paid += payment
      residuals.set(debt.id, residual - payment)
      if (residual - payment <= 0 && payment > 0) payoffMonth.set(debt.id, month)
    }
    const oneOff = events.filter((item) => item.month === month).reduce((sum, item) => sum + item.amount, 0)
    liquidity = liquidity + income - fixedExpenses - variableExpenses - paid - saving + oneOff
    raw.push(point(month))
  }

  const deflate = (value: number, month: number) => (inflationOn ? value / Math.pow(1 + inflation / 100, month / 12) : value)
  const points: ProjectionPoint[] = raw.map((item) => ({
    month: item.month,
    date: monthAfter(today, item.month),
    liquidity: deflate(item.liquidity, item.month),
    investments: deflate(item.investments, item.month),
    stable: deflate(item.stable, item.month),
    netWorth: deflate(item.netWorth, item.month),
    debts: deflate(item.debts, item.month),
  }))
  const last = points[points.length - 1]

  let outcome: GoalOutcome = { status: 'none' }
  if (goal !== null && goal > 0) {
    // R7: the first month at or above the goal, comparing the values as shown.
    const reached = points.find((item) => item.netWorth >= goal)
    outcome = !reached ? { status: 'notReached' } : reached.month === 0 ? { status: 'already' } : { status: 'reached', date: reached.date }
  }

  const negative = points.find((item) => item.month > 0 && item.liquidity < 0)
  return {
    points,
    last,
    change: last.netWorth - points[0].netWorth,
    goal: outcome,
    negativeLiquidity: negative?.date ?? null,
    payoffs: debts.map((debt) => {
      const month = payoffMonth.get(debt.id)
      return { debtId: debt.id, name: debt.name, date: month === undefined ? null : monthAfter(today, month), withoutPayment: paymentOf(debt.id) <= 0 }
    }),
  }
}
