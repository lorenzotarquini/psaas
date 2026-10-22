import { faPlus, faXmark } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useDeleteView, useSavedViews, useSaveView } from '@/entities/saved-view'
import { useTenantCounts, type TenantCounts } from '@/entities/tenant'
import { ApiError } from '@/shared/api'
import { statusTabs } from '../model/searchParams'

const countOf = (counts: TenantCounts | undefined, key: string) =>
  !counts ? undefined : key === 'all' ? counts.all : key === 'renewingSoon' ? counts.renewingSoon : (counts.byStatus[key as keyof TenantCounts['byStatus']] ?? 0)

interface ListTabsProps {
  /** The current filters as URL parameters (no page). */
  filters: string
  /** URL parameters of each status tab. */
  tabFilters: Record<string, string>
  onApply: (filters: string) => void
}

// Tabs of mock 1c: status tabs with counts (UC21-R1), then the administrator's saved views (UC22) and "+ Salva vista".
export function ListTabs({ filters, tabFilters, onApply }: ListTabsProps) {
  const { t } = useTranslation()
  const { data: counts } = useTenantCounts()
  const { data: views = [] } = useSavedViews('tenants')
  const save = useSaveView('tenants')
  const remove = useDeleteView('tenants')
  const [naming, setNaming] = useState(false)
  const [name, setName] = useState('')
  const nameError = save.error instanceof ApiError ? save.error.fieldErrors.name?.[0] : undefined
  const tabClass = (active: boolean) =>
    `flex items-center gap-1.5 border-b-2 px-3 pb-2.5 pt-1 whitespace-nowrap ${active ? 'border-primary font-semibold text-base-content' : 'border-transparent text-base-content/65 hover:text-base-content'}`

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      await save.mutateAsync({ name, query: filters })
      setNaming(false)
      setName('')
    } catch {
      // Shown next to the field from save.error.
    }
  }

  return (
    <nav className="flex flex-wrap items-end gap-1 border-b border-base-300" aria-label={t('tenants.tabsLabel')}>
      {statusTabs.map((tab) => {
        const count = countOf(counts, tab.key)
        const active = filters === tabFilters[tab.key]
        return (
          <button key={tab.key} type="button" className={tabClass(active)} aria-current={active ? 'page' : undefined} onClick={() => onApply(tabFilters[tab.key])}>
            {t(`tenants.tabs.${tab.key}`)}
            {count !== undefined && <span className="rounded bg-base-200 px-1.5 font-mono text-[11px] text-base-content/70">{count}</span>}
          </button>
        )
      })}
      {views.map((view) => {
        const active = filters === view.query
        return (
          <span key={view.id} className={tabClass(active)}>
            <button type="button" onClick={() => onApply(view.query)} aria-current={active ? 'page' : undefined}>
              {view.name}
            </button>
            <button type="button" className="text-base-content/40 hover:text-error" aria-label={t('tenants.deleteView', { name: view.name })} onClick={() => remove.mutate(view.id)}>
              <FontAwesomeIcon icon={faXmark} className="text-[10px]" />
            </button>
          </span>
        )
      })}
      {naming ? (
        <form onSubmit={submit} className="flex items-center gap-1.5 pb-1.5">
          <input
            autoFocus
            className={`input input-xs w-44 ${nameError ? 'input-error' : ''}`}
            placeholder={t('tenants.viewName')}
            aria-label={t('tenants.viewName')}
            maxLength={40}
            value={name}
            onChange={(e) => setName(e.target.value)}
            onKeyDown={(e) => e.key === 'Escape' && setNaming(false)}
          />
          <button type="submit" className="btn btn-primary btn-xs" disabled={save.isPending}>
            {t('tenants.saveView')}
          </button>
          {nameError && <span className="text-[12px] text-error">{t(`validation.${nameError}`)}</span>}
        </form>
      ) : (
        <button type="button" className="flex items-center gap-1.5 px-3 pb-2.5 pt-1 text-primary hover:underline" onClick={() => setNaming(true)}>
          <FontAwesomeIcon icon={faPlus} className="text-[11px]" />
          {t('tenants.saveView')}
        </button>
      )}
    </nav>
  )
}
