import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'
import { ApiError } from '@/shared/api'

/** Backend field name → form field name, e.g. { categoryId: 'category' }. */
export type ServerFieldMap<T extends FieldValues> = Readonly<Record<string, Path<T>>>

/**
 * Puts the backend field codes on the form fields (RNF5). Codes on fields the form does not show
 * (for example "limit") come back to be shown as a form-level message; "Unexpected" for any other failure.
 */
export function applyServerErrors<T extends FieldValues>(error: unknown, setError: UseFormSetError<T>, fields: ServerFieldMap<T>): string[] {
  if (!(error instanceof ApiError) || error.status !== 400) return ['Unexpected']
  const formLevel: string[] = []
  for (const [field, codes] of Object.entries(error.fieldErrors)) {
    const code = codes[0] ?? 'InvalidValue'
    const formField = fields[field]
    if (formField) setError(formField, { type: 'server', message: code })
    else formLevel.push(code)
  }
  return formLevel
}

/** The common case: the form uses the backend's field names. */
export const sameFields = <T extends FieldValues>(...names: Path<T>[]): ServerFieldMap<T> =>
  Object.fromEntries(names.map((name) => [name, name])) as ServerFieldMap<T>
