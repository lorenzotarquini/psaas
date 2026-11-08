import type { Profile } from '@/entities/profile'

export { emptyUser, userSchema, type UserValues } from '@/entities/user'

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
