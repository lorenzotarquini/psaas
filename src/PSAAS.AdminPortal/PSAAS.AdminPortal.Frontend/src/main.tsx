import { StrictMode, Suspense } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import './i18n'
import App from './App.tsx'
import { initAuth } from './auth'
import { getLogger } from './logger'

const logger = getLogger('main')
const root = createRoot(document.getElementById('root')!)

// Keycloak docs: init before rendering/routing, since login changes the URL.
initAuth()
  .then(() =>
    root.render(
      <StrictMode>
        <Suspense fallback={<span className="loading loading-spinner loading-lg m-8" />}>
          <App />
        </Suspense>
      </StrictMode>,
    ),
  )
  .catch((error: unknown) => {
    logger.fatal({ err: error }, 'Keycloak initialization failed')
    // Translations may not be loaded yet: plain bilingual fallback.
    root.render(<div className="alert alert-error m-8">Autenticazione non riuscita / Authentication failed.</div>)
  })
