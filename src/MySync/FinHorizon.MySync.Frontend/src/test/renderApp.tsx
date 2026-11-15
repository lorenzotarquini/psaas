import type { ReactElement } from 'react'
import { render } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { createMemoryRouter, RouterProvider } from 'react-router'
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

/** One component with a query client, on a fake API. */
export function renderWithApi(element: ReactElement, api = new FakeApi()) {
  vi.stubGlobal('fetch', api.fetch)
  render(<QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>{element}</QueryClientProvider>)
  return { api }
}
