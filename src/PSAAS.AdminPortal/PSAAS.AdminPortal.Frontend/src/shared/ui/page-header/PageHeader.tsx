import type { ReactNode } from 'react'
import { useBreadcrumb, type Crumb } from '@/shared/lib/breadcrumb'

interface PageHeaderProps {
  title: string
  /** Shown next to the title in mono, e.g. the number of tenants. */
  count?: number
  subtitle?: ReactNode
  /** Pages above this one in the topbar breadcrumb (e.g. Clienti for a tenant). */
  parents?: Crumb[]
  actions?: ReactNode
}

export function PageHeader({ title, count, subtitle, parents = [], actions }: PageHeaderProps) {
  useBreadcrumb([...parents, { label: title }])

  return (
    <div className="flex flex-wrap items-end justify-between gap-3">
      <div>
        <h1 className="flex items-baseline gap-2 text-[22px] font-semibold tracking-[-0.01em]">
          {title}
          {count !== undefined && <span className="data text-[13px] font-normal text-base-content/50">{count.toLocaleString()}</span>}
        </h1>
        {subtitle && <div className="mt-0.5 text-base-content/60">{subtitle}</div>}
      </div>
      {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
    </div>
  )
}
