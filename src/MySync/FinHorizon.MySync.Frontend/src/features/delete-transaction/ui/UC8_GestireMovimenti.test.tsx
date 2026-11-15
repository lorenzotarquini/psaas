import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { FakeApi } from '@/test/fakeApi'
import { renderWithApi } from '@/test/renderApp'
import { DeleteTransactionButton } from './DeleteTransactionButton'

vi.mock('@/shared/auth', () => ({
  session: { headers: () => Promise.resolve({}), userName: () => 'Mario', profile: () => ({ firstName: 'Mario', lastName: 'Rossi', email: 'mario@example.com', username: 'mario' }), login: vi.fn(), logout: vi.fn(), init: vi.fn() },
}))

// docs/mysync/use-cases/UC8-gestire-movimenti.md
describe('UC8_GestireMovimenti', () => {
  it('S8_ConfermaEliminazione', async () => {
    const api = new FakeApi()
    const transaction = api.givenTransaction(api.givenAccount('Conto Intesa'), 85.4)
    renderWithApi(<DeleteTransactionButton transaction={transaction} />, api)
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: 'Elimina' }))
    const dialog = screen.getByRole('dialog')
    expect(within(dialog).getByText('5 nov 2026')).toBeTruthy()
    expect(within(dialog).getByText(/-85,40\s€/)).toBeTruthy()
    expect(within(dialog).getByText('Conto Intesa')).toBeTruthy()
    expect(within(dialog).getByText("L'eliminazione è definitiva e non si può annullare.")).toBeTruthy()

    await user.click(within(dialog).getByRole('button', { name: 'Annulla' }))

    expect(api.transactions).toHaveLength(1)
    expect(api.requests.some((r) => r.method === 'DELETE')).toBe(false)
  })

  it('confermando il movimento viene eliminato', async () => {
    const api = new FakeApi()
    const transaction = api.givenTransaction(api.givenAccount('Conto Intesa'), 10)
    renderWithApi(<DeleteTransactionButton transaction={transaction} />, api)
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: 'Elimina' }))
    await user.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Elimina' }))

    await waitFor(() => expect(api.transactions).toHaveLength(0))
  })
})
