import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  formatLimits,
  formatPrice,
  ModuleIcon,
  useModules,
  usePlans,
  useSubscribe,
  type Plan,
  type Subscription,
} from '@/entities/catalog'
import { ApiError } from '@/shared/api'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { addOnCandidates, isUnchanged, type SubscriptionChoice } from '../model/choice'

interface SubscriptionEditorProps {
  tenantId: string
  current: Subscription | null
  /** The form id, so the caller places the submit button (tab footer, wizard footer). */
  formId: string
  /** Called after saving, or right away when nothing changed (current may be null: no plan chosen). */
  onSaved: (subscription: Subscription | null) => void
}

// UC17 (mock 1d "Abbonamento e moduli", mock 1e step 2): plan, trial, add-ons; the backend checks limits.
export function SubscriptionEditor({ tenantId, current, formId, onSaved }: SubscriptionEditorProps) {
  const { t, i18n } = useTranslation()
  const { data: plans = [], error: plansError } = usePlans()
  const { data: modules = [] } = useModules()
  const subscribe = useSubscribe(tenantId)
  const [choice, setChoice] = useState<SubscriptionChoice>({
    planId: current?.planId ?? null,
    trial: false,
    addOnModuleIds: current?.addOnModuleIds ?? [],
    autoRenew: current?.autoRenew ?? true,
  })

  const sellable = plans.filter((plan) => plan.active || plan.id === current?.planId)
  const plan = plans.find((p) => p.id === choice.planId)
  const candidates = addOnCandidates(plan, modules)
  const canTrial = !current && Boolean(plan?.trialDays)
  const errors = subscribe.error instanceof ApiError ? subscribe.error.fieldErrors : {}
  const per = { Monthly: t('catalog.per.Monthly'), Yearly: t('catalog.per.Yearly') }

  const toggleAddOn = (id: string) =>
    setChoice((c) => ({
      ...c,
      addOnModuleIds: c.addOnModuleIds.includes(id) ? c.addOnModuleIds.filter((x) => x !== id) : [...c.addOnModuleIds, id],
    }))

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (isUnchanged(choice, current)) return onSaved(current)
    try {
      onSaved(await subscribe.mutateAsync({ ...choice, trial: canTrial && choice.trial }))
    } catch {
      // Shown below from subscribe.error.
    }
  }

  return (
    <form id={formId} onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
      {plansError && <ErrorAlert error={plansError} />}
      {subscribe.error && !(subscribe.error instanceof ApiError && subscribe.error.status === 400) && (
        <ErrorAlert error={subscribe.error} action="save" />
      )}
      {errors.planId && (
        <div role="alert" className="alert alert-error alert-soft">
          {t(`catalog.subscriptionErrors.${errors.planId[0]}`, { defaultValue: t(`validation.${errors.planId[0]}`) })}
        </div>
      )}
      {sellable.length === 0 ? (
        <p className="panel px-5 py-4 text-base-content/60">{t('catalog.noPlans')}</p>
      ) : (
        <fieldset className="grid gap-3 md:grid-cols-2 xl:grid-cols-3" aria-label={t('catalog.plan')}>
          {sellable.map((p) => (
            <PlanCard
              key={p.id}
              plan={p}
              selected={p.id === choice.planId}
              price={formatPrice(p.price, p.billingPeriod, i18n.language, per)}
              onSelect={() => setChoice((c) => ({ ...c, planId: p.id, addOnModuleIds: c.addOnModuleIds.filter((id) => !p.moduleIds.includes(id)) }))}
            />
          ))}
        </fieldset>
      )}
      {plan && (
        <div className="panel flex flex-col gap-3 px-5 py-4">
          {canTrial && (
            <label className="flex items-center gap-2">
              <input type="checkbox" className="checkbox checkbox-sm" checked={choice.trial} onChange={(e) => setChoice((c) => ({ ...c, trial: e.target.checked }))} />
              {t('catalog.startTrial', { count: plan.trialDays })}
            </label>
          )}
          <label className="flex items-center gap-2">
            <input type="checkbox" className="checkbox checkbox-sm" checked={choice.autoRenew} onChange={(e) => setChoice((c) => ({ ...c, autoRenew: e.target.checked }))} />
            {t('catalog.autoRenew')}
          </label>
          {candidates.length > 0 && (
            <div>
              <div className="eyebrow pb-1.5 text-base-content/55">{t('catalog.addOns')}</div>
              <ul className="grid gap-2 md:grid-cols-2">
                {candidates.map((module) => (
                  <li key={module.id}>
                    <label className="flex items-center gap-2.5 rounded-box border border-base-300 px-3 py-2">
                      <input type="checkbox" className="checkbox checkbox-sm" checked={choice.addOnModuleIds.includes(module.id)} onChange={() => toggleAddOn(module.id)} />
                      <ModuleIcon icon={module.icon} />
                      <span className="flex-1">{module.name}</span>
                      <span className="data text-[11px] text-base-content/50">v{module.version}</span>
                    </label>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )}
    </form>
  )
}

function PlanCard({ plan, selected, price, onSelect }: { plan: Plan; selected: boolean; price: string; onSelect: () => void }) {
  const { t } = useTranslation()
  return (
    <label className={`panel flex cursor-pointer flex-col gap-1.5 px-4 py-3.5 ${selected ? 'outline-2 outline-primary' : ''}`}>
      <span className="flex items-center justify-between">
        <span className="font-semibold">{plan.name}</span>
        <input type="radio" name="planId" className="radio radio-sm radio-primary" checked={selected} onChange={onSelect} />
      </span>
      <span className="data text-[15px]">{price}</span>
      <span className="text-[12px] text-base-content/60">
        {formatLimits(t, plan.maxUsers, plan.maxProductiveUnits)}
      </span>
      <span className="text-[12px] text-base-content/60">{t('catalog.moduleCount', { count: plan.moduleIds.length })}</span>
      {plan.trialDays > 0 && <span className="text-[12px] text-info">{t('catalog.trialDays', { count: plan.trialDays })}</span>}
    </label>
  )
}
