import type { FieldValues, UseFormSetError } from 'react-hook-form'
import { ApiError } from '@/shared/api'
import { applyServerErrors } from './serverErrors'

/**
 * Runs a save; on a 400 puts the backend codes on the fields. Other errors stay on the mutation
 * (shown with ErrorAlert). Returns the result, or undefined when the save failed.
 */
export async function submitWithServerErrors<T extends FieldValues, R>(
  save: () => Promise<R>,
  setError: UseFormSetError<T>,
): Promise<R | undefined> {
  try {
    return await save()
  } catch (error) {
    if (error instanceof ApiError && error.status === 400) applyServerErrors(error.fieldErrors, setError)
    return undefined
  }
}
