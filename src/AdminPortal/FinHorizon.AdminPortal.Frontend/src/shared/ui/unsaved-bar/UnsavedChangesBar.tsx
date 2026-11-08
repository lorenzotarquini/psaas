import { faTriangleExclamation } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useEffect, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Kbd } from '@/shared/ui/kbd'

interface UnsavedChangesBarProps {
  count: number
  /** Consequences of saving, e.g. users losing a profile (mock 1d). */
  warning?: ReactNode
  saveLabel: string
  saving: boolean
  onDiscard: () => void
  /** Submit button of the surrounding form: the bar has type="submit". */
  formId?: string
}

// Ink bar of mock 1d, sticky at the bottom while there are unsaved changes; Ctrl/⌘+S saves.
export function UnsavedChangesBar({ count, warning, saveLabel, saving, onDiscard, formId }: UnsavedChangesBarProps) {
  const { t } = useTranslation()

  useEffect(() => {
    if (count === 0) return
    const onKey = (event: KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 's') {
        event.preventDefault()
        const form = formId ? (document.getElementById(formId) as HTMLFormElement | null) : null
        form?.requestSubmit()
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [count, formId])

  if (count === 0) return null

  return (
    <div
      role="status"
      className="sticky bottom-4 z-10 flex flex-wrap items-center gap-3 rounded-box bg-neutral px-4 py-3 text-neutral-content shadow-lg"
    >
      <FontAwesomeIcon icon={faTriangleExclamation} className="text-warning" />
      <p className="min-w-60 flex-1">
        <b className="font-semibold text-white">{t('unsaved.count', { count })}</b> {warning}
      </p>
      <button type="button" className="btn btn-ghost btn-sm text-neutral-content" onClick={onDiscard}>
        {t('unsaved.discard')}
      </button>
      <button type="submit" form={formId} className="btn btn-primary btn-sm" disabled={saving}>
        {saving && <span className="loading loading-spinner loading-xs" />}
        {saveLabel}
        <Kbd>{t('unsaved.saveShortcut')}</Kbd>
      </button>
    </div>
  )
}
