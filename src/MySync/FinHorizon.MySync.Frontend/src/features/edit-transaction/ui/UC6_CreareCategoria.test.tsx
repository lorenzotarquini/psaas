import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { FakeApi } from '@/test/fakeApi'
import { renderWithApi } from '@/test/renderApp'
import { TransactionForm } from './TransactionForm'

vi.mock('@/shared/auth', () => ({
  session: { headers: () => Promise.resolve({}), userName: () => 'Mario', login: vi.fn(), logout: vi.fn(), init: vi.fn() },
}))

// docs/mysync/use-cases/UC6-creare-categoria.md
describe('UC6_CreareCategoria', () => {
  it('S5_MovimentoConNuovaCategoria', async () => {
    const api = new FakeApi()
    api.givenAccount('Conto Intesa')
    renderWithApi(<TransactionForm onDone={vi.fn()} />, api)
    const user = userEvent.setup()

    await user.type(screen.getByLabelText('Importo'), '40')
    await user.selectOptions(await screen.findByLabelText('Conto'), 'Conto Intesa')
    await user.type(screen.getByLabelText('Categoria'), 'Palestra')
    await user.click(await screen.findByRole('button', { name: 'Crea «Palestra»' }))

    await waitFor(() => expect(api.categories.map((c) => c.name)).toEqual(['Palestra']))
    expect((screen.getByLabelText('Categoria') as HTMLInputElement).value).toBe('Palestra')
    expect((screen.getByLabelText('Importo') as HTMLInputElement).value).toBe('40')
    expect((screen.getByLabelText('Conto') as HTMLSelectElement).selectedOptions[0]?.text).toBe('Conto Intesa')
    expect(api.requests.find((r) => r.path === '/api/categories' && r.method === 'POST')?.body).toEqual({ kind: 'Expense', name: 'Palestra' })
  })

  it('una categoria esistente si sceglie senza crearne un’altra', async () => {
    const api = new FakeApi()
    api.categories.push({ id: 'c1', kind: 'Expense', name: 'Palestra' })
    renderWithApi(<TransactionForm onDone={vi.fn()} />, api)
    const user = userEvent.setup()

    await user.type(screen.getByLabelText('Categoria'), 'pal')

    expect(await screen.findByRole('button', { name: 'Palestra' })).toBeTruthy()
    expect(screen.queryByRole('button', { name: /Crea/ })).toBeTruthy()
    await user.click(screen.getByRole('button', { name: 'Palestra' }))
    expect((screen.getByLabelText('Categoria') as HTMLInputElement).value).toBe('Palestra')
  })
})
