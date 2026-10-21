import { useEffect, useRef, useState } from 'react'

/**
 * Search box bound to a committed value (usually in the URL): typing commits after a pause,
 * and a committed value changed from outside (back/forward) shows up in the box.
 */
export function useDebouncedSearch(committed: string, commit: (value: string) => void, delayMs = 300) {
  const [text, setText] = useState(committed)
  const [lastCommitted, setLastCommitted] = useState(committed)

  // React's "adjust state while rendering" pattern; skip it when the box already says the same thing,
  // so a trailing space being typed is not wiped by its own commit.
  if (committed !== lastCommitted) {
    setLastCommitted(committed)
    if (committed !== text.trim()) setText(committed)
  }

  const commitRef = useRef(commit)
  useEffect(() => {
    commitRef.current = commit
  })

  useEffect(() => {
    const value = text.trim()
    if (value === committed) return
    const timeout = setTimeout(() => commitRef.current(value), delayMs)
    return () => clearTimeout(timeout)
  }, [text, committed, delayMs])

  return [text, setText] as const
}
