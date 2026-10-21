import { useId, type InputHTMLAttributes } from 'react'
import { get, useFormContext, type RegisterOptions } from 'react-hook-form'
import { FieldError } from './FieldError'

type TextFieldProps = {
  name: string
  label: string
  /** Passed to react-hook-form's register, e.g. an onBlur check. */
  registerOptions?: RegisterOptions
} & InputHTMLAttributes<HTMLInputElement>

export function TextField({ name, label, registerOptions, className = '', ...inputProps }: TextFieldProps) {
  const id = useId()
  const { register, formState } = useFormContext()
  const invalid = Boolean(get(formState.errors, name))

  return (
    <div className={`fieldset ${className}`}>
      <label htmlFor={id} className="fieldset-legend">
        {label}
      </label>
      <input
        id={id}
        className={`input w-full ${invalid ? 'input-error' : ''}`}
        aria-invalid={invalid}
        aria-describedby={invalid ? `${id}-error` : undefined}
        {...inputProps}
        {...register(name, registerOptions)}
      />
      <FieldError name={name} id={`${id}-error`} />
    </div>
  )
}
