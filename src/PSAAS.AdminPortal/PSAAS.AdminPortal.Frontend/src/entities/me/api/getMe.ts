import { apiGet } from '@/shared/api'

// Mirrors MeDto in PSAAS.AdminPortal.Backend.
export interface Me {
  keycloakId: string | null
  name: string | null
  email: string | null
  roles: string[]
}

export const getMe = (signal?: AbortSignal) => apiGet<Me>('/api/me', signal)
