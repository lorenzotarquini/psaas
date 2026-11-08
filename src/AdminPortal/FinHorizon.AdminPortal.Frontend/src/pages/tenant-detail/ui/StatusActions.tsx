import { faEllipsis } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useChangeTenantStatus, type TenantDetail, type TenantStatus } from '@/entities/tenant'
import { ApiError } from '@/shared/api'
import { ErrorAlert } from '@/shared/ui/error-alert'

// UC13-R2: what each status can go to, as the actions of the "…" menu (mock 1d). Drafts finish in the wizard.
const actions: Record<TenantStatus, TenantStatus[]> = {
  Onboarding: [],
  Trial: ['Active', 'Suspended', 'Ceased'],
  Active: ['Suspended', 'Cancelling'],
  Suspended: ['Active', 'Ceased'],
  Cancelling: ['Active', 'Ceased'],
  Ceased: [],
}
const needsReason = (status: TenantStatus) => status === 'Suspended' || status === 'Cancelling' || status === 'Ceased'

export function StatusActions({ tenant }: { tenant: TenantDetail }) {
  const { t } = useTranslation()
  const change = useChangeTenantStatus()
  const [target, setTarget] = useState<TenantStatus>()
  const [reason, setReason] = useState('')
  const available = actions[tenant.lifecycle.status]
  if (available.length === 0) return null

  const close = () => {
    setTarget(undefined)
    setReason('')
    change.reset()
  }
  const confirm = async () => {
    const done = await change.mutateAsync({ id: tenant.id, status: target!, reason: reason || undefined }).catch(() => undefined)
    if (done) close()
  }
  const reasonMissing = change.error instanceof ApiError && Boolean(change.error.fieldErrors.reason)

  return (
    <>
      <div className="dropdown dropdown-end">
        <div tabIndex={0} role="button" className="btn btn-square btn-sm" aria-label={t('tenantStatus.actions')}>
          <FontAwesomeIcon icon={faEllipsis} />
        </div>
        <ul tabIndex={-1} className="dropdown-content menu z-20 mt-1 w-56 rounded-box border border-base-300 bg-base-100 shadow-lg">
          {available.map((status) => (
            <li key={status}>
              <button
                type="button"
                className={status === 'Suspended' || status === 'Ceased' ? 'text-error' : ''}
                onClick={() => {
                  ;(document.activeElement as HTMLElement | null)?.blur()
                  setTarget(status)
                }}
              >
                {t(`tenantStatus.to.${status}`)}
              </button>
            </li>
          ))}
        </ul>
      </div>
      <dialog className={`modal ${target ? 'modal-open' : ''}`} aria-labelledby="status-title">
        {target && (
          <div className="modal-box">
            <h2 id="status-title" className="text-[15px] font-semibold">
              {t(`tenantStatus.to.${target}`)} · {tenant.companyName}
            </h2>
            <p className="py-2 text-base-content/70">{t(`tenantStatus.explain.${target}`)}</p>
            {needsReason(target) && (
              <label className="fieldset">
                <span className="fieldset-legend text-[12px]">{t('tenantStatus.reason')}</span>
                <textarea
                  className={`textarea w-full ${reasonMissing ? 'textarea-error' : ''}`}
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  rows={3}
                />
                {reasonMissing && <span className="text-[12px] text-error">{t('validation.Required')}</span>}
              </label>
            )}
            {change.error && !reasonMissing && <ErrorAlert error={change.error} action="save" />}
            <div className="modal-action">
              <button type="button" className="btn btn-sm" onClick={close}>
                {t('unsaved.discard')}
              </button>
              <button
                type="button"
                className={`btn btn-sm ${target === 'Suspended' || target === 'Ceased' ? 'btn-error' : 'btn-primary'}`}
                disabled={change.isPending}
                onClick={() => void confirm()}
              >
                {t(`tenantStatus.to.${target}`)}
              </button>
            </div>
          </div>
        )}
      </dialog>
    </>
  )
}
