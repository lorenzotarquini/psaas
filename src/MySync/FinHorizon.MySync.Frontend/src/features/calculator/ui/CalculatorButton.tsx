import { useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { useTranslation } from 'react-i18next'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCalculator } from '@fortawesome/free-solid-svg-icons'
import { CalculatorWindow } from './CalculatorWindow'

/**
 * The navbar button: every click opens one more floating calculator. The windows live above the page,
 * so the user keeps working (copy, paste, other pages) and closes each one when done.
 */
export function CalculatorButton() {
  const { t } = useTranslation()
  // Open windows, back to front: the last one is drawn on top.
  const [windows, setWindows] = useState<number[]>([])
  const nextId = useRef(1)

  const open = () => setWindows((current) => [...current, nextId.current++])
  const close = (id: number) => setWindows((current) => current.filter((other) => other !== id))
  const raise = (id: number) => setWindows((current) => (current.at(-1) === id ? current : [...current.filter((other) => other !== id), id]))

  return (
    <>
      <span className="tooltip tooltip-bottom" data-tip={t('calculator.open')}>
        <button type="button" className="btn btn-ghost btn-sm btn-circle" aria-label={t('calculator.open')} onClick={open}>
          <FontAwesomeIcon icon={faCalculator} />
        </button>
      </span>
      {createPortal(
        // Sorted by id so React keeps each window's state; the stacking order is only the z-index.
        [...windows]
          .sort((a, b) => a - b)
          .map((id) => (
            <CalculatorWindow
              key={id}
              number={id}
              cascade={(id - 1) % 6}
              layer={windows.indexOf(id)}
              onFocus={() => raise(id)}
              onClose={() => close(id)}
            />
          )),
        document.body,
      )}
    </>
  )
}
