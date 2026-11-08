import { Link } from 'react-router'

export interface TabItem<K extends string> {
  key: K
  label: string
  /** Shown in mono next to the label, e.g. the number of users. */
  count?: number
  to: string
}

// Underlined tabs of mocks 1c/1d: links, so every tab has its own URL.
export function Tabs<K extends string>({ items, active, label }: { items: TabItem<K>[]; active: K; label: string }) {
  return (
    <nav role="tablist" aria-label={label} className="flex gap-1 overflow-x-auto border-b border-base-300">
      {items.map((item) => {
        const selected = item.key === active
        return (
          <Link
            key={item.key}
            to={item.to}
            role="tab"
            aria-selected={selected}
            className={`-mb-px flex items-center gap-1.5 border-b-2 px-3 py-2 whitespace-nowrap ${
              selected ? 'border-primary font-semibold text-base-content' : 'border-transparent text-base-content/60 hover:text-base-content'
            }`}
          >
            {item.label}
            {item.count !== undefined && (
              <span className="rounded bg-base-200 px-1.5 font-mono text-[11px] text-base-content/70">{item.count}</span>
            )}
          </Link>
        )
      })}
    </nav>
  )
}
