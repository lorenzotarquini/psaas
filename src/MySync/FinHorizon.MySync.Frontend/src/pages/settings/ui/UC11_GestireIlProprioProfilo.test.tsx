import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { session } from '@/shared/auth'
import { SettingsPage } from './SettingsPage'

vi.mock('@/shared/auth', () => ({
  session: {
    headers: () => Promise.resolve({}),
    userName: () => 'Mario Rossi',
    profile: () => ({ firstName: 'Mario', lastName: 'Rossi', email: 'mario.rossi@example.com', username: 'mario.rossi@example.com' }),
    updateProfile: vi.fn(() => Promise.resolve()),
    changePassword: vi.fn(() => Promise.resolve()),
    login: vi.fn(),
    logout: vi.fn(),
    init: vi.fn(),
  },
}))

// docs/mysync/use-cases/UC11-gestire-il-proprio-profilo.md
describe('UC11_GestireIlProprioProfilo', () => {
  it('S1_DatiDalToken', () => {
    render(<SettingsPage />)

    expect(screen.getByText('Mario')).toBeTruthy()
    expect(screen.getByText('Rossi')).toBeTruthy()
    expect(screen.getAllByText('mario.rossi@example.com')).toHaveLength(2)
  })

  it('S2_ModificaDatiSuKeycloak', async () => {
    render(<SettingsPage />)

    await userEvent.click(screen.getByRole('button', { name: 'Modifica i tuoi dati' }))

    expect(session.updateProfile).toHaveBeenCalledOnce()
  })

  it('S3_ReimpostaPasswordSuKeycloak', async () => {
    render(<SettingsPage />)

    await userEvent.click(screen.getByRole('button', { name: 'Reimposta la password' }))

    expect(session.changePassword).toHaveBeenCalledOnce()
  })
})
