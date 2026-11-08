import { faCalendar, faColumns, faLayerGroup, faLocationDot, faMagnifyingGlass, faPuzzlePiece, faXmark } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon, type FontAwesomeIconProps } from '@fortawesome/react-fontawesome'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { useModules, usePlans } from '@/entities/catalog'
import type { TenantsQuery } from '@/entities/tenant'
import { optionalColumns, type OptionalColumn } from '../model/searchParams'

interface FilterBarProps {
  query: TenantsQuery
  searchText: string
  onSearchText: (value: string) => void
  onChange: (next: Partial<TenantsQuery>) => void
  columns: OptionalColumn[]
  onColumns: (columns: OptionalColumn[]) => void
}

const renewalOptions = [7, 30, 60, 90]

// Filter row of mock 1c: search, then chips that are dashed while empty and show their value with × once set.
export function FilterBar({ query, searchText, onSearchText, onChange, columns, onColumns }: FilterBarProps) {
  const { t } = useTranslation()
  const { data: plans = [] } = usePlans()
  const { data: modules = [] } = useModules()
  const planNames = plans.filter((p) => query.planIds.includes(p.id)).map((p) => p.name)
  const toggle = <T,>(list: T[], value: T) => (list.includes(value) ? list.filter((x) => x !== value) : [...list, value])

  return (
    <div className="flex flex-wrap items-center gap-2">
      <label className="input input-sm w-full max-w-xs">
        <FontAwesomeIcon icon={faMagnifyingGlass} className="text-base-content/40" />
        <input
          type="search"
          placeholder={t('tenants.searchPlaceholder')}
          aria-label={t('tenants.searchPlaceholder')}
          value={searchText}
          onChange={(e) => onSearchText(e.target.value)}
        />
      </label>

      <Chip icon={faLayerGroup} label={t('tenants.filters.plan')} value={planNames.join(', ')} onClear={() => onChange({ planIds: [] })}>
        {plans.map((plan) => (
          <label key={plan.id} className="flex items-center gap-2 px-2 py-1">
            <input type="checkbox" className="checkbox checkbox-xs" checked={query.planIds.includes(plan.id)} onChange={() => onChange({ planIds: toggle(query.planIds, plan.id) })} />
            {plan.name}
          </label>
        ))}
      </Chip>

      <Chip icon={faPuzzlePiece} label={t('tenants.filters.module')} value={modules.find((m) => m.id === query.moduleId)?.name} onClear={() => onChange({ moduleId: undefined })}>
        {modules.map((module) => (
          <button key={module.id} type="button" className="block w-full rounded px-2 py-1 text-left hover:bg-base-200" onClick={() => onChange({ moduleId: module.id })}>
            {module.name}
          </button>
        ))}
      </Chip>

      <Chip icon={faLocationDot} label={t('tenants.filters.province')} value={query.province} onClear={() => onChange({ province: undefined })}>
        <form
          className="p-1"
          onSubmit={(e) => {
            e.preventDefault()
            const value = new FormData(e.currentTarget).get('province')?.toString().trim().toUpperCase()
            onChange({ province: value || undefined })
          }}
        >
          <input name="province" className="input input-sm w-28 uppercase" maxLength={2} placeholder="MI" aria-label={t('tenants.filters.provinceCode')} defaultValue={query.province} />
        </form>
      </Chip>

      <Chip
        icon={faCalendar}
        label={t('tenants.filters.renewal')}
        value={query.renewalWithinDays ? t('tenants.filters.within', { count: query.renewalWithinDays }) : undefined}
        onClear={() => onChange({ renewalWithinDays: undefined })}
      >
        {renewalOptions.map((days) => (
          <button key={days} type="button" className="block w-full rounded px-2 py-1 text-left hover:bg-base-200" onClick={() => onChange({ renewalWithinDays: days })}>
            {t('tenants.filters.within', { count: days })}
          </button>
        ))}
      </Chip>

      <details className="dropdown dropdown-end ml-auto">
        <summary className="btn btn-ghost btn-sm">
          <FontAwesomeIcon icon={faColumns} />
          {t('tenants.columns')}
        </summary>
        <div className="dropdown-content z-20 mt-1 w-48 rounded-box border border-base-300 bg-base-100 p-2 shadow">
          {optionalColumns.map((column) => (
            <label key={column} className="flex items-center gap-2 px-2 py-1">
              <input type="checkbox" className="checkbox checkbox-xs" checked={columns.includes(column)} onChange={() => onColumns(toggle(columns, column))} />
              {t(`tenants.col.${column}`)}
            </label>
          ))}
        </div>
      </details>
    </div>
  )
}

function Chip({
  icon,
  label,
  value,
  onClear,
  children,
}: {
  icon: FontAwesomeIconProps['icon']
  label: string
  value: string | undefined
  onClear: () => void
  children: ReactNode
}) {
  const { t } = useTranslation()
  return (
    <span className="flex items-center">
      <details className="dropdown">
        <summary
          className={`btn btn-sm font-normal ${value ? 'rounded-r-none border-base-300 bg-base-100' : 'btn-outline border-dashed border-base-300 text-base-content/75'}`}
        >
          <FontAwesomeIcon icon={icon} className="text-[11px]" />
          {label}
          {value && <span className="max-w-40 truncate font-semibold">{value}</span>}
        </summary>
        <div className="dropdown-content z-20 mt-1 max-h-72 min-w-44 overflow-y-auto rounded-box border border-base-300 bg-base-100 p-1.5 shadow">{children}</div>
      </details>
      {value && (
        <button type="button" className="btn btn-sm rounded-l-none border-l-0 border-base-300 bg-base-100 px-2" onClick={onClear} aria-label={t('tenants.filters.clear', { label })}>
          <FontAwesomeIcon icon={faXmark} className="text-[11px]" />
        </button>
      )}
    </span>
  )
}
