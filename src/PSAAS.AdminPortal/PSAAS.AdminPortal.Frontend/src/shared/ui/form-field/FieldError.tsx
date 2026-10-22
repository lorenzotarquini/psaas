import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { get, useFormContext } from 'react-hook-form'

// Messages are validation codes, the same the backend uses (Required, NotUnique…): translated here.
// The detail ("atteso IT + 11 cifre") comes from validationHint.<field>.<code> when it exists.
export function FieldError({ name, id, extra }: { name: string; id?: string; extra?: ReactNode }) {
  const { t } = useTranslation()
  const { formState } = useFormContext()
  const code = (get(formState.errors, name) as { message?: string } | undefined)?.message
  if (!code) return null

  const field = name.split('.').at(-1)
  const detail = t(`validationHint.${field}.${code}`, { defaultValue: '' })
  return (
    <p id={id} className="text-[12px] text-error">
      {t(`validation.${code}`, { defaultValue: code })}
      {detail && ` · ${detail}`}
      {extra && <> {extra}</>}
    </p>
  )
}
