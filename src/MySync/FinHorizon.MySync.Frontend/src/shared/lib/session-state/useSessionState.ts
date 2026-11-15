import { useCallback, useState } from 'react'

/** "session": until the tab closes (UC12-R2); "local": stays on the device (layout choices). */
export type StateStorage = 'session' | 'local'

const storeOf = (kind: StateStorage) => (kind === 'session' ? sessionStorage : localStorage)

function read<T>(kind: StateStorage, key: string, fallback: T): T {
  try {
    const stored = storeOf(kind).getItem(key)
    return stored === null ? fallback : (JSON.parse(stored) as T)
  } catch {
    return fallback
  }
}

/**
 * State remembered in the browser. By default for the working session: it survives page changes and reloads,
 * and is gone when the tab closes, so every new session starts again from the default (UC12-R2).
 */
export function useSessionState<T>(key: string, fallback: T, kind: StateStorage = 'session') {
  const [value, setValue] = useState<T>(() => read(kind, key, fallback))
  const set = useCallback(
    (next: T) => {
      setValue(next)
      try {
        storeOf(kind).setItem(key, JSON.stringify(next))
      } catch {
        // Private mode or blocked storage: the choice lasts for this page only.
      }
    },
    [key, kind],
  )
  return [value, set] as const
}
