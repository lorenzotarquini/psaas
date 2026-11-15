import { useCallback, useSyncExternalStore } from 'react'

export type ThemeMode = 'light' | 'dark'

// Names of the daisyUI themes declared in app/styles/theme.css.
const themeNames: Record<ThemeMode, string> = { light: 'mysync', dark: 'mysync-dark' }
const storageKey = 'mysync.theme'
const listeners = new Set<() => void>()

function storedMode(): ThemeMode | null {
  try {
    const value = localStorage.getItem(storageKey)
    return value === 'light' || value === 'dark' ? value : null
  } catch {
    return null
  }
}

function systemMode(): ThemeMode {
  return typeof window !== 'undefined' && window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

let current: ThemeMode = storedMode() ?? systemMode()

function apply(mode: ThemeMode) {
  document.documentElement.dataset.theme = themeNames[mode]
}

/** Applies the remembered (or system) mode before the first render, so there is no flash of the other theme. */
export function initTheme() {
  apply(current)
}

export function setThemeMode(mode: ThemeMode) {
  current = mode
  apply(mode)
  try {
    localStorage.setItem(storageKey, mode)
  } catch {
    // Private mode or blocked storage: the choice lasts for this page only.
  }
  listeners.forEach((listener) => listener())
}

const subscribe = (listener: () => void) => {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

/** Light or dark mode, shared by every component that reads it. */
export function useThemeMode() {
  const mode = useSyncExternalStore(subscribe, () => current)
  const toggle = useCallback(() => setThemeMode(current === 'dark' ? 'light' : 'dark'), [])
  return { mode, toggle }
}
