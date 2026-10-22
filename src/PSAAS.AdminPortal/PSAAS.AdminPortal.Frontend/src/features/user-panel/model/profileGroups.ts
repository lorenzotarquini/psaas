import type { Profile } from '@/entities/profile'

export interface TenantGroup {
  tenantId: string
  tenantCompanyName: string
  profiles: { id: string; name: string; selected: boolean }[]
}

/**
 * Mock 1f "Profili raggruppati per cliente": every profile of the tenants the user is in (or that the administrator
 * added to the panel), ticked when the user has it. Tenants in the order they were added, profiles by name.
 */
export function groupProfilesByTenant(all: Profile[], selectedIds: string[], extraTenantIds: string[]): TenantGroup[] {
  const selected = new Set(selectedIds)
  const tenantIds = [...new Set([...all.filter((p) => selected.has(p.id)).map((p) => p.tenantId), ...extraTenantIds])]
  return tenantIds.flatMap((tenantId) => {
    const profiles = all.filter((p) => p.tenantId === tenantId).sort((a, b) => a.name.localeCompare(b.name))
    if (profiles.length === 0) return []
    return [
      {
        tenantId,
        tenantCompanyName: profiles[0].tenantCompanyName,
        profiles: profiles.map((p) => ({ id: p.id, name: p.name, selected: selected.has(p.id) })),
      },
    ]
  })
}

export const toggle = (ids: string[], id: string) => (ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id])
