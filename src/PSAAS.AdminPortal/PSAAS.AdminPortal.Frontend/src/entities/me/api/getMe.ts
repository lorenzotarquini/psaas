import { useQuery } from '@tanstack/react-query'
import { apiGet } from '@/shared/api'

// Mirrors MeDto in PSAAS.AdminPortal.Backend.
export interface Me {
  keycloakId: string | null
  name: string | null
  email: string | null
  roles: string[]
}

export const useMe = () =>
  useQuery({ queryKey: ['me'], queryFn: ({ signal }) => apiGet<Me>('/api/me', signal) })
