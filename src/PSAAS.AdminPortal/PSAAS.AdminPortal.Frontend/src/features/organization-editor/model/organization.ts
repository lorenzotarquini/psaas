import { z } from 'zod'
import type { OrganizationRequest, TenantDetail } from '@/entities/tenant'

const required = z.string().trim().min(1, 'Required')

// id null = not saved yet. key links units to profiles inside the form (and the request).
export const organizationSchema = z.object({
  profiles: z.array(z.object({ id: z.string().nullable(), key: z.string(), name: required, description: z.string() })),
  productiveUnits: z.array(
    z.object({ id: z.string().nullable(), name: required, description: z.string(), profileKeys: z.array(z.string()) }),
  ),
})

export type OrganizationValues = z.input<typeof organizationSchema>
export type ProfileValue = OrganizationValues['profiles'][number]
export type UnitValue = OrganizationValues['productiveUnits'][number]

export const emptyOrganization: OrganizationValues = { profiles: [], productiveUnits: [] }

export const newProfile = (): ProfileValue => ({ id: null, key: crypto.randomUUID(), name: '', description: '' })
export const newUnit = (): UnitValue => ({ id: null, name: '', description: '', profileKeys: [] })

// Existing profiles use their id as key: units already reference them by id.
export const organizationFromTenant = ({ profiles, productiveUnits }: TenantDetail): OrganizationValues => ({
  profiles: profiles.map((p) => ({ id: p.id, key: p.id, name: p.name, description: p.description ?? '' })),
  productiveUnits: productiveUnits.map((u) => ({
    id: u.id,
    name: u.name,
    description: u.description ?? '',
    profileKeys: u.profileIds,
  })),
})

const nullIfBlank = (value: string) => value.trim() || null

export const toOrganizationRequest = ({ profiles, productiveUnits }: OrganizationValues): OrganizationRequest => ({
  profiles: profiles.map((p) => ({ id: p.id, key: p.key, name: p.name.trim(), description: nullIfBlank(p.description) })),
  productiveUnits: productiveUnits.map((u) => ({
    id: u.id,
    name: u.name.trim(),
    description: nullIfBlank(u.description),
    profileKeys: u.profileKeys,
  })),
})

/** UC5 R4: removing a saved profile takes it away from its users; the page asks for confirmation. */
export const removedSavedProfiles = (original: TenantDetail, values: OrganizationValues) =>
  original.profiles.filter((saved) => !values.profiles.some((p) => p.id === saved.id)).map((p) => p.name)
