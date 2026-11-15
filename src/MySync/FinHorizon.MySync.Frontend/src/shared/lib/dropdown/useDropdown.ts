import { useCallback, useEffect, useRef, useState } from 'react'

/** Open/closed state of a drop-down menu: it closes on a click outside it and on Esc, giving focus back to its button. */
export function useDropdown<T extends HTMLElement = HTMLDivElement>() {
  const [open, setOpen] = useState(false)
  const containerRef = useRef<T>(null)
  const buttonRef = useRef<HTMLButtonElement>(null)

  const close = useCallback(() => setOpen(false), [])
  const toggle = useCallback(() => setOpen((current) => !current), [])

  useEffect(() => {
    if (!open) return
    const onPointerDown = (event: MouseEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) setOpen(false)
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setOpen(false)
        buttonRef.current?.focus()
      }
    }
    document.addEventListener('mousedown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('mousedown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [open])

  return { open, toggle, close, containerRef, buttonRef }
}
