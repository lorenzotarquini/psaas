import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { usePlans } from '@/entities/catalog'
import { ApiError, apiPut } from '@/shared/api'

interface Outcome {
  name: string
  error: string | null
}

// Bulk "Cambia piano" of mock 1c: UC17 for every selected tenant, one at a time; each tenant reports its own outcome.
export function ChangePlanDialog({ tenants, onClose }: { tenants: { id: string; companyName: string }[] | null; onClose: () => void }) {
  const { t } = useTranslation()
  const dialog = useRef<HTMLDialogElement>(null)
  const queryClient = useQueryClient()
  const { data: plans = [] } = usePlans()
  const [planId, setPlanId] = useState('')
  const [outcomes, setOutcomes] = useState<Outcome[] | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    if (tenants) dialog.current?.showModal()
    else dialog.current?.close()
  }, [tenants])

  const close = () => {
    setPlanId('')
    setOutcomes(null)
    onClose()
  }

  const apply = async () => {
    setBusy(true)
    const results: Outcome[] = []
    for (const tenant of tenants ?? []) {
      try {
        await apiPut(`/api/tenants/${encodeURIComponent(tenant.id)}/subscription`, { planId, trial: false, addOnModuleIds: [], autoRenew: true })
        results.push({ name: tenant.companyName, error: null })
      } catch (error) {
        const code = error instanceof ApiError ? (error.fieldErrors.planId?.[0] ?? String(error.status)) : 'network'
        results.push({ name: tenant.companyName, error: code })
      }
    }
    setOutcomes(results)
    setBusy(false)
    for (const key of [['tenants'], ['subscriptions'], ['audit']]) void queryClient.invalidateQueries({ queryKey: key })
  }

  return (
    <dialog ref={dialog} className="modal" onClose={close}>
      <div className="modal-box flex flex-col gap-3">
        <h2 className="text-[17px] font-semibold">{t('tenants.bulk.changePlanTitle', { count: tenants?.length ?? 0 })}</h2>
        <p className="text-base-content/60">{t('tenants.bulk.changePlanHint')}</p>
        {!outcomes ? (
          <select className="select w-full" value={planId} aria-label={t('catalog.plan')} onChange={(e) => setPlanId(e.target.value)}>
            <option value="">{t('catalog.choosePlan')}</option>
            {plans
              .filter((plan) => plan.active)
              .map((plan) => (
                <option key={plan.id} value={plan.id}>
                  {plan.name}
                </option>
              ))}
          </select>
        ) : (
          <ul className="flex flex-col gap-1">
            {outcomes.map((o) => (
              <li key={o.name} className={o.error ? 'text-error' : 'text-success'}>
                {o.name}: {o.error ? t(`catalog.subscriptionErrors.${o.error}`, { defaultValue: t('errors.saveFailed') }) : t('tenants.bulk.done')}
              </li>
            ))}
          </ul>
        )}
        <div className="modal-action mt-0">
          <button type="button" className="btn btn-ghost btn-sm" onClick={close}>
            {t(outcomes ? 'users.import.close' : 'unsaved.discard')}
          </button>
          {!outcomes && (
            <button type="button" className="btn btn-primary btn-sm" disabled={!planId || busy} onClick={() => void apply()}>
              {busy && <span className="loading loading-spinner loading-xs" />}
              {t('tenants.bulk.apply')}
            </button>
          )}
        </div>
      </div>
    </dialog>
  )
}
