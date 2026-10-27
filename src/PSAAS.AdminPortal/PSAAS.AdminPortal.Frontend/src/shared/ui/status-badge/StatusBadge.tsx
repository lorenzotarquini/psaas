import type { ReactNode } from 'react'

/** Color is information (screen 1a): green active, amber attention, red critical, indigo info, grey inert. */
export type StatusTone = 'success' | 'warning' | 'error' | 'info' | 'neutral'

const toneClasses: Record<StatusTone, string> = {
  success: 'badge-success',
  warning: 'badge-warning',
  error: 'badge-error',
  info: 'badge-info',
  // Soft with no color follows base-content: badge-neutral would be the sidebar ink, unreadable in dark.
  neutral: '',
}

export function StatusBadge({ tone, children }: { tone: StatusTone; children: ReactNode }) {
  return (
    <span className={`badge badge-soft badge-sm gap-1.5 font-medium whitespace-nowrap ${toneClasses[tone]}`}>
      <span className="size-1.5 rounded-full bg-current" aria-hidden />
      {children}
    </span>
  )
}
