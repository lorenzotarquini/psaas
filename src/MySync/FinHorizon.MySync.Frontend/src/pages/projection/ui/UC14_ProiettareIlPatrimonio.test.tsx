import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { FakeApi } from '@/test/fakeApi'
import { renderApp } from '@/test/renderApp'

vi.mock('@/shared/auth', () => ({
  session: { headers: () => Promise.resolve({}), userName: () => 'Mario', profile: () => ({ firstName: 'Mario', lastName: 'Rossi', email: 'mario@example.com', username: 'mario' }), login: vi.fn(), logout: vi.fn(), init: vi.fn() },
}))

// Recharts measures its container; jsdom has no layout, so the chart only needs an observer that never fires.
vi.stubGlobal(
  'ResizeObserver',
  class {
    observe() {}
    unobserve() {}
    disconnect() {}
  },
)

/** UC14-S2 on 3 months: liquidity 1.000,00 €, income 2.000,00 €, fixed 700,00 €, variable 600,00 €. */
function givenS2(monthsOfData = 6) {
  const api = new FakeApi()
  api.givenAccount('Conto Intesa', 1000)
  api.projection.monthsOfData = monthsOfData
  api.projection.suggested = { income: 2000, fixedExpenses: 700, variableExpenses: 600, saving: 0, annualReturn: 0, inflation: 2, payments: [], subscriptions: [] }
  api.projection.saved = {
    horizonMonths: 3,
    income: 2000,
    fixedExpenses: 700,
    variableExpenses: 600,
    saving: 0,
    annualReturn: 0,
    inflation: 2,
    inflationOn: false,
    payments: [],
    subscriptions: [],
    events: [],
    goal: null,
  }
  return api
}

const finalValue = () => screen.getByTestId('projection-final').textContent

// docs/mysync/use-cases/UC14-proiettare-il-patrimonio.md
describe('UC14_ProiettareIlPatrimonio', () => {
  it('S8_IlGraficoCambiaSubito', async () => {
    const api = givenS2()
    renderApp('/projection', api)
    const user = userEvent.setup()

    await waitFor(() => expect(finalValue()).toMatch(/3100,00/))
    expect(screen.getByText('Patrimonio a febbraio 2027')).toBeTruthy()
    const calls = api.requests.length

    const variable = screen.getByLabelText('Spese variabili al mese')
    await user.clear(variable)
    await user.type(variable, '400')

    expect(finalValue()).toMatch(/3700,00/)
    expect(api.requests.length).toBe(calls)
  })

  it('S15_EStaccassiUnAbbonamento', async () => {
    const api = givenS2()
    api.projection.subscriptions = [{ id: 'palestra', name: 'Palestra', amount: 45, firstChargeOn: '2026-10-03', endsOn: null }]
    api.projection.saved = { ...api.projection.saved!, horizonMonths: 12, subscriptions: [{ subscriptionId: 'palestra', amount: 45, included: true }] }
    renderApp('/projection', api)
    const user = userEvent.setup()
    await waitFor(() => expect(finalValue()).toMatch(/8860,00/))
    const calls = api.requests.length

    await user.click(screen.getByRole('checkbox', { name: 'Tieni Palestra nella proiezione' }))

    expect(finalValue()).toMatch(/9400,00/)
    expect(api.requests.length).toBe(calls)
    expect(api.requests.some((request) => request.path.startsWith('/api/subscriptions/'))).toBe(false)
  })

  it('3a_AvvisoConPochiDati', async () => {
    renderApp('/projection', givenS2(1))

    expect(await screen.findByText('La stima si basa su 1 mesi di dati: controlla i parametri.')).toBeTruthy()
  })

  it('S9_SalvareERipartire', async () => {
    const api = givenS2()
    renderApp('/projection', api)
    const user = userEvent.setup()
    await waitFor(() => expect(finalValue()).toMatch(/3100,00/))

    await user.click(screen.getByRole('button', { name: '5 anni' }))
    await user.click(screen.getByRole('button', { name: 'Salva' }))

    expect(await screen.findByText('Proiezione salvata.')).toBeTruthy()
    expect(api.requests.find((r) => r.method === 'PUT' && r.path === '/api/projection')?.body).toMatchObject({ horizonMonths: 60, variableExpenses: 600, events: [] })

    await user.click(screen.getByRole('button', { name: 'Riparti dai miei dati' }))

    await waitFor(() => expect((screen.getByLabelText('Orizzonte (mesi)') as HTMLInputElement).value).toBe('12'))
    expect(api.projection.saved).toBeNull()
  })

  it('UnCampoNonValidoNonSiSalva', async () => {
    renderApp('/projection', givenS2())
    const user = userEvent.setup()
    await waitFor(() => expect(finalValue()).toMatch(/3100,00/))

    const horizon = screen.getByLabelText('Orizzonte (mesi)')
    await user.clear(horizon)
    await user.type(horizon, '361')

    expect(within(horizon.closest('.fieldset') as HTMLElement).getByRole('alert').textContent).toBe('Valore non valido')
    expect((screen.getByRole('button', { name: 'Salva' }) as HTMLButtonElement).disabled).toBe(true)
    // The chart keeps the last valid horizon, 36 months while typing "361": 1.000 + 36 × 700.
    expect(finalValue()).toMatch(/26\.200,00/)
  })
})
