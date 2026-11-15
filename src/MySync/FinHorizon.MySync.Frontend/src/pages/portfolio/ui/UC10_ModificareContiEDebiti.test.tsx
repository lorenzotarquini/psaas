import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { FakeApi } from '@/test/fakeApi'
import { renderWithApi } from '@/test/renderApp'
import { PortfolioPage } from './PortfolioPage'

vi.mock('@/shared/auth', () => ({
  session: { headers: () => Promise.resolve({}), userName: () => 'Mario', login: vi.fn(), logout: vi.fn(), init: vi.fn() },
}))

// docs/mysync/use-cases/UC10-modificare-conti-e-debiti.md
describe('UC10_ModificareContiEDebiti', () => {
  it('S6_AvvisoPrimaDiModificareUnConto', async () => {
    const api = new FakeApi()
    const credem = api.givenAccount('Credem', 6000)
    renderWithApi(<PortfolioPage />, api)
    const user = userEvent.setup()

    await user.click(await screen.findByRole('button', { name: 'Modifica il conto Credem' }))
    const dialog = screen.getByRole('dialog')
    const balance = within(dialog).getByLabelText('Saldo iniziale')
    await user.clear(balance)
    await user.type(balance, '7600')
    await user.click(within(dialog).getByRole('button', { name: 'Salva' }))

    const warning = within(dialog).getByRole('alertdialog')
    expect(warning.textContent).toContain('il patrimonio attuale e il suo andamento verranno ricalcolati')
    await user.click(within(warning).getByRole('button', { name: 'Annulla' }))
    expect(api.requests.some((r) => r.method === 'PUT')).toBe(false)

    await user.click(within(dialog).getByRole('button', { name: 'Salva' }))
    await user.click(within(screen.getByRole('alertdialog')).getByRole('button', { name: 'Procedi' }))

    await waitFor(() => expect(api.requests.find((r) => r.method === 'PUT')?.path).toBe(`/api/accounts/${credem.id}`))
    expect(api.requests.find((r) => r.method === 'PUT')?.body).toMatchObject({ name: 'Credem', initialBalance: 7600 })
  })

  it('S7_AggiuntaDisabilitataOltreIlLimite', async () => {
    const api = new FakeApi()
    api.limits = { maxAccounts: 2, maxDebts: 1 }
    api.givenAccount('Credem')
    api.givenAccount('Revolut')
    renderWithApi(<PortfolioPage />, api)

    const newAccount = await screen.findByRole('button', { name: 'Nuovo conto' })
    expect((newAccount as HTMLButtonElement).disabled).toBe(true)
    expect(screen.getByLabelText('Nuovo conto: Hai raggiunto il numero massimo di conti.')).toBeTruthy()
    expect((screen.getByRole('button', { name: 'Nuovo debito' }) as HTMLButtonElement).disabled).toBe(false)
  })

  it('un debito si modifica senza avviso', async () => {
    const api = new FakeApi()
    const credem = api.givenAccount('Credem')
    api.debts.push({
      id: 'd1',
      kind: 'Mortgage',
      name: 'Mutuo',
      description: null,
      tag: null,
      accountId: credem.id,
      initialResidual: 170,
      residual: 170,
      createdOn: '2026-11-01',
      paidOff: false,
    })
    renderWithApi(<PortfolioPage />, api)
    const user = userEvent.setup()

    await user.click(await screen.findByRole('button', { name: 'Modifica il debito Mutuo' }))
    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Salva' }))

    await waitFor(() => expect(api.requests.find((r) => r.method === 'PUT')?.path).toBe('/api/debts/d1'))
    expect(screen.queryByRole('alertdialog')).toBeNull()
  })
})
