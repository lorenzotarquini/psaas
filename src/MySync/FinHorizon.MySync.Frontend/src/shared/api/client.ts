import { session } from '@/shared/auth'
import { getLogger } from '@/shared/lib/logger'

const logger = getLogger('api')

/** Validation codes per field, e.g. { "name": ["NotUnique"], "limit": ["LimitReached"] }. */
export type FieldErrors = Record<string, string[]>

export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: FieldErrors

  constructor(status: number, path: string, fieldErrors: FieldErrors = {}) {
    super(`${path} returned HTTP ${status}`)
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

// 400 ValidationProblem from the backend: field codes live in "errors".
export async function readFieldErrors(response: Response): Promise<FieldErrors> {
  try {
    const problem = (await response.json()) as { errors?: FieldErrors }
    return problem.errors ?? {}
  } catch {
    return {}
  }
}

type Method = 'GET' | 'POST' | 'PUT' | 'DELETE'

// Relative /api: proxied to the backend by Vite in dev (vite.config.ts).
async function request<T>(method: Method, path: string, body?: unknown, signal?: AbortSignal): Promise<T> {
  const response = await fetch(path, {
    method,
    headers: {
      ...(await session.headers()),
      Accept: 'application/json',
      ...(body !== undefined && { 'Content-Type': 'application/json' }),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
    signal,
  })

  if (!response.ok) {
    const fieldErrors = response.status === 400 ? await readFieldErrors(response) : {}
    logger.error({ method, path, status: response.status, fields: Object.keys(fieldErrors) }, 'API call failed')
    // Token rejected despite the refresh: the session is gone, start a new login.
    if (response.status === 401) void session.login()
    throw new ApiError(response.status, path, fieldErrors)
  }
  return response.status === 204 ? (undefined as T) : ((await response.json()) as T)
}

export const apiGet = <T>(path: string, signal?: AbortSignal) => request<T>('GET', path, undefined, signal)
export const apiPost = <T>(path: string, body: unknown) => request<T>('POST', path, body)
export const apiPut = <T>(path: string, body: unknown) => request<T>('PUT', path, body)
export const apiDelete = (path: string) => request<void>('DELETE', path)

/** Query string from the defined values only. */
export function toQuery(params: Record<string, string | number | null | undefined>): string {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  }
  const text = search.toString()
  return text ? `?${text}` : ''
}
