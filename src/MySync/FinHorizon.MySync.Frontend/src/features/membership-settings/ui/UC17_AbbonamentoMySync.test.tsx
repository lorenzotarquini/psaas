import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { goTo, type Membership } from '@/entities/membership'
import { FakeApi } from '@/test/fakeApi'
import { renderApp, renderWithApi } from '@/test/renderApp'
import { MembershipSettings } from './MembershipSettings'

vi.mock('@/shared/auth', () => ({
  session: {
    headers: () => Promise.resolve({}),
    userName: () => 'Mario Rossi',
    profile: () => ({ firstName: 'Mario', lastName: 'Rossi', email: 'mario@example.com', username: 'mario' }),
    login: vi.fn(),
    logout: vi.fn(),
    init: vi.fn(),
  },
}))

const expired = (overrides: Partial<Membership> = {}): Partial<Membership> => ({
  status: 'Expired',
  canWrite: false,
  currentPeriodEnd: null,
  card: null,
  hasCustomer: false,
  ...overrides,
})

const apiWith = (membership: Partial<Membership>) => {
  const api = new FakeApi()
  api.membership = { ...api.membership, ...membership }
  return api
}

afterEach(() => {
  vi.restoreAllMocks()
  window.history.replaceState(null, '', '/')
})

// docs/mysync/use-cases/UC17-abbonamento-mysync.md
describe('UC17_AbbonamentoMySync', () => {
  it('S1_ScadutoInSolaLettura', async () => {
    renderApp('/', apiWith(expired()))

    expect(await screen.findByText('Abbonamento scaduto: MySync è in sola lettura.')).toBeTruthy()
    expect(screen.getByRole('link', { name: 'Rinnova' }).getAttribute('href')).toBe('/settings?tab=billing')
  })

  it('S2_RinnovaApreStripeEAlRitornoEAttivo', async () => {
    const assign = vi.spyOn(goTo, 'url').mockImplementation(() => {})
    const api = apiWith(expired())
    renderWithApi(<MembershipSettings />, api)

    expect(await screen.findByText('MySync mensile · 4,99 € al mese')).toBeTruthy()
    expect(await screen.findByText(/IVA 22% inclusa · 14 giorni di prova gratuita/)).toBeTruthy()
    await userEvent.click(screen.getByRole('radio', { name: /MySync annuale/ }))
    await userEvent.click(screen.getByRole('button', { name: /Abbonati/ }))

    await waitFor(() => expect(assign).toHaveBeenCalledWith('https://checkout.stripe.test/session'))
    expect(api.requests.find((request) => request.path === '/api/membership/checkout')?.body).toEqual({ planCode: 'mysync-annuale' })
  })

  it('S2_RitornoDaStripeRileggeLoStato', async () => {
    window.history.replaceState(null, '', '/settings?tab=billing&membership=paid')
    const api = apiWith(expired())
    api.membershipAfterRefresh = { ...new FakeApi().membership }
    renderWithApi(<MembershipSettings />, api)

    expect(await screen.findByText('Attivo')).toBeTruthy()
    expect(screen.getByText('MySync mensile · 4,99 € al mese')).toBeTruthy()
    expect(screen.getByText('VISA •••• 4242, scade 12/2030')).toBeTruthy()
    expect(window.location.search).toBe('?tab=billing')
  })

  it('S3_PagamentoNonRiuscitoMostraLErrore', async () => {
    const api = apiWith({
      status: 'PastDue',
      lastFailure: { code: 'insufficient_funds', message: 'Your card has insufficient funds.', failedAt: '2026-11-15T09:00:00+00:00' },
      payUrl: 'https://invoice.stripe.test/in_1',
    })
    renderWithApi(<MembershipSettings />, api)

    expect(await screen.findByText(/Il pagamento del 15 nov 2026 non è andato a buon fine: fondi insufficienti sulla carta\./)).toBeTruthy()
    expect(screen.getByRole('link', { name: 'Paga ora' }).getAttribute('href')).toBe('https://invoice.stripe.test/in_1')
    expect(screen.getByRole('button', { name: /Aggiorna metodo di pagamento/ })).toBeTruthy()
  })

  it('S4_TentativiEsauritiErroreERinnova', async () => {
    renderWithApi(
      <MembershipSettings />,
      apiWith(expired({ hasCustomer: true, lastFailure: { code: 'card_declined', message: null, failedAt: '2026-11-15T09:00:00+00:00' } })),
    )

    expect(await screen.findByText(/la carta è stata rifiutata\./)).toBeTruthy()
    expect(screen.getByRole('button', { name: /Rinnova/ })).toBeTruthy()
  })

  it('S5_AttivoSiGestisceSulPortale', async () => {
    const assign = vi.spyOn(goTo, 'url').mockImplementation(() => {})
    renderWithApi(<MembershipSettings />, new FakeApi())

    await userEvent.click(await screen.findByRole('button', { name: /Gestisci abbonamento/ }))

    await waitFor(() => expect(assign).toHaveBeenCalledWith('https://billing.stripe.test/session'))
    expect(screen.queryByRole('button', { name: /Rinnova/ })).toBeNull()
  })

  it('S12_IlLivelloDiceCosaIncludeLaProiezioneFuoriDalPianoSiBlocca', async () => {
    const api = new FakeApi()
    renderWithApi(<MembershipSettings />, api)
    expect(await screen.findByText('Plus')).toBeTruthy()
    expect(screen.getByText('proiezione del patrimonio · 10 conti · 10 debiti · 20 abbonamenti')).toBeTruthy()
  })

  it('S12_ProiezioneNonInclusaNelLivello', async () => {
    const api = new FakeApi()
    api.membership = { ...api.membership, tier: { level: 1, name: 'Base', features: [], limits: { accounts: 2, debts: 1, subscriptions: 5 } } }
    renderApp('/projection', api)

    expect(await screen.findByText('Non incluso nel tuo piano')).toBeTruthy()
    expect(screen.getByRole('link', { name: 'Vedi i piani' }).getAttribute('href')).toBe('/settings?tab=billing')
  })

  it('S15_PassaAUnTierSuperiore', async () => {
    const api = new FakeApi()
    renderWithApi(<MembershipSettings />, api)

    expect(await screen.findByText('Passa a un piano superiore')).toBeTruthy()
    expect(screen.queryByRole('radio', { name: /MySync mensile/ })).toBeNull()
    await userEvent.click(screen.getByRole('button', { name: /Passa a questo piano/ }))

    await waitFor(() => expect(api.requests.find((request) => request.path === '/api/membership/change')?.body).toEqual({ planCode: 'mysync-annuale' }))
    expect(await screen.findByText("MySync annuale · 49,00 € all'anno")).toBeTruthy()
    await waitFor(() => expect(screen.queryByText('Passa a un piano superiore')).toBeNull())
  })

  it('S17_DifferenzaRifiutataIlPianoResta', async () => {
    const api = new FakeApi()
    api.planChangeDeclined = 'insufficient_funds'
    renderWithApi(<MembershipSettings />, api)

    await userEvent.click(await screen.findByRole('button', { name: /Passa a questo piano/ }))

    expect(await screen.findByText(/Il pagamento della differenza non è riuscito/)).toBeTruthy()
    expect(screen.getByText('MySync mensile · 4,99 € al mese')).toBeTruthy()
  })
})
