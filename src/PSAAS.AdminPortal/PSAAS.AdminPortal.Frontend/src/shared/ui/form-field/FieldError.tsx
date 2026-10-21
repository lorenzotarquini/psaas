import { useTranslation } from 'react-i18next'
import { get, useFormContext } from 'react-hook-form'

// Messages are validation codes, the same the backend uses (Required, NotUnique…): translated here.
export function FieldError({ name, id }: { name: string; id?: string }) {
  const { t } = useTranslation()
  const { formState } = useFormContext()
  const code = (get(formState.errors, name) as { message?: string } | undefined)?.message
  if (!code) return null

  return (
    <p id={id} className="text-error text-sm">
      {t(`validation.${code}`, { defaultValue: code })}
    </p>
  )
}
