import { StrictMode, Suspense } from 'react'
import { createRoot } from 'react-dom/client'
import 'flag-icons/css/flag-icons.min.css'
import '../styles/index.css'
import '@/shared/i18n'
import { QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router'
import { session } from '@/shared/auth'
import { getLogger } from '@/shared/lib/logger'
import { initTheme } from '@/shared/theme'
import { queryClient } from '../api/queryClient'
import { createAppRouter } from '../routes/router'

const logger = getLogger('main')
const root = createRoot(document.getElementById('root')!)

initTheme()

// Keycloak docs: init before rendering/routing, since login changes the URL.
session
  .init()
  .then(() =>
    root.render(
      <StrictMode>
        <Suspense fallback={<span className="loading loading-spinner loading-lg m-8" />}>
          <QueryClientProvider client={queryClient}>
            <RouterProvider router={createAppRouter()} />
          </QueryClientProvider>
        </Suspense>
      </StrictMode>,
    ),
  )
  .catch((error: unknown) => {
    logger.fatal({ err: error }, 'Authentication initialization failed')
    // Translations may not be loaded yet: plain bilingual fallback.
    root.render(<div className="alert alert-error m-8">Autenticazione non riuscita / Authentication failed.</div>)
  })
