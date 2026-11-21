import type { ReactElement } from 'react'
import { render } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { createMemoryRouter, MemoryRouter, RouterProvider } from 'react-router'
import { vi } from 'vitest'
import { routes } from '@/app/routes/router'
import { FakeApi } from './fakeApi'

/** The real app (routes, pages, features) on a fake API, starting at the given URL. */
export function renderApp(url: string, api = new FakeApi()) {
  vi.stubGlobal('fetch', api.fetch)
  const router = createMemoryRouter(routes, { initialEntries: [url] })
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  )
  return { api, router }
}

/** One component with a query client, on a fake API, at the browser's current URL (for links and ?query state). */
export function renderWithApi(element: ReactElement, api = new FakeApi()) {
  vi.stubGlobal('fetch', api.fetch)
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter initialEntries={[`${window.location.pathname}${window.location.search}`]}>{element}</MemoryRouter>
    </QueryClientProvider>,
  )
  return { api }
}
