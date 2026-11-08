import { faArrowRight } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import type { ReactNode } from 'react'

interface StepFooterProps {
  onCancel: () => void
  cancelLabel: string
  /** e.g. "Salta organizzazione e attiva" (mock 1e). */
  secondary?: ReactNode
  primaryLabel: string
  busy: boolean
  /** The step form; the primary button submits it. */
  formId: string
}

// Footer of mock 1e: cancel on the left, skip and next on the right.
export function StepFooter({ onCancel, cancelLabel, secondary, primaryLabel, busy, formId }: StepFooterProps) {
  return (
    <footer className="mt-auto flex flex-wrap items-center gap-2 border-t border-base-300 pt-4">
      <button type="button" className="btn btn-ghost btn-sm" onClick={onCancel}>
        {cancelLabel}
      </button>
      <div className="ml-auto flex flex-wrap gap-2">
        {secondary}
        <button type="submit" form={formId} className="btn btn-primary btn-sm" disabled={busy}>
          {busy && <span className="loading loading-spinner loading-xs" />}
          {primaryLabel}
          <FontAwesomeIcon icon={faArrowRight} />
        </button>
      </div>
    </footer>
  )
}
