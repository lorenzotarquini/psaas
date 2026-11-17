import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { FakeApi } from '@/test/fakeApi'
import { renderApp } from '@/test/renderApp'

vi.mock('@/shared/auth', () => ({
  session: { headers: () => Promise.resolve({}), userName: () => 'Mario', profile: () => ({ firstName: 'Mario', lastName: 'Rossi', email: 'mario@example.com', username: 'mario' }), login: vi.fn(), logout: vi.fn(), init: vi.fn() },
}))

vi.stubGlobal(
  'ResizeObserver',
  class {
    observe() {}
    unobserve() {}
    disconnect() {}
  },
)

/** UC16-S4: "Netflix" with the charge of 2026-11-05 to register. */
function givenNetflixDue() {
  const api = new FakeApi()
  const intesa = api.givenAccount('Conto Intesa', 2000)
  api.categories.push({ id: 'cat-abbonamenti', kind: 'Expense', name: 'Abbonamenti', fixed: false })
  const netflix = api.givenSubscription(intesa, 'Netflix', 17.99, { categoryId: 'cat-abbonamenti', dueOn: '2026-11-05', hasPayments: true })
  return { api, intesa, netflix }
}

const postsTo = (api: FakeApi, path: string) => api.requests.filter((request) => request.method === 'POST' && request.path === path)

// docs/mysync/use-cases/UC16-gestire-abbonamenti.md
describe('UC16_GestireAbbonamenti', () => {
  it('S15_RegistraDallaDashboard', async () => {
    const { api, intesa, netflix } = givenNetflixDue()
    renderApp('/', api)
    const user = userEvent.setup()

    await user.click(await screen.findByRole('button', { name: 'Registra il pagamento di Netflix' }))

    const dialog = await screen.findByRole('dialog', { name: 'Spesa' })
    expect((within(dialog).getByLabelText('Data') as HTMLInputElement).value).toBe('2026-11-05')
    expect((within(dialog).getByLabelText('Importo') as HTMLInputElement).value).toBe('17.99')
    expect((within(dialog).getByLabelText('Conto') as HTMLSelectElement).value).toBe(intesa.id)
    expect((within(dialog).getByLabelText('Categoria') as HTMLInputElement).value).toBe('Abbonamenti')
    await waitFor(() => expect((within(dialog).getByLabelText('Abbonamento') as HTMLSelectElement).value).toBe(netflix.id))
    expect(postsTo(api, '/api/transactions')).toHaveLength(0)

    await user.click(within(dialog).getByRole('button', { name: 'Registra' }))

    await waitFor(() => expect(postsTo(api, '/api/transactions')).toHaveLength(1))
    expect(postsTo(api, '/api/transactions')[0].body).toMatchObject({
      kind: 'Expense',
      date: '2026-11-05',
      amount: 17.99,
      accountId: intesa.id,
      categoryId: 'cat-abbonamenti',
      subscriptionId: netflix.id,
      debtId: null,
    })
    await waitFor(() => expect(screen.queryByRole('button', { name: 'Registra il pagamento di Netflix' })).toBeNull())
  })

  it('S8_SaltareUnAddebito', async () => {
    const { api, netflix } = givenNetflixDue()
    renderApp('/subscriptions', api)
    const user = userEvent.setup()

    await user.click(await screen.findByRole('button', { name: "Salta l'addebito di Netflix" }))

    await waitFor(() => expect(postsTo(api, `/api/subscriptions/${netflix.id}/skip`)).toHaveLength(1))
    expect(postsTo(api, `/api/subscriptions/${netflix.id}/skip`)[0].body).toEqual({ dueOn: '2026-11-05' })
    expect(postsTo(api, '/api/transactions')).toHaveLength(0)
  })

  it('S1_NuovoAbbonamento', async () => {
    const api = new FakeApi()
    const intesa = api.givenAccount('Conto Intesa', 2000)
    api.categories.push({ id: 'cat-abbonamenti', kind: 'Expense', name: 'Abbonamenti', fixed: false })
    renderApp('/subscriptions', api)
    const user = userEvent.setup()

    await user.click(await screen.findByRole('button', { name: 'Nuovo abbonamento' }))
    const dialog = await screen.findByRole('dialog', { name: 'Nuovo abbonamento' })
    await user.type(within(dialog).getByLabelText('Nome'), 'Netflix')
    await user.type(within(dialog).getByLabelText('Importo mensile'), '17,99')
    const firstCharge = within(dialog).getByLabelText('Primo addebito')
    await user.clear(firstCharge)
    await user.type(firstCharge, '2026-12-05')
    await user.selectOptions(within(dialog).getByLabelText('Conto'), intesa.id)
    await user.type(within(dialog).getByLabelText('Categoria'), 'Abbo')
    await user.click(await within(dialog).findByRole('button', { name: 'Abbonamenti' }))
    await user.click(within(dialog).getByRole('button', { name: 'Crea abbonamento' }))

    await waitFor(() => expect(postsTo(api, '/api/subscriptions')).toHaveLength(1))
    expect(postsTo(api, '/api/subscriptions')[0].body).toEqual({
      name: 'Netflix',
      accountId: intesa.id,
      categoryId: 'cat-abbonamenti',
      amount: 17.99,
      firstChargeOn: '2026-12-05',
    })
    expect(await screen.findByText('Netflix')).toBeTruthy()
    expect(postsTo(api, '/api/transactions')).toHaveLength(0)
  })

  it('A_Annullare', async () => {
    const { api, netflix } = givenNetflixDue()
    renderApp('/subscriptions', api)
    const user = userEvent.setup()

    await user.click(await screen.findByRole('button', { name: 'Annulla Netflix' }))
    const dialog = await screen.findByRole('dialog', { name: 'Annulla abbonamento' })
    const endsOn = within(dialog).getByLabelText('Data di fine')
    await user.clear(endsOn)
    await user.type(endsOn, '2026-12-10')
    await user.click(within(dialog).getByRole('button', { name: 'Annulla' }))

    await waitFor(() => expect(postsTo(api, `/api/subscriptions/${netflix.id}/cancel`)).toHaveLength(1))
    expect(postsTo(api, `/api/subscriptions/${netflix.id}/cancel`)[0].body).toEqual({ endsOn: '2026-12-10' })
    expect(await screen.findByRole('button', { name: 'Riattiva Netflix' })).toBeTruthy()
  })

  it('E_ConPagamentiSiAnnullaNonSiElimina', async () => {
    const { api } = givenNetflixDue()
    renderApp('/subscriptions', api)
    const user = userEvent.setup()

    await user.click(await screen.findByRole('button', { name: 'Elimina Netflix' }))

    expect((await screen.findByRole('alert')).textContent).toMatch(/annullalo invece di eliminarlo/)
    expect(api.requests.some((request) => request.method === 'DELETE')).toBe(false)
  })

  it('4d_LimiteRaggiunto', async () => {
    const { api } = givenNetflixDue()
    api.maxSubscriptions = 1
    renderApp('/subscriptions', api)

    const button = await screen.findByRole('button', { name: 'Nuovo abbonamento' })

    expect((button as HTMLButtonElement).disabled).toBe(true)
    expect(screen.getByLabelText('Nuovo abbonamento: Hai raggiunto il numero massimo di abbonamenti attivi.')).toBeTruthy()
  })
})
