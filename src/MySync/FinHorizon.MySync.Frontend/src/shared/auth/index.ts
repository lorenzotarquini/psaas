import { keycloakSession } from './keycloak'
import type { AuthSession } from './session'

export type { AuthSession } from './session'

/** The login of the app: Keycloak (PKCE). */
export const session: AuthSession = keycloakSession
