import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { SettingsPage } from '@/pages/settings'
import { AccountsCard } from '@/widgets/portfolio-lists'
import { FakeApi } from '@/test/fakeApi'
import { renderApp, renderWithApi } from '@/test/renderApp'
import { AccountForm } from '@/features/account-form'

vi.mock('@/shared/auth', () => ({
  session: {
    headers: () => Promise.resolve({}),
    userName: () => 'Mario',
    profile: () => ({ firstName: 'Mario', lastName: 'Rossi', email: 'mario@example.com', username: 'mario' }),
    updateProfile: vi.fn(),
    changePassword: vi.fn(),
    login: vi.fn(),
    logout: vi.fn(),
    init: vi.fn(),
  },
}))

const optionsOf = (select: HTMLElement) => within(select).getAllByRole('option').map((option) => (option as HTMLOptionElement).value)

// docs/mysync/use-cases/UC13-valute-e-moneta-virtuale.md (interface scenarios)
describe('UC13_Valute', () => {
  it('S13_ValutaDelContoSecondoIlTipo', async () => {
    const { api } = renderWithApi(<AccountForm />)
    const user = userEvent.setup()
    const currency = await screen.findByLabelText('Valuta')
    await waitFor(() => expect(optionsOf(currency)).toEqual(['EUR', 'USD']))

    await user.selectOptions(screen.getByLabelText('Tipo di conto'), 'VirtualCurrency')
    expect(optionsOf(screen.getByLabelText('Valuta'))).toEqual(['BTC', 'ETH'])
    expect((screen.getByLabelText('Valuta') as HTMLSelectElement).value).toBe('BTC')

    await user.type(screen.getByLabelText('Nome'), 'Ledger')
    await user.type(screen.getByLabelText('Saldo iniziale'), '0,12345678')
    await user.click(screen.getByRole('button', { name: 'Crea conto' }))

    await waitFor(() =>
      expect(api.requests.find((r) => r.method === 'POST' && r.path === '/api/accounts')?.body).toMatchObject({
        kind: 'VirtualCurrency',
        currency: 'BTC',
        initialBalance: 0.12345678,
      }),
    )
  })

  it('S14_ValutaNellaConfigurazione', async () => {
    const { api } = renderApp('/setup')
    const user = userEvent.setup()

    await screen.findByRole('heading', { name: 'In che valuta vuoi vedere il tuo patrimonio?' })
    await waitFor(() => expect(optionsOf(screen.getByLabelText('Valuta di visualizzazione'))).toContain('USD'))
    await user.selectOptions(screen.getByLabelText('Valuta di visualizzazione'), 'USD')
    await user.click(screen.getByRole('button', { name: 'Avanti' }))

    expect(await screen.findByRole('heading', { name: 'I tuoi conti' })).toBeTruthy()
    expect(api.preferences).toEqual({ displayCurrency: 'USD', chosen: true })
    // New accounts start in the display currency.
    expect((await screen.findByLabelText('Valuta') as HTMLSelectElement).value).toBe('USD')
  })

  it('S15_ValutaNelleImpostazioni', async () => {
    const { api } = renderWithApi(<SettingsPage />)
    const select = await screen.findByLabelText('Valuta di visualizzazione')
    await waitFor(() => expect((select as HTMLSelectElement).value).toBe('EUR'))

    await userEvent.selectOptions(select, 'USD')

    await waitFor(() => expect(api.preferences.displayCurrency).toBe('USD'))
  })

  it('S16_TrasferimentoTraValute', async () => {
    const api = new FakeApi()
    api.givenAccount('Credem', 1000)
    api.givenAccount('Chase', 0, 'CurrentAccount', 'USD')
    renderWithApi(<AccountsCard accounts={api.accounts} max={2} />, api)
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: 'Trasferisci denaro' }))
    const dialog = screen.getByRole('dialog')
    await user.type(await within(dialog).findByLabelText('Importo in uscita'), '100')
    await user.type(within(dialog).getByLabelText('Importo in entrata'), '108')
    await user.click(within(dialog).getByRole('button', { name: 'Trasferisci' }))

    await waitFor(() =>
      expect(api.requests.find((r) => r.method === 'POST' && r.path === '/api/transactions')?.body).toMatchObject({
        kind: 'Transfer',
        amount: 100,
        counterAmount: 108,
      }),
    )
  })
})
