import { lazy, Suspense, useRef, type RefObject } from 'react'
import Draggable from 'react-draggable'
import { useTranslation } from 'react-i18next'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faGripVertical, faXmark } from '@fortawesome/free-solid-svg-icons'
import { Loading } from '@/shared/ui'

// mathjs is large: it is downloaded the first time a calculator opens, not with the app.
const Calculator = lazy(() => import('./Calculator'))

interface CalculatorWindowProps {
  /** Shown in the title when more than one is open. */
  number: number
  /** Each new window opens a little lower and to the left of the previous one. */
  cascade: number
  /** Position in the stack: the window clicked last is on top. */
  layer: number
  onFocus: () => void
  onClose: () => void
}

/** A floating, non-modal window: dragged by its title bar, the page behind stays usable. */
export function CalculatorWindow({ number, cascade, layer, onFocus, onClose }: CalculatorWindowProps) {
  const { t } = useTranslation()
  // react-draggable needs the DOM node itself with React 19 (no findDOMNode).
  const node = useRef<HTMLElement>(null)
  const title = number > 1 ? `${t('calculator.title')} ${number}` : t('calculator.title')

  return (
    <Draggable nodeRef={node as RefObject<HTMLElement>} handle=".calculator-handle" cancel="button" bounds="body">
      <section
        ref={node}
        role="dialog"
        aria-label={title}
        style={{ top: 72 + cascade * 28, right: 24 + cascade * 28, zIndex: 40 + layer }}
        className="rounded-box border-base-300 bg-base-100 fixed w-80 max-w-[calc(100vw-2rem)] border shadow-2xl"
        onPointerDownCapture={onFocus}
        onFocusCapture={onFocus}
      >
        <header className="calculator-handle border-base-300 flex cursor-move touch-none items-center gap-2 border-b px-3 py-2 select-none">
          <FontAwesomeIcon icon={faGripVertical} className="muted" />
          <h2 className="flex-1 text-sm font-semibold">{title}</h2>
          <button type="button" className="btn btn-ghost btn-xs btn-circle" aria-label={t('actions.close')} onClick={onClose}>
            <FontAwesomeIcon icon={faXmark} />
          </button>
        </header>
        <div className="p-3">
          <Suspense fallback={<Loading />}>
            <Calculator />
          </Suspense>
        </div>
      </section>
    </Draggable>
  )
}
