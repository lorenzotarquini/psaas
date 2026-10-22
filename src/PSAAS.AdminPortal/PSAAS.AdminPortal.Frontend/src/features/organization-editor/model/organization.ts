import { z } from 'zod'
import type { OrganizationRequest, TenantDetail } from '@/entities/tenant'

const required = z.string().trim().min(1, 'Required')

// id null = not saved yet. key links units to profiles inside the form (and the request).
// removed: a saved item marked for removal; it stays visible (struck through) until saved or restored.
export const organizationSchema = z.object({
  profiles: z.array(
    z.object({ id: z.string().nullable(), key: z.string(), name: required, description: z.string(), removed: z.boolean() }),
  ),
  productiveUnits: z.array(
    z.object({
      id: z.string().nullable(),
      name: required,
      description: z.string(),
      profileKeys: z.array(z.string()),
      removed: z.boolean(),
    }),
  ),
})

export type OrganizationValues = z.input<typeof organizationSchema>
export type ProfileValue = OrganizationValues['profiles'][number]
export type UnitValue = OrganizationValues['productiveUnits'][number]

export const emptyOrganization: OrganizationValues = { profiles: [], productiveUnits: [] }

export const newProfile = (): ProfileValue => ({ id: null, key: crypto.randomUUID(), name: '', description: '', removed: false })
export const newUnit = (): UnitValue => ({ id: null, name: '', description: '', profileKeys: [], removed: false })

// Existing profiles use their id as key: units already reference them by id.
export const organizationFromTenant = ({ profiles, productiveUnits }: Pick<TenantDetail, 'profiles' | 'productiveUnits'>): OrganizationValues => ({
  profiles: profiles.map((p) => ({ id: p.id, key: p.id, name: p.name, description: p.description ?? '', removed: false })),
  productiveUnits: productiveUnits.map((u) => ({
    id: u.id,
    name: u.name,
    description: u.description ?? '',
    profileKeys: u.profileIds,
    removed: false,
  })),
})

const nullIfBlank = (value: string) => value.trim() || null

// Removed items are left out: UC5 replaces the whole organization, so missing means removed.
export const toOrganizationRequest = ({ profiles, productiveUnits }: OrganizationValues): OrganizationRequest => {
  const kept = profiles.filter((p) => !p.removed)
  const keptKeys = new Set(kept.map((p) => p.key))
  return {
    profiles: kept.map((p) => ({ id: p.id, key: p.key, name: p.name.trim(), description: nullIfBlank(p.description) })),
    productiveUnits: productiveUnits
      .filter((u) => !u.removed)
      .map((u) => ({
        id: u.id,
        name: u.name.trim(),
        description: nullIfBlank(u.description),
        profileKeys: u.profileKeys.filter((key) => keptKeys.has(key)),
      })),
  }
}

/** UC5-R4: saved profiles marked for removal; their users lose them in Keycloak. */
export const removedSavedProfiles = (values: OrganizationValues) => values.profiles.filter((p) => p.id && p.removed)

/** Whether a profile opens a unit now and whether that differs from the saved organization (mock 1d highlight). */
export function accessCell(original: OrganizationValues, unit: UnitValue, profile: ProfileValue) {
  const open = unit.profileKeys.includes(profile.key)
  const saved = original.productiveUnits.find((u) => u.id && u.id === unit.id)
  const changed = Boolean(saved && profile.id) && saved!.profileKeys.includes(profile.key) !== open
  return { open, changed }
}

/**
 * Number of unsaved changes for the bar of mock 1d: each added, renamed or removed profile or unit,
 * and each access changed between saved ones (links of new items count with their addition).
 */
export function countChanges(original: OrganizationValues, current: OrganizationValues) {
  const edited = <T extends { id: string | null; name: string; description: string; removed: boolean }>(items: T[], saved: T[]) =>
    items.filter((item) => {
      if (!item.id) return !item.removed
      if (item.removed) return true
      const before = saved.find((s) => s.id === item.id)
      return !before || before.name.trim() !== item.name.trim() || before.description.trim() !== item.description.trim()
    }).length
  let links = 0
  for (const unit of current.productiveUnits.filter((u) => u.id && !u.removed)) {
    for (const profile of current.profiles.filter((p) => p.id && !p.removed)) {
      if (accessCell(original, unit, profile).changed) links++
    }
  }
  return edited(current.profiles, original.profiles) + edited(current.productiveUnits, original.productiveUnits) + links
}
