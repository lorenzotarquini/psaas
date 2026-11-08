import type { TenantDetail } from '@/entities/tenant'

// For each profile (row), whether it opens each productive unit (column), in the order received.
export function buildAccessMatrix({ profiles, productiveUnits }: Pick<TenantDetail, 'profiles' | 'productiveUnits'>) {
  return profiles.map((profile) => ({
    profile,
    opens: productiveUnits.map((unit) => unit.profileIds.includes(profile.id)),
  }))
}
