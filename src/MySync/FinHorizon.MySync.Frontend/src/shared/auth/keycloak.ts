import Keycloak from 'keycloak-js'
import { getLogger } from '@/shared/lib/logger'
import type { AuthSession } from './session'

const logger = getLogger('auth')

/** Realm role the Admin Portal gives with the MySync module (UC32); the backend requires it too. */
const mySyncRole = 'module-mysync'

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
    // The subject, a pseudonymous id: never the username or the email.
    logger.info({ userId: keycloak.tokenParsed?.sub }, 'User authenticated')
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

  hasAccess: () => keycloak.hasRealmRole(mySyncRole),

  userName: () => keycloak.tokenParsed?.name ?? keycloak.tokenParsed?.preferred_username ?? '',

  profile: () => ({
    firstName: keycloak.tokenParsed?.given_name ?? '',
    lastName: keycloak.tokenParsed?.family_name ?? '',
    email: keycloak.tokenParsed?.email ?? '',
    username: keycloak.tokenParsed?.preferred_username ?? '',
  }),

  // Application Initiated Actions: Keycloak shows its own page and comes back with a fresh token.
  updateProfile: () => keycloak.login({ action: 'UPDATE_PROFILE', redirectUri: window.location.href }),

  changePassword: () => keycloak.login({ action: 'UPDATE_PASSWORD', redirectUri: window.location.href }),

  login: () => keycloak.login(),

  logout() {
    logger.info('User logging out')
    return keycloak.logout({ redirectUri: window.location.origin })
  },
}
