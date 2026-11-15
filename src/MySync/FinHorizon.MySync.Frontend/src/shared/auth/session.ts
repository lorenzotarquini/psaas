/** The signed-in user's own data, as the identity provider knows it. */
export interface UserProfile {
  firstName: string
  lastName: string
  email: string
  username: string
}

/** What the rest of the app needs from a login, whatever provides it. */
export interface AuthSession {
  /** Runs before rendering: the login may change the URL. */
  init(): Promise<void>
  /** Headers that authenticate one API call. */
  headers(): Promise<Record<string, string>>
  /** Display name of the signed-in user. */
  userName(): string
  profile(): UserProfile
  /** UC11: the user edits their own data on the identity provider's page, then comes back here. */
  updateProfile(): Promise<void>
  /** UC11: the user sets a new password on the identity provider's page; it never passes through MySync. */
  changePassword(): Promise<void>
  login(): Promise<void>
  logout(): Promise<void>
}
