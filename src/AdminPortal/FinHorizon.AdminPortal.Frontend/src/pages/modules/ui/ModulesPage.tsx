import { faPen, faPlus } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ModuleIcon, ModuleStatusBadge, useModules, useSetModuleStatus, type Module } from '@/entities/catalog'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { PageHeader } from '@/shared/ui/page-header'
import { ManifestForm } from './ManifestForm'

// UC15 (EXP-01): the module catalog; registering a manifest adds a module or a new version, "Edit" changes one (R7).
export function ModulesPage() {
  const { t } = useTranslation()
  const { data: modules = [], error, isPending } = useModules()
  const setStatus = useSetModuleStatus()
  const [registering, setRegistering] = useState(false)
  const [editing, setEditing] = useState<Module | null>(null)
  const startRegistering = () => {
    setEditing(null)
    setRegistering(true)
  }
  const startEditing = (module: Module) => {
    setRegistering(false)
    setEditing(module)
  }

  return (
    <>
      <PageHeader
        title={t('nav.modules')}
        count={isPending ? undefined : modules.length}
        subtitle={t('catalog.modulesSubtitle')}
        actions={
          <button type="button" className="btn btn-primary btn-sm" onClick={startRegistering} disabled={registering}>
            <FontAwesomeIcon icon={faPlus} />
            {t('catalog.registerModule')}
          </button>
        }
      />
      {registering && <ManifestForm onClose={() => setRegistering(false)} />}
      {editing && <ManifestForm key={editing.id} module={editing} onClose={() => setEditing(null)} />}
      {setStatus.error && <ErrorAlert error={setStatus.error} action="save" />}
      <div className="panel overflow-hidden">
        {error ? (
          <div className="p-4">
            <ErrorAlert error={error} />
          </div>
        ) : isPending ? (
          <span className="loading loading-spinner m-4" aria-label={t('loading')} />
        ) : (
          <div className="overflow-x-auto">
            <table className="table table-dense">
              <thead>
                <tr>
                  <th>{t('catalog.module')}</th>
                  <th>{t('catalog.version')}</th>
                  <th>{t('catalog.status')}</th>
                  <th>{t('catalog.requires')}</th>
                  <th>{t('catalog.permissions')}</th>
                  <th className="text-right">{t('catalog.tenants')}</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {modules.map((module) => (
                  <ModuleRow
                    key={module.id}
                    module={module}
                    busy={setStatus.isPending}
                    onStatus={(status) => setStatus.mutate({ id: module.id, status })}
                    onEdit={() => startEditing(module)}
                  />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </>
  )
}

function ModuleRow({
  module,
  busy,
  onStatus,
  onEdit,
}: {
  module: Module
  busy: boolean
  onStatus: (status: Module['status']) => void
  onEdit: () => void
}) {
  const { t } = useTranslation()
  return (
    <tr className="hover:bg-base-200/60">
      <td>
        <span className="flex items-center gap-2.5">
          <ModuleIcon icon={module.icon} />
          <span>
            <span className="font-semibold">{module.name}</span>
            <span className="data block text-[11px] text-base-content/50">{module.id}</span>
          </span>
        </span>
      </td>
      <td className="data">v{module.version}</td>
      <td>
        <ModuleStatusBadge status={module.status} />
      </td>
      <td className="data text-[12px]">{module.requires.join(', ') || '–'}</td>
      <td className="text-[12px] text-base-content/70" title={module.permissions.join('\n')}>
        {t('catalog.permissionCount', { count: module.permissions.length })}
      </td>
      <td className="data text-right">{module.tenantCount}</td>
      <td className="whitespace-nowrap text-right">
        <button type="button" className="btn btn-ghost btn-xs" onClick={onEdit} aria-label={t('catalog.editModule', { name: module.name })}>
          <FontAwesomeIcon icon={faPen} />
          {t('catalog.edit')}
        </button>
        <button type="button" className="btn btn-ghost btn-xs" disabled={busy} onClick={() => onStatus(module.status === 'Deprecated' ? 'Available' : 'Deprecated')}>
          {t(module.status === 'Deprecated' ? 'catalog.restore' : 'catalog.deprecate')}
        </button>
      </td>
    </tr>
  )
}
