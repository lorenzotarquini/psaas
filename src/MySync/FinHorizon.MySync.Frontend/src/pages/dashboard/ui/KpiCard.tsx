import type { ReactNode } from 'react'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import type { IconDefinition } from '@fortawesome/fontawesome-svg-core'

interface KpiCardProps {
  icon: IconDefinition
  label: string
  highlight?: boolean
  children: ReactNode
}

/** One headline figure of the dashboard, like the template's "Total Balance" cards. */
export function KpiCard({ icon, label, highlight = false, children }: KpiCardProps) {
  return (
    <div className={`surface flex items-center gap-4 p-5 ${highlight ? 'ring-1 ring-primary/30' : ''}`}>
      <span className="grid size-12 shrink-0 place-items-center rounded-full bg-primary/10 text-lg text-primary" aria-hidden>
        <FontAwesomeIcon icon={icon} />
      </span>
      <div className="min-w-0">
        <div className="muted text-xs font-medium uppercase tracking-wide">{label}</div>
        <div className="truncate text-xl font-semibold">{children}</div>
      </div>
    </div>
  )
}
