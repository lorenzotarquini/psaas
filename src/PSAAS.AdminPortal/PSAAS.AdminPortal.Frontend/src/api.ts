import { getAccessToken } from './auth'
import { getLogger } from './logger'

const logger = getLogger('api')

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, path: string) {
    super(`${path} returned HTTP ${status}`)
    this.status = status
  }
}

// Relative /api: proxied to the backend by Vite in dev (vite.config.ts).
export async function apiGet<T>(path: string, signal?: AbortSignal): Promise<T> {
  const response = await fetch(path, {
    headers: { Authorization: `Bearer ${await getAccessToken()}`, Accept: 'application/json' },
    signal,
  })
  if (!response.ok) {
    logger.error({ path, status: response.status }, 'API call failed')
    throw new ApiError(response.status, path)
  }
  return (await response.json()) as T
}

export interface Me {
  keycloakId: string | null
  name: string | null
  email: string | null
  roles: string[]
}
