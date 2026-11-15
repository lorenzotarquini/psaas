import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { FakeApi } from '@/test/fakeApi'
import { renderWithApi } from '@/test/renderApp'
import { AccountsCard } from './AccountsCard'

vi.mock('@/shared/auth', () => ({
  session: { headers: () => Promise.resolve({}), userName: () => 'Mario', login: vi.fn(), logout: vi.fn(), init: vi.fn() },
}))

function givenTwoAccounts() {
  const api = new FakeApi()
  const credem = api.givenAccount('Credem', 7600)
  const revolut = api.givenAccount('Revolut', 23)
  renderWithApi(<AccountsCard accounts={api.accounts} max={2} />, api)
  return { api, credem, revolut }
}

// docs/mysync/use-cases/UC9-trasferire-denaro.md
describe('UC9_TrasferireDenaro', () => {
  it('S8_ModaleDelTrasferimento', async () => {
    const { api, credem, revolut } = givenTwoAccounts()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: 'Trasferisci denaro' }))
    const dialog = screen.getByRole('dialog')
    const out = within(dialog).getByLabelText('Importo in uscita') as HTMLInputElement
    const into = within(dialog).getByLabelText('Importo in entrata') as HTMLInputElement
    expect([out.placeholder, into.placeholder]).toEqual(['-0', '+0'])

    await user.type(out, '500')
    expect([out.value, into.value]).toEqual(['-500', '+500'])

    await user.click(within(dialog).getByRole('button', { name: 'Inverti direzione' }))
    expect([out.value, into.value]).toEqual(['+500', '-500'])

    await user.click(within(dialog).getByRole('button', { name: 'Trasferisci' }))
    await waitFor(() => expect(api.requests.some((r) => r.method === 'POST' && r.path === '/api/transactions')).toBe(true))
    // Swapped: the money leaves the bottom row (Revolut) and enters the top one (Credem).
    expect(api.requests.find((r) => r.path === '/api/transactions')?.body).toMatchObject({
      kind: 'Transfer',
      amount: 500,
      accountId: revolut.id,
      counterAccountId: credem.id,
    })
  })

  it('lo stesso conto nelle due righe viene rifiutato', async () => {
    const { api, credem } = givenTwoAccounts()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: 'Trasferisci denaro' }))
    const dialog = screen.getByRole('dialog')
    await user.selectOptions(within(dialog).getByLabelText('Secondo conto'), credem.id)
    await user.type(within(dialog).getByLabelText('Importo in uscita'), '10')
    await user.click(within(dialog).getByRole('button', { name: 'Trasferisci' }))

    expect(await within(dialog).findByText('Scegli due conti diversi')).toBeTruthy()
    expect(api.requests.some((r) => r.method === 'POST')).toBe(false)
  })

  it('con un solo conto il pulsante non c’è', () => {
    const api = new FakeApi()
    api.givenAccount('Credem')
    renderWithApi(<AccountsCard accounts={api.accounts} max={2} />, api)

    expect(screen.queryByRole('button', { name: 'Trasferisci denaro' })).toBeNull()
  })
})
