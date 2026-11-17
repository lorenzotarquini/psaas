import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { FakeApi } from '@/test/fakeApi'
import { renderApp } from '@/test/renderApp'

vi.mock('@/shared/auth', () => ({
  session: { headers: () => Promise.resolve({}), userName: () => 'Mario', profile: () => ({ firstName: 'Mario', lastName: 'Rossi', email: 'mario@example.com', username: 'mario' }), login: vi.fn(), logout: vi.fn(), init: vi.fn() },
}))

// docs/mysync/use-cases/UC4-consultare-dashboard.md
describe('UC4_ConsultareDashboard', () => {
  it('S6_RiquadroProiezione', async () => {
    const api = new FakeApi()
    api.givenAccount('Conto Intesa', 1000)
    api.projection.suggested = { income: 2000, fixedExpenses: 700, variableExpenses: 600, saving: 0, annualReturn: 0, inflation: 2, payments: [] }
    const { router } = renderApp('/', api)
    const user = userEvent.setup()

    await user.click(await screen.findByRole('button', { name: 'Mostra gli importi' }))

    const card = await screen.findByRole('link', { name: 'Tra 12 mesi (patrimonio totale)' })
    await waitFor(() => expect(card.textContent).toMatch(/9400,00/))

    await user.click(card)

    await waitFor(() => expect(router.state.location.pathname).toBe('/projection'))
  })
})
