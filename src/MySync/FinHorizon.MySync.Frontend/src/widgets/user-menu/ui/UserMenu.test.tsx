import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { session } from '@/shared/auth'
import { setThemeMode } from '@/shared/theme'
import { initials, UserMenu } from './UserMenu'

vi.mock('@/shared/auth', () => ({
  session: {
    userName: () => 'Mario Rossi',
    profile: () => ({ firstName: 'Mario', lastName: 'Rossi', email: 'mario@example.com', username: 'mario' }),
    logout: vi.fn(() => Promise.resolve()),
  },
}))

function renderMenu() {
  const router = createMemoryRouter(
    [
      { path: '/', element: <UserMenu /> },
      { path: '/settings', element: <p>Pagina impostazioni</p> },
    ],
    { initialEntries: ['/'] },
  )
  render(<RouterProvider router={router} />)
  return router
}

describe('UserMenu', () => {
  it('shows the bigger avatar, the name, theme, settings and sign out', async () => {
    renderMenu()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: 'Apri il menu del profilo' }))

    const menu = screen.getByRole('menu', { name: 'Profilo' })
    expect(menu.textContent).toContain('Mario Rossi')
    expect(screen.getAllByText('MR')).toHaveLength(2)
    expect(screen.getByRole('menuitemcheckbox', { name: /Tema scuro/ })).toBeTruthy()
    expect(screen.getByRole('menuitem', { name: /Impostazioni/ })).toBeTruthy()
    expect(screen.getByRole('menuitem', { name: /Esci/ })).toBeTruthy()
  })

  it('switches the theme from the menu', async () => {
    setThemeMode('light')
    renderMenu()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: 'Apri il menu del profilo' }))
    await user.click(screen.getByRole('menuitemcheckbox', { name: /Tema scuro/ }))

    expect(document.documentElement.dataset.theme).toBe('mysync-dark')
    expect(screen.getByRole('menuitemcheckbox', { name: /Tema scuro/ }).getAttribute('aria-checked')).toBe('true')
  })

  it('opens the settings and signs out', async () => {
    const router = renderMenu()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: 'Apri il menu del profilo' }))
    await user.click(screen.getByRole('menuitem', { name: /Esci/ }))
    expect(session.logout).toHaveBeenCalledOnce()

    await user.click(screen.getByRole('menuitem', { name: /Impostazioni/ }))
    expect(router.state.location.pathname).toBe('/settings')
  })

  it('closes with Esc', async () => {
    renderMenu()
    const user = userEvent.setup()

    await user.click(screen.getByRole('button', { name: 'Apri il menu del profilo' }))
    await user.keyboard('{Escape}')

    expect(screen.queryByRole('menu')).toBeNull()
  })

  it.each([
    ['Mario', 'Rossi', 'x', 'MR'],
    ['anna', '', 'x', 'A'],
    ['', '', 'mario.rossi', 'M'],
    ['', '', '', '?'],
  ])('initials(%j, %j, %j) is %j', (first, last, fallback, expected) => expect(initials(first, last, fallback)).toBe(expected))
})
