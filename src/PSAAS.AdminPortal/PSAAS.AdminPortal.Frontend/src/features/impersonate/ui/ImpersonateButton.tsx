import { faUserSecret } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useImpersonate, useImpersonationStatus } from '@/entities/overview'
import { useUsers } from '@/entities/user'
import { ApiError } from '@/shared/api'
import { ErrorAlert } from '@/shared/ui/error-alert'

const durations = [15, 30, 60]

/**
 * UC31 (mock 1d "Impersona"): pick an active user (of the tenant, or the given one), say why and for how long, then the
 * platform opens in a new tab with that user's session. Disabled with the reason when not configured.
 */
export function ImpersonateButton({ tenantId, user, className = 'btn btn-sm' }: { tenantId?: string; user?: { id: string; name: string }; className?: string }) {
  const { t } = useTranslation()
  const dialog = useRef<HTMLDialogElement>(null)
  const { data: status } = useImpersonationStatus()
  const impersonate = useImpersonate()
  const { data: candidates } = useUsers({ search: '', tenantId, status: 'Active', page: 1, pageSize: 100 })
  const [userId, setUserId] = useState(user?.id ?? '')
  const [reason, setReason] = useState('')
  const [minutes, setMinutes] = useState(30)
  const errors = impersonate.error instanceof ApiError ? impersonate.error.fieldErrors : {}
  const configured = status?.configured ?? false

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      const ticket = await impersonate.mutateAsync({ userId, reason, minutes })
      // noopener: the platform tab cannot reach back into the console.
      window.open(ticket.url, '_blank', 'noopener')
      dialog.current?.close()
    } catch {
      // Shown below.
    }
  }

  return (
    <>
      <button
        type="button"
        className={className}
        disabled={!configured}
        title={configured ? undefined : t('impersonation.notConfigured')}
        onClick={() => {
          impersonate.reset()
          dialog.current?.showModal()
        }}
      >
        <FontAwesomeIcon icon={faUserSecret} />
        {t('impersonation.button')}
      </button>
      <dialog ref={dialog} className="modal">
        <form onSubmit={submit} className="modal-box flex flex-col gap-3">
          <h2 className="text-[17px] font-semibold">{t('impersonation.title')}</h2>
          <p className="text-base-content/60">{t('impersonation.hint')}</p>
          {!user && (
            <label className="fieldset py-0">
              <span className="fieldset-legend py-1 text-[12px]">{t('impersonation.user')}</span>
              <select className="select w-full" value={userId} onChange={(e) => setUserId(e.target.value)} required>
                <option value="">{t('impersonation.chooseUser')}</option>
                {candidates?.items.map((u) => (
                  <option key={u.id} value={u.id}>
                    {u.firstName} {u.lastName} · {u.email}
                  </option>
                ))}
              </select>
              {candidates?.items.length === 0 && <span className="text-[12px] text-base-content/60">{t('impersonation.noUsers')}</span>}
            </label>
          )}
          {user && <p className="font-semibold">{user.name}</p>}
          <label className="fieldset py-0">
            <span className="fieldset-legend py-1 text-[12px]">{t('impersonation.reason')}</span>
            <input className={`input w-full ${errors.reason ? 'input-error' : ''}`} maxLength={200} value={reason} onChange={(e) => setReason(e.target.value)} placeholder={t('impersonation.reasonPlaceholder')} />
            {errors.reason && <span className="text-[12px] text-error">{t(`impersonation.errors.reason.${errors.reason[0]}`)}</span>}
          </label>
          <label className="fieldset py-0">
            <span className="fieldset-legend py-1 text-[12px]">{t('impersonation.duration')}</span>
            <select className="select w-full" value={minutes} onChange={(e) => setMinutes(Number(e.target.value))}>
              {durations.map((d) => (
                <option key={d} value={d}>
                  {t('impersonation.minutes', { count: d })}
                </option>
              ))}
            </select>
          </label>
          {impersonate.error && !(impersonate.error instanceof ApiError && impersonate.error.status === 400) && (
            <ErrorAlert error={impersonate.error} action="save" />
          )}
          <p className="text-[12px] text-warning">{t('impersonation.audited')}</p>
          <div className="modal-action mt-0">
            <button type="button" className="btn btn-ghost btn-sm" onClick={() => dialog.current?.close()}>
              {t('unsaved.discard')}
            </button>
            <button type="submit" className="btn btn-primary btn-sm" disabled={!userId || impersonate.isPending}>
              {impersonate.isPending && <span className="loading loading-spinner loading-xs" />}
              {t('impersonation.start')}
            </button>
          </div>
        </form>
      </dialog>
    </>
  )
}
