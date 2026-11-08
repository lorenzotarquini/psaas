import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'
import type { FieldErrors } from '@/shared/api'

// ASP.NET paths ("profiles[1].name", sometimes "$.companyName") → react-hook-form paths ("profiles.1.name").
export const toFormPath = (key: string) => key.replace(/^\$\./, '').replace(/\[(\d+)\]/g, '.$1')

/** Puts each backend validation code on its field; the code (e.g. "NotUnique") is the error message. */
export function applyServerErrors<T extends FieldValues>(errors: FieldErrors, setError: UseFormSetError<T>) {
  for (const [key, codes] of Object.entries(errors)) {
    setError(toFormPath(key) as Path<T>, { type: 'server', message: codes[0] })
  }
}
