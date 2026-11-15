import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { AppLayout } from './AppLayout'

vi.mock('@/shared/auth', () => ({
  session: {
    userName: () => 'Mario Rossi',
    profile: () => ({ firstName: 'Mario', lastName: 'Rossi', email: 'mario@example.com', username: 'mario' }),
    logout: vi.fn(),
  },
}))

const renderLayout = () =>
  render(<RouterProvider router={createMemoryRouter([{ path: '/', Component: AppLayout, children: [{ index: true, element: <p>Pagina</p> }] }])} />)

describe('AppLayout sidebar', () => {
  it('starts collapsed and expands with the sidebar button at the left of the topbar, showing the name and a label per icon', async () => {
    renderLayout()
    const user = userEvent.setup()
    const nav = screen.getByRole('navigation', { name: 'Navigazione principale' })
    const topbar = screen.getByRole('banner')

    const toggle = within(topbar).getByRole('button', { name: 'Espandi il menu' })
    expect(toggle.getAttribute('aria-expanded')).toBe('false')
    expect(within(nav).queryByText('MySync')).toBeNull()
    expect(within(nav).getByText('Movimenti').className).toBe('sr-only')

    await user.click(toggle)

    expect(within(nav).getByText('MySync')).toBeTruthy()
    expect(within(nav).getByText('Movimenti').className).toContain('md:not-sr-only')
    expect(within(topbar).getByRole('button', { name: 'Riduci il menu' }).getAttribute('aria-expanded')).toBe('true')
    expect(localStorage.getItem('mysync.sidebarExpanded')).toBe('true')
  })

  it('remembers the choice on the device', () => {
    localStorage.setItem('mysync.sidebarExpanded', 'true')

    renderLayout()

    expect(within(screen.getByRole('navigation', { name: 'Navigazione principale' })).getByText('MySync')).toBeTruthy()
  })

  it('shows the version at the bottom and no environment badge', () => {
    renderLayout()

    expect(screen.getByText(/^v\d+\.\d+\.\d+$/)).toBeTruthy()
    expect(screen.queryByText('SVILUPPO')).toBeNull()
    // The topbar has the sidebar button where the app name used to be.
    expect(within(screen.getByRole('banner')).queryByText('MySync')).toBeNull()
  })
})
