import type { ReactNode } from 'react'

interface FieldProps {
  label: string
  htmlFor: string
  error?: string
  hint?: string
  children: ReactNode
}

/** Label, control and the translated error of one form field. */
export function Field({ label, htmlFor, error, hint, children }: FieldProps) {
  return (
    <div className="fieldset py-1">
      <label htmlFor={htmlFor} className="fieldset-legend pb-1 pt-0 text-sm font-medium">
        {label}
      </label>
      {children}
      {error ? (
        <p id={`${htmlFor}-error`} role="alert" className="label text-error">
          {error}
        </p>
      ) : (
        hint && <p className="label">{hint}</p>
      )}
    </div>
  )
}
