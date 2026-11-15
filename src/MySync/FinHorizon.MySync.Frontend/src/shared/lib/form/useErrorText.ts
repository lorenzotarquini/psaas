import { useTranslation } from 'react-i18next'

/** Error codes (from zod or from the backend) translated: "Required" → "Campo obbligatorio". */
export function useErrorText() {
  const { t } = useTranslation()
  return (code: string | undefined) => (code ? t(`errors.codes.${code}`, { defaultValue: t('errors.codes.InvalidValue') }) : undefined)
}
