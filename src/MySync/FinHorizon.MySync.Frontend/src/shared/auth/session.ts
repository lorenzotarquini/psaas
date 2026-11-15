/** What the rest of the app needs from a login, whatever provides it. */
export interface AuthSession {
  /** Runs before rendering: the login may change the URL. */
  init(): Promise<void>
  /** Headers that authenticate one API call. */
  headers(): Promise<Record<string, string>>
  /** Display name of the signed-in user. */
  userName(): string
  login(): Promise<void>
  logout(): Promise<void>
}
