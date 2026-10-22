import { faCircleCheck, faCircleExclamation } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useId, type InputHTMLAttributes, type ReactNode } from 'react'
import { get, useFormContext, useWatch, type RegisterOptions } from 'react-hook-form'
import { FieldError } from './FieldError'

type TextFieldProps = {
  name: string
  label: string
  /** Passed to react-hook-form's register, e.g. an onBlur check. */
  registerOptions?: RegisterOptions
  /** Checked and fine (e.g. a unique value still free): green check and the hint in green (mock 1a). */
  valid?: boolean
  /** Text under the field: with valid, a confirmation ("Disponibile"); with an error, extra help (a link). */
  hint?: ReactNode
  /** Shows "used/maxLength" next to the label. */
  counter?: boolean
} & InputHTMLAttributes<HTMLInputElement>

export function TextField({
  name,
  label,
  registerOptions,
  valid,
  hint,
  counter,
  className = '',
  maxLength,
  ...inputProps
}: TextFieldProps) {
  const id = useId()
  const { register, formState, control } = useFormContext()
  const invalid = Boolean(get(formState.errors, name))
  const value = (useWatch({ control, name }) as string | undefined) ?? ''

  return (
    <div className={`fieldset py-1.5 ${className}`}>
      <label htmlFor={id} className="fieldset-legend flex justify-between py-1 text-[12px] font-medium text-base-content/80">
        {label}
        {counter && maxLength && (
          <span className="data text-[11px] font-normal text-base-content/50">
            {value.length}/{maxLength}
          </span>
        )}
      </label>
      <label className={`input w-full ${invalid ? 'input-error' : ''}`}>
        <input
          id={id}
          maxLength={maxLength}
          aria-invalid={invalid}
          aria-describedby={invalid || hint ? `${id}-help` : undefined}
          {...inputProps}
          {...register(name, registerOptions)}
        />
        {invalid ? (
          <FontAwesomeIcon icon={faCircleExclamation} className="text-error" aria-hidden />
        ) : (
          valid && <FontAwesomeIcon icon={faCircleCheck} className="text-success" aria-hidden />
        )}
      </label>
      <div id={`${id}-help`}>
        <FieldError name={name} extra={invalid ? hint : undefined} />
        {!invalid && hint && <p className={`text-[12px] ${valid ? 'text-success' : 'text-base-content/60'}`}>{hint}</p>}
      </div>
    </div>
  )
}
