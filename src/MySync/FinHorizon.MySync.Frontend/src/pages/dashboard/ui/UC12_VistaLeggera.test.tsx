import { act, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { FakeApi } from '@/test/fakeApi'
import { renderApp } from '@/test/renderApp'

vi.mock('@/shared/auth', () => ({
  session: { headers: () => Promise.resolve({}), userName: () => 'Mario', profile: () => ({ firstName: 'Mario', lastName: 'Rossi', email: 'mario@example.com', username: 'mario' }), login: vi.fn(), logout: vi.fn(), init: vi.fn() },
}))

function givenHeavyAndLight() {
  const api = new FakeApi()
  api.limits = { maxAccounts: 4, maxDebts: 2 }
  const credem = api.givenAccount('Credem', 6000)
  api.givenAccount('Oro', 20000, 'Valuables')
  api.givenAccount('PAC Fideuram', 3000, 'SavingsPlan')
  api.debts.push({
    id: 'mutuo',
    kind: 'Mortgage',
    name: 'Mutuo Prima Casa',
    description: null,
    tag: null,
    accountId: credem.id,
    initialResidual: 80000,
    residual: 80000,
    createdOn: '2026-11-01',
    paidOff: false,
    group: 'Stable',
    currency: 'EUR',
    displayResidual: 80000,
  })
  return api
}

// docs/mysync/use-cases/UC12-vista-leggera-e-importi-nascosti.md
describe('UC12_VistaLeggera', () => {
  it('S4_SwitchPatrimonioPesante', async () => {
    const api = givenHeavyAndLight()
    renderApp('/', api)
    const user = userEvent.setup()

    const toggle = (await screen.findByRole('checkbox', { name: 'Patrimonio stabile e debiti a lungo termine' })) as HTMLInputElement
    expect(toggle.checked).toBe(false)
    expect(screen.getByText('Credem')).toBeTruthy()
    expect(screen.queryByText('Oro')).toBeNull()
    expect(screen.queryByText('Mutuo Prima Casa')).toBeNull()
    expect(screen.getByText('1 bene stabile nascosto')).toBeTruthy()
    expect(screen.getByText('1 debito a lungo termine nascosto')).toBeTruthy()
    expect(api.requests.some((r) => r.path === '/api/dashboard')).toBe(true)

    await user.click(toggle)
    await user.click(screen.getByRole('button', { name: 'Mostra gli importi' }))

    expect(await screen.findByText('Oro')).toBeTruthy()
    expect(screen.getByText('Mutuo Prima Casa')).toBeTruthy()
    await waitFor(() => expect(screen.getByText(/26\.000,00/)).toBeTruthy())
  })

  it('S5_ImportiNascostiDiDefault', async () => {
    renderApp('/', givenHeavyAndLight())
    const user = userEvent.setup()

    const reveal = await screen.findByRole('button', { name: 'Mostra gli importi' })
    expect(reveal.getAttribute('aria-pressed')).toBe('true')
    expect(reveal.textContent).toBe('Mostra')
    expect(screen.queryByText(/6000,00/)).toBeNull()
    expect(screen.getAllByLabelText('Importo nascosto').length).toBeGreaterThan(0)

    await user.click(reveal)

    expect(screen.getAllByText(/6000,00/).length).toBeGreaterThan(0)
    const hide = screen.getByRole('button', { name: 'Nascondi gli importi' })
    expect(hide.getAttribute('aria-pressed')).toBe('false')
    expect(hide.textContent).toBe('Nascondi')
  })

  it('S7_SceltaRicordataNellaSessione', async () => {
    const { router } = renderApp('/', givenHeavyAndLight())
    const user = userEvent.setup()

    await user.click(await screen.findByRole('checkbox', { name: 'Patrimonio stabile e debiti a lungo termine' }))
    await user.click(screen.getByRole('button', { name: 'Mostra gli importi' }))
    await act(() => router.navigate('/net-worth'))

    const trendToggle = (await screen.findByRole('checkbox', { name: 'Patrimonio stabile e debiti a lungo termine' })) as HTMLInputElement
    expect(trendToggle.checked).toBe(true)

    await act(() => router.navigate('/'))

    expect(await screen.findByRole('button', { name: 'Nascondi gli importi' })).toBeTruthy()
    expect(sessionStorage.getItem('mysync.amountsHidden')).toBe('false')
  })

  it('S9_SwitchInvestimentiEDepositi', async () => {
    renderApp('/', givenHeavyAndLight())
    const user = userEvent.setup()

    const investments = (await screen.findByRole('checkbox', { name: 'Investimenti e depositi' })) as HTMLInputElement
    expect(investments.checked).toBe(false)
    expect(screen.queryByText('PAC Fideuram')).toBeNull()
    expect(screen.getByText('1 investimento o deposito nascosto')).toBeTruthy()

    await user.click(investments)

    expect(await screen.findByText('PAC Fideuram')).toBeTruthy()
    expect(screen.queryByText('Oro')).toBeNull()
    expect(screen.queryByText('1 investimento o deposito nascosto')).toBeNull()
  })
})
