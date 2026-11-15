import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { FakeApi } from '@/test/fakeApi'
import { renderApp } from '@/test/renderApp'

vi.mock('@/shared/auth', () => ({
  session: { headers: () => Promise.resolve({}), userName: () => 'Mario', profile: () => ({ firstName: 'Mario', lastName: 'Rossi', email: 'mario@example.com', username: 'mario' }), login: vi.fn(), logout: vi.fn(), init: vi.fn() },
}))

// docs/mysync/use-cases/UC12-vista-leggera-e-importi-nascosti.md
describe('UC12_VistaLeggera', () => {
  it('S6_AndamentoSenzaVociOpzionaliDiDefault', async () => {
    const api = new FakeApi()
    renderApp('/net-worth', api)
    const user = userEvent.setup()
    const trendUrls = () => api.urls.filter((url) => url.startsWith('/api/net-worth'))

    const stable = (await screen.findByRole('checkbox', { name: 'Patrimonio stabile e debiti a lungo termine' })) as HTMLInputElement
    const investments = screen.getByRole('checkbox', { name: 'Investimenti e depositi' }) as HTMLInputElement
    expect([stable.checked, investments.checked]).toEqual([false, false])
    await waitFor(() => expect(trendUrls()).toEqual(['/api/net-worth?period=30d&includeStable=false&includeInvestments=false']))

    await user.click(investments)
    await waitFor(() => expect(trendUrls().at(-1)).toBe('/api/net-worth?period=30d&includeStable=false&includeInvestments=true'))

    await user.click(stable)
    await waitFor(() => expect(trendUrls().at(-1)).toBe('/api/net-worth?period=30d&includeStable=true&includeInvestments=true'))
  })
})
