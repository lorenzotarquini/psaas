import { faRotate } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { useServicesHealth, type ServiceStatus } from '@/entities/usage'
import { formatWhen } from '@/shared/lib/format'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { StatusBadge, type StatusTone } from '@/shared/ui/status-badge'

const tones: Record<ServiceStatus, StatusTone> = { Up: 'success', Degraded: 'warning', Down: 'error', NotConfigured: 'neutral' }

// From the platform itself out to the integrations; unknown keys go last.
const order = ['database', 'keycloak', 'keycloak-admin', 'city-list', 'module-hooks', 'invoice-transmission']
const rank = (key: string) => (order.includes(key) ? order.indexOf(key) : order.length)

// UC26 (mock 1b "Servizi collegati"): one row per connected system with status and response time.
export function ServicesStatus() {
  const { t, i18n } = useTranslation()
  const { data, error, isPending, isFetching, refetch } = useServicesHealth()

  return (
    <section className="panel">
      <div className="flex items-center justify-between border-b border-base-300 px-4 py-3">
        <h2 className="panel-title flex items-center gap-2">
          {t('services.title')}
          {data && <StatusBadge tone={tones[data.status]}>{t(`services.status.${data.status}`)}</StatusBadge>}
        </h2>
        <button type="button" className="btn btn-ghost btn-xs" onClick={() => void refetch()} disabled={isFetching} aria-label={t('services.refresh')}>
          <FontAwesomeIcon icon={faRotate} spin={isFetching} />
        </button>
      </div>
      {error ? (
        <div className="p-4">
          <ErrorAlert error={error} />
        </div>
      ) : isPending ? (
        <span className="loading loading-spinner m-4" aria-label={t('loading')} />
      ) : (
        <>
          <ul className="divide-y divide-base-300">
            {[...data.services].sort((a, b) => rank(a.key) - rank(b.key)).map((service) => (
              <li key={service.key} className="flex items-center gap-3 px-4 py-2.5">
                <div className="min-w-0 flex-1">
                  <div className="font-medium">{t(`services.names.${service.key}`, { defaultValue: service.key })}</div>
                  {service.detail && (
                    <div className="truncate text-[12px] text-base-content/60" title={service.detail}>
                      {service.key === 'module-hooks'
                        ? t('services.failedModules', { count: Number(service.detail) })
                        : service.detail}
                    </div>
                  )}
                  {service.status === 'NotConfigured' && <div className="text-[12px] text-base-content/60">{t(`services.notConfigured.${service.key}`, { defaultValue: '' })}</div>}
                </div>
                {service.status !== 'NotConfigured' && <span className="data text-[12px] text-base-content/55">{service.latencyMs} ms</span>}
                <StatusBadge tone={tones[service.status]}>{t(`services.status.${service.status}`)}</StatusBadge>
              </li>
            ))}
          </ul>
          <p className="border-t border-base-300 px-4 py-2 text-[12px] text-base-content/55">
            {t('services.checkedAt', { when: formatWhen(data.checkedAt, i18n.language, t('audit.yesterday')) })}
          </p>
        </>
      )}
    </section>
  )
}
