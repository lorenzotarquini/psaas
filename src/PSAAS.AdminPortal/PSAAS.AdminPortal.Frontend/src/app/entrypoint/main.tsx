import { StrictMode, Suspense } from 'react'
import { createRoot } from 'react-dom/client'
import '../styles/index.css'
import '@/shared/i18n'
import { HomePage } from '@/pages/home'
import { initAuth } from '@/shared/auth'
import { getLogger } from '@/shared/lib/logger'

const logger = getLogger('main')
const root = createRoot(document.getElementById('root')!)

// Keycloak docs: init before rendering/routing, since login changes the URL.
initAuth()
  .then(() =>
    root.render(
      <StrictMode>
        <Suspense fallback={<span className="loading loading-spinner loading-lg m-8" />}>
          <HomePage />
        </Suspense>
      </StrictMode>,
    ),
  )
  .catch((error: unknown) => {
    logger.fatal({ err: error }, 'Keycloak initialization failed')
    // Translations may not be loaded yet: plain bilingual fallback.
    root.render(<div className="alert alert-error m-8">Autenticazione non riuscita / Authentication failed.</div>)
  })
