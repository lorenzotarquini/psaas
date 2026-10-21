import Keycloak from 'keycloak-js'
import { getLogger } from '@/shared/lib/logger'

const logger = getLogger('auth')

// Single instance created outside React: keycloak-js throws if init() runs twice (StrictMode).
export const keycloak = new Keycloak({
  url: import.meta.env.VITE_KEYCLOAK_URL,
  realm: import.meta.env.VITE_KEYCLOAK_REALM,
  clientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID,
})

export async function initAuth(): Promise<void> {
  // checkLoginIframe relies on third-party cookies, blocked by modern browsers.
  await keycloak.init({ onLoad: 'login-required', pkceMethod: 'S256', checkLoginIframe: false })
  logger.info({ user: keycloak.tokenParsed?.preferred_username }, 'User authenticated')
}

// Tokens live in memory only; refreshed when they expire within 30 seconds.
export async function getAccessToken(): Promise<string> {
  try {
    await keycloak.updateToken(30)
  } catch (error) {
    logger.warn({ err: error }, 'Token refresh failed, redirecting to login')
    await keycloak.login()
  }
  return keycloak.token!
}

export function logout(): Promise<void> {
  logger.info('User logging out')
  return keycloak.logout({ redirectUri: window.location.origin })
}
