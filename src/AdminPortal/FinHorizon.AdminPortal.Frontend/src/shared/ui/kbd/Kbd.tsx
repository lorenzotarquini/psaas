import type { ReactNode } from 'react'

/** Keyboard shortcut hint, e.g. ⌘K or N (mock 1a: "Da tastiera"). */
export function Kbd({ children }: { children: ReactNode }) {
  return <kbd className="kbd kbd-xs font-mono">{children}</kbd>
}
