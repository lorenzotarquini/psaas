import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { renderApp } from '@/test/renderApp'
import { FakeApi } from '@/test/fakeApi'

vi.mock('@/shared/auth', () => ({
  session: { headers: () => Promise.resolve({}), userName: () => 'Mario', profile: () => ({ firstName: 'Mario', lastName: 'Rossi', email: 'mario@example.com', username: 'mario' }), login: vi.fn(), logout: vi.fn(), init: vi.fn() },
}))

// docs/mysync/use-cases/UC1-configurare-mysync.md
describe('UC1_ConfigurareMySync', () => {
  it('S1_PrimoAccessoMostraLaConfigurazione', async () => {
    renderApp('/')

    expect(await screen.findByRole('heading', { name: 'Configura MySync' })).toBeTruthy()
    expect(screen.getByRole('heading', { name: 'I tuoi conti' })).toBeTruthy()
    expect(screen.queryByRole('heading', { name: 'Dashboard' })).toBeNull()
  })

  it('S2_SenzaContiNonSiProsegue', async () => {
    renderApp('/setup')

    await userEvent.click(await screen.findByRole('button', { name: 'Avanti' }))

    expect(screen.getByRole('alert').textContent).toBe('Crea almeno un conto per continuare.')
    expect(screen.getByRole('heading', { name: 'I tuoi conti' })).toBeTruthy()
  })

  it('S3_ConfigurazioneCompleta', async () => {
    const { api, router } = renderApp('/setup')
    const user = userEvent.setup()

    await user.type(await screen.findByLabelText('Nome'), 'Conto Intesa')
    await user.type(screen.getByLabelText('Saldo iniziale'), '2500')
    await user.click(screen.getByRole('button', { name: 'Crea conto' }))
    await screen.findByText('Conto Intesa')
    await user.click(screen.getByRole('button', { name: 'Avanti' }))

    await user.type(await screen.findByLabelText('Nome'), 'Mutuo casa')
    await user.selectOptions(screen.getByLabelText('Conto collegato'), 'Conto Intesa')
    await user.type(screen.getByLabelText('Residuo da pagare'), '120000')
    await user.click(screen.getByRole('button', { name: 'Crea debito' }))
    await screen.findByText('Mutuo casa')
    await user.click(screen.getByRole('button', { name: 'Concludi' }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/'))
    expect(await screen.findByRole('heading', { name: 'Dashboard' })).toBeTruthy()
    expect(api.requests.find((r) => r.path === '/api/debts' && r.method === 'POST')?.body).toMatchObject({ residual: 120000, kind: 'Loan' })
  })

  it('S4_ConUnContoSiApreLaDashboard', async () => {
    const api = new FakeApi()
    api.givenAccount('Conto Intesa')

    renderApp('/', api)

    expect(await screen.findByRole('heading', { name: 'Dashboard' })).toBeTruthy()
  })
})
