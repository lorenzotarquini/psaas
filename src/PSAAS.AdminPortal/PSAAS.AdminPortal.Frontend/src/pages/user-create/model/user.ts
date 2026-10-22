import { z } from 'zod'
import type { Profile } from '@/entities/profile'

const required = z.string().trim().min(1, 'Required')

// UC6 R1–R4. Email uniqueness and profile existence are checked by the backend.
export const userSchema = z.object({
  firstName: required,
  lastName: required,
  email: required.regex(/^[^\s@]+@[^\s@]+\.[^\s@]+$/, 'InvalidFormat'),
  birthDate: required.regex(/^\d{4}-\d{2}-\d{2}$/, 'InvalidFormat'),
  profileIds: z.array(z.string()),
})

export type UserValues = z.input<typeof userSchema>

export const emptyUser: UserValues = { firstName: '', lastName: '', email: '', birthDate: '', profileIds: [] }

export interface TenantProfiles {
  tenantId: string
  tenantName: string
  profiles: Profile[]
}

// R4: profiles may belong to different tenants; group them so the choice reads "tenant → profiles".
export function groupProfilesByTenant(profiles: Profile[]): TenantProfiles[] {
  const groups = new Map<string, TenantProfiles>()
  for (const profile of profiles) {
    const group = groups.get(profile.tenantId) ?? {
      tenantId: profile.tenantId,
      tenantName: profile.tenantCompanyName,
      profiles: [],
    }
    group.profiles.push(profile)
    groups.set(profile.tenantId, group)
  }
  return [...groups.values()].sort((a, b) => a.tenantName.localeCompare(b.tenantName))
}
