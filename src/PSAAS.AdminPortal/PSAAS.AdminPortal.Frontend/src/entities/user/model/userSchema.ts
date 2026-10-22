import { z } from 'zod'

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
