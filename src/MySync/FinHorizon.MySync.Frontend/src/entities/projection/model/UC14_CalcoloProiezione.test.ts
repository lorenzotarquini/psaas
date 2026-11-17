import { describe, expect, it } from 'vitest'
import type { ProjectionDebt, ProjectionParameters } from '../api/projection'
import { project, type ProjectionInput } from './project'

const today = '2026-11-17'
const cents = (value: number) => Math.round(value * 100) / 100

const parameters = (overrides: Partial<ProjectionParameters> = {}): ProjectionParameters => ({
  horizonMonths: 3,
  income: 0,
  fixedExpenses: 0,
  variableExpenses: 0,
  saving: 0,
  annualReturn: 0,
  inflation: 2,
  inflationOn: false,
  payments: [],
  events: [],
  goal: null,
  ...overrides,
})

const input = (overrides: Partial<ProjectionParameters> = {}, liquidity = 0, investments = 0, debts: ProjectionDebt[] = []): ProjectionInput => ({
  today,
  start: { liquidity, investments, stable: 0 },
  debts,
  parameters: parameters(overrides),
})

/** UC14-S2: liquidity 1.000,00 €, income 2.000,00 €, fixed 700,00 €, variable 600,00 €. */
const s2 = (overrides: Partial<ProjectionParameters> = {}) => input({ income: 2000, fixedExpenses: 700, variableExpenses: 600, ...overrides }, 1000)

// docs/mysync/use-cases/UC14-proiettare-il-patrimonio.md (R4..R8)
describe('UC14_CalcoloProiezione', () => {
  it('S2_CrescitaMensile', () => {
    const result = project(s2())

    expect(result.points.map((point) => [point.date, point.netWorth])).toEqual([
      ['2026-11', 1000],
      ['2026-12', 1700],
      ['2027-01', 2400],
      ['2027-02', 3100],
    ])
    expect(result.change).toBe(2100)
  })

  it('S3_RendimentoDegliInvestimenti', () => {
    const result = project(input({ horizonMonths: 2, saving: 100, annualReturn: 6 }, 1000, 10000))

    expect(result.points.slice(1).map((point) => [point.date, cents(point.liquidity), cents(point.investments), cents(point.netWorth)])).toEqual([
      ['2026-12', 900, 10150, 11050],
      ['2027-01', 800, 10300.75, 11100.75],
    ])
  })

  it('S4_IlDebitoEstintoLiberaLaRata', () => {
    const auto: ProjectionDebt = { id: 'auto', name: 'Auto', residual: 500, monthlyPayment: 200 }

    const result = project(input({ horizonMonths: 4, income: 1000, payments: [{ debtId: 'auto', amount: 200 }] }, 0, 0, [auto]))

    expect(result.points.slice(1).map((point) => [point.date, point.netWorth, point.debts])).toEqual([
      ['2026-12', 800, 300],
      ['2027-01', 1600, 100],
      ['2027-02', 2500, 0],
      ['2027-03', 3500, 0],
    ])
    expect(result.payoffs).toEqual([{ debtId: 'auto', name: 'Auto', date: '2027-02', withoutPayment: false }])
  })

  it('S5_EventiELiquiditaNegativa', () => {
    const result = project(
      s2({
        events: [
          { month: 2, amount: 1500, note: 'Tredicesima' },
          { month: 3, amount: -5000, note: 'Auto nuova' },
        ],
      }),
    )

    expect(result.points.slice(1).map((point) => point.netWorth)).toEqual([1700, 3900, -400])
    expect(result.negativeLiquidity).toBe('2027-02')
  })

  it.each([
    [5000, { status: 'reached', date: '2027-05' }],
    [20000, { status: 'notReached' }],
    [500, { status: 'already' }],
  ])('S6_Obiettivo %d', (goal, outcome) => {
    expect(project(s2({ horizonMonths: 12, goal })).goal).toEqual(outcome)
  })

  it('S7_PotereDAcquisto', () => {
    const on = project(input({ horizonMonths: 12, inflationOn: true }, 10000))
    const off = project(input({ horizonMonths: 12, inflationOn: false }, 10000))

    expect([on.last.date, cents(on.last.netWorth)]).toEqual(['2027-11', 9803.92])
    expect(off.last.netWorth).toBe(10000)
  })

  it('3b_UnDebitoSenzaRataNonScende', () => {
    const result = project(input({ horizonMonths: 2 }, 0, 0, [{ id: 'fido', name: 'Fido', residual: 300, monthlyPayment: null }]))

    expect(result.last.debts).toBe(300)
    expect(result.payoffs[0]).toMatchObject({ date: null, withoutPayment: true })
  })
})
