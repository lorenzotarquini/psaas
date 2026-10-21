import { z } from 'zod'
import type { TenantDetail, TenantRegistryRequest } from '@/entities/tenant'

// Messages are the backend validation codes, so client and server errors render the same way.
const required = z.string().trim().min(1, 'Required')

// UC3 R1–R4 (UC4 R2 reuses them). City/postal code existence is checked by the backend (R5).
export const registrySchema = z.object({
  companyName: required.max(20, 'MaxLengthExceeded'),
  completeVatNumber: required.regex(/^[A-Za-z]{2}\d{11}$/, 'InvalidFormat'),
  certifiedEmail: required.regex(/^[^\s@]+@[^\s@]+\.[^\s@]+$/, 'InvalidFormat'),
  street: required,
  city: required,
  postalCode: required,
})

export type RegistryValues = z.input<typeof registrySchema>

export const emptyRegistry: RegistryValues = {
  companyName: '',
  completeVatNumber: '',
  certifiedEmail: '',
  street: '',
  city: '',
  postalCode: '',
}

export const registryFromTenant = (tenant: TenantDetail): RegistryValues => ({
  companyName: tenant.companyName,
  completeVatNumber: tenant.completeVatNumber,
  certifiedEmail: tenant.certifiedEmail,
  street: tenant.address.street,
  city: tenant.address.city,
  postalCode: tenant.address.postalCode,
})

export const toRegistryRequest = (values: z.output<typeof registrySchema>): TenantRegistryRequest => values
