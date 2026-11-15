import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlus } from '@fortawesome/free-solid-svg-icons'

interface AddButtonProps {
  label: string
  /** When true the button stays visible but disabled, and the tooltip says why. */
  limitReached: boolean
  limitMessage: string
  onClick: () => void
}

/** "New account" / "New debt": always shown, disabled once the user has as many as allowed (RF2, RF4). */
export function AddButton({ label, limitReached, limitMessage, onClick }: AddButtonProps) {
  const button = (
    <button type="button" className="btn btn-ghost btn-sm text-primary" disabled={limitReached} onClick={onClick}>
      <FontAwesomeIcon icon={faPlus} /> {label}
    </button>
  )
  // A disabled button gets no pointer events: the tooltip and its accessible text live on the wrapper.
  return limitReached ? (
    <span className="tooltip tooltip-left" data-tip={limitMessage} tabIndex={0} aria-label={`${label}: ${limitMessage}`}>
      {button}
    </span>
  ) : (
    button
  )
}
