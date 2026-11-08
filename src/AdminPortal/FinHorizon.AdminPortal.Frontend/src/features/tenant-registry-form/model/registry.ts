import { z } from 'zod'
import type { TenantDetail, TenantKind, TenantRegistryRequest } from '@/entities/tenant'

// Messages are the backend validation codes, so client and server errors render the same way.
const required = z.string().trim().min(1, 'Required')

// Format only; the check letter is verified by the backend (UC3-R9).
const taxCodePattern = /^[A-Z]{6}[0-9LMNPQRSTUV]{2}[ABCDEHLMPRST][0-9LMNPQRSTUV]{2}[A-Z][0-9LMNPQRSTUV]{3}[A-Z]$/i

/** UC3-R2 / UC3-R9: a company name is shorter than a person's full name. */
export const nameMaxLength = (kind: TenantKind) => (kind === 'Individual' ? 50 : 20)

// UC3 R1–R4 and R9 (UC4 R2 reuses them). City/postal code existence is checked by the backend (R5).
export const registrySchema = z
  .object({
    kind: z.enum(['Business', 'Individual']),
    // Business: company name; Individual: full name.
    companyName: required,
    // Business: complete VAT number; Individual: tax code (codice fiscale).
    completeVatNumber: required,
    // Business: PEC; Individual: plain email.
    certifiedEmail: required.regex(/^[^\s@]+@[^\s@]+\.[^\s@]+$/, 'InvalidFormat'),
    street: required,
    city: required,
    postalCode: required,
    // UC14-R3: optional, 7 letters or digits (6 for public administrations); never for an individual.
    sdiCode: z.string().trim().regex(/^([A-Za-z0-9]{6,7})?$/, 'InvalidFormat'),
    accountManager: z.string(),
  })
  .superRefine((values, ctx) => {
    if (values.companyName.length > nameMaxLength(values.kind)) {
      ctx.addIssue({ code: 'custom', path: ['companyName'], message: 'MaxLengthExceeded' })
    }
    const identifier = values.kind === 'Individual' ? taxCodePattern : /^[A-Za-z]{2}\d{11}$/
    if (values.completeVatNumber.length > 0 && !identifier.test(values.completeVatNumber)) {
      ctx.addIssue({ code: 'custom', path: ['completeVatNumber'], message: 'InvalidFormat' })
    }
    if (values.kind === 'Individual' && values.sdiCode.length > 0) {
      ctx.addIssue({ code: 'custom', path: ['sdiCode'], message: 'InvalidValue' })
    }
  })

export type RegistryValues = z.input<typeof registrySchema>

export const emptyRegistry: RegistryValues = {
  kind: 'Business',
  companyName: '',
  completeVatNumber: '',
  certifiedEmail: '',
  street: '',
  city: '',
  postalCode: '',
  sdiCode: '',
  accountManager: '',
}

export const registryFromTenant = (tenant: TenantDetail): RegistryValues => ({
  kind: tenant.kind,
  companyName: tenant.companyName,
  completeVatNumber: tenant.completeVatNumber,
  certifiedEmail: tenant.certifiedEmail,
  street: tenant.address.street,
  city: tenant.address.city,
  postalCode: tenant.address.postalCode,
  sdiCode: tenant.sdiCode ?? '',
  accountManager: tenant.accountManager ?? '',
})

/** The kind is not part of an update (UC4): it is fixed at creation and sent only then. */
export const toRegistryRequest = ({ kind: _kind, sdiCode, accountManager, ...registry }: z.output<typeof registrySchema>): TenantRegistryRequest => ({
  ...registry,
  sdiCode: sdiCode.toUpperCase() || null,
  accountManager: accountManager.trim() || null,
})
