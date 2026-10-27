import { faArrowDownWideShort, faArrowUpWideShort } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { ModuleIcon } from '@/entities/catalog'
import { TenantStatusBadge, type TenantSort, type TenantSummary } from '@/entities/tenant'
import { formatDate } from '@/shared/lib/format'
import { tenantPath, type ListReturnState } from '@/shared/routes'
import { renewalLabel, type OptionalColumn } from '../model/searchParams'

interface TenantsTableProps {
  tenants: TenantSummary[]
  columns: OptionalColumn[]
  sort: string
  onSort: (sort: string) => void
  selected: string[]
  onSelected: (ids: string[]) => void
  returnState: ListReturnState
}

function SortHeader({ by, label, sort, onSort }: { by: TenantSort; label: string; sort: string; onSort: (sort: string) => void }) {
  const active = sort.replace(/^-/, '') === by
  const descending = sort.startsWith('-')
  return (
    <th aria-sort={active ? (descending ? 'descending' : 'ascending') : 'none'}>
      <button type="button" className="flex items-center gap-1.5" onClick={() => onSort(active && !descending ? `-${by}` : by)}>
        {label}
        {active && <FontAwesomeIcon icon={descending ? faArrowDownWideShort : faArrowUpWideShort} className="text-[11px]" />}
      </button>
    </th>
  )
}

// Table of mock 1c: checkbox, company name with VAT, PEC, city, status, plan, modules, users against the limit, renewal.
export function TenantsTable({ tenants, columns, sort, onSort, selected, onSelected, returnState }: TenantsTableProps) {
  const { t, i18n } = useTranslation()
  const show = (column: OptionalColumn) => columns.includes(column)
  const allSelected = tenants.length > 0 && tenants.every((tenant) => selected.includes(tenant.id))
  const toggle = (id: string) => onSelected(selected.includes(id) ? selected.filter((x) => x !== id) : [...selected, id])

  return (
    <div className="overflow-x-auto">
      <table className="table table-dense">
        <thead>
          <tr>
            <th className="w-10">
              <input
                type="checkbox"
                className="checkbox checkbox-xs"
                aria-label={t('tenants.selectPage')}
                checked={allSelected}
                onChange={() => onSelected(allSelected ? selected.filter((id) => !tenants.some((t) => t.id === id)) : [...new Set([...selected, ...tenants.map((t) => t.id)])])}
              />
            </th>
            <SortHeader sort={sort} onSort={onSort} by="companyName" label={t('tenants.companyName')} />
            {show('certifiedEmail') && <th>{t('tenants.col.certifiedEmail')}</th>}
            {show('city') && <SortHeader sort={sort} onSort={onSort} by="city" label={t('tenants.col.city')} />}
            {show('status') && <SortHeader sort={sort} onSort={onSort} by="status" label={t('tenants.col.status')} />}
            {show('plan') && <SortHeader sort={sort} onSort={onSort} by="plan" label={t('tenants.col.plan')} />}
            {show('modules') && <th>{t('tenants.col.modules')}</th>}
            {show('users') && <th>{t('tenants.col.users')}</th>}
            {show('renewal') && <SortHeader sort={sort} onSort={onSort} by="renewsAt" label={t('tenants.col.renewal')} />}
          </tr>
        </thead>
        <tbody>
          {tenants.map((tenant) => {
            const isSelected = selected.includes(tenant.id)
            const renewal = renewalLabel(tenant.renewsAt)
            const usage = tenant.userCount !== null && tenant.maxUsers ? tenant.userCount / tenant.maxUsers : null
            return (
              <tr key={tenant.id} className={isSelected ? 'bg-primary/6' : 'hover:bg-base-200/60'}>
                <td>
                  <input type="checkbox" className="checkbox checkbox-xs" aria-label={t('tenants.select', { name: tenant.companyName })} checked={isSelected} onChange={() => toggle(tenant.id)} />
                </td>
                <td className="leading-tight whitespace-nowrap">
                  <Link to={tenantPath(tenant.id)} state={returnState} className="link link-hover font-semibold">
                    {tenant.companyName}
                  </Link>
                  <span className="data block text-[11.5px] text-base-content/55">{tenant.completeVatNumber}</span>
                </td>
                {show('certifiedEmail') && <td className="whitespace-nowrap">{tenant.certifiedEmail.toLowerCase()}</td>}
                {show('city') && <td className="whitespace-nowrap">{tenant.city}</td>}
                {show('status') && (
                  <td>
                    <TenantStatusBadge status={tenant.status} />
                  </td>
                )}
                {show('plan') && <td className="font-semibold">{tenant.planName ?? '—'}</td>}
                {show('modules') && (
                  <td>
                    <span className="flex gap-1">
                      {tenant.modules.map((module) => (
                        <span key={module.id} title={module.name} className={module.status === 'Failed' ? 'rounded-md ring-1 ring-error' : ''}>
                          <ModuleIcon icon={module.icon} />
                        </span>
                      ))}
                    </span>
                  </td>
                )}
                {show('users') && (
                  <td className="min-w-28">
                    <span className="data text-[12px]">
                      {tenant.userCount ?? '–'} / {tenant.maxUsers ?? '∞'}
                    </span>
                    {usage !== null && (
                      <progress
                        className={`progress h-1 w-full ${usage >= 0.9 ? 'progress-warning' : 'progress-primary'}`}
                        value={Math.min(usage, 1) * 100}
                        max={100}
                      />
                    )}
                  </td>
                )}
                {show('renewal') && (
                  <td className={`data text-[12.5px] ${renewal.kind === 'expired' || renewal.kind === 'soon' ? 'text-error' : ''}`}>
                    {renewal.kind === 'none'
                      ? '–'
                      : renewal.kind === 'expired'
                        ? t('tenants.expired')
                        : renewal.kind === 'soon'
                          ? t('tenants.inDays', { count: renewal.days })
                          : formatDate(tenant.renewsAt!, i18n.language)}
                  </td>
                )}
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
