import { useQuery } from '@tanstack/react-query'
import { apiGet } from '@/shared/api'

// Mirrors ProfileReadModel in PSAAS.Application. The backend orders profiles by name.
export interface Profile {
  id: string
  tenantId: string
  tenantCompanyName: string
  name: string
  description: string | null
}

export const useProfiles = () =>
  useQuery({ queryKey: ['profiles'], queryFn: ({ signal }) => apiGet<Profile[]>('/api/profiles', signal) })
