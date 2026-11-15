import Keycloak from 'keycloak-js'
import { getLogger } from '@/shared/lib/logger'
import type { AuthSession } from './session'

const logger = getLogger('auth')

// Single instance created outside React: keycloak-js throws if init() runs twice (StrictMode).
const keycloak = new Keycloak({
  url: import.meta.env.VITE_KEYCLOAK_URL,
  realm: import.meta.env.VITE_KEYCLOAK_REALM,
  clientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID,
})

/** Keycloak login (PKCE): the tokens live in memory only and are refreshed when they expire within 30 seconds. */
export const keycloakSession: AuthSession = {
  async init() {
    // checkLoginIframe relies on third-party cookies, blocked by modern browsers.
    await keycloak.init({ onLoad: 'login-required', pkceMethod: 'S256', checkLoginIframe: false })
    logger.info({ user: keycloak.tokenParsed?.preferred_username }, 'User authenticated')
  },

  async headers() {
    try {
      await keycloak.updateToken(30)
    } catch (error) {
      logger.warn({ err: error }, 'Token refresh failed, redirecting to login')
      await keycloak.login()
    }
    return { Authorization: `Bearer ${keycloak.token!}` }
  },

  userName: () => keycloak.tokenParsed?.name ?? keycloak.tokenParsed?.preferred_username ?? '',

  login: () => keycloak.login(),

  logout() {
    logger.info('User logging out')
    return keycloak.logout({ redirectUri: window.location.origin })
  },
}
