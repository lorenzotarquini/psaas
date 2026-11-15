import { useEffect, useId, useRef, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faXmark } from '@fortawesome/free-solid-svg-icons'

interface ModalProps {
  open: boolean
  title: string
  onClose: () => void
  children: ReactNode
}

/** A native <dialog> (focus trap, Esc, backdrop) styled by daisyUI. */
export function Modal({ open, title, onClose, children }: ModalProps) {
  const { t } = useTranslation()
  const ref = useRef<HTMLDialogElement>(null)
  const titleId = useId()

  useEffect(() => {
    const dialog = ref.current
    if (!dialog) return
    // jsdom has no showModal: the open attribute keeps tests meaningful.
    if (open && !dialog.open) {
      if (typeof dialog.showModal === 'function') dialog.showModal()
      else dialog.setAttribute('open', '')
    }
    if (!open && dialog.open) {
      if (typeof dialog.close === 'function') dialog.close()
      else dialog.removeAttribute('open')
    }
  }, [open])

  return (
    <dialog ref={ref} className="modal modal-bottom sm:modal-middle" aria-labelledby={titleId} onClose={onClose}>
      {open && (
        <div className="modal-box max-w-lg">
          <div className="mb-4 flex items-start justify-between gap-4">
            <h2 id={titleId} className="text-lg font-semibold">
              {title}
            </h2>
            <button type="button" className="btn btn-ghost btn-sm btn-circle" aria-label={t('actions.close')} onClick={onClose}>
              <FontAwesomeIcon icon={faXmark} />
            </button>
          </div>
          {children}
        </div>
      )}
      <form method="dialog" className="modal-backdrop">
        <button type="submit" aria-label={t('actions.close')} onClick={onClose} />
      </form>
    </dialog>
  )
}
