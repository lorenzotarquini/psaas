// @vitest-environment jsdom
import { act, cleanup, render } from '@testing-library/react'
import { createMemoryRouter, Outlet, RouterProvider } from 'react-router'
import { afterEach, describe, expect, it } from 'vitest'
import { BreadcrumbProvider, useBreadcrumb, useCrumbs } from '.'

function Topbar() {
  return <p data-testid="crumbs">{useCrumbs().crumbs.map((crumb) => crumb.label).join(' > ')}</p>
}

function Tenant() {
  useBreadcrumb([{ label: 'Clienti', to: '/tenants' }, { label: 'Alfa Srl' }])
  return null
}

function setup() {
  const router = createMemoryRouter(
    [
      {
        path: '/',
        element: (
          <BreadcrumbProvider>
            <Topbar />
            <Outlet />
          </BreadcrumbProvider>
        ),
        children: [
          { path: 'tenants/1', element: <Tenant /> },
          { path: 'plain', element: null },
        ],
      },
    ],
    { initialEntries: ['/tenants/1'] },
  )
  const view = render(<RouterProvider router={router} />)
  return { router, crumbs: () => view.getByTestId('crumbs').textContent }
}

describe('breadcrumb', () => {
  afterEach(cleanup)

  it('shows what the current page declares', () => {
    expect(setup().crumbs()).toBe('Clienti > Alfa Srl')
  })

  it('shows nothing on a page that declares no crumbs, instead of the previous page ones', async () => {
    const { router, crumbs } = setup()
    await act(() => router.navigate('/plain'))
    expect(crumbs()).toBe('')
  })
})
