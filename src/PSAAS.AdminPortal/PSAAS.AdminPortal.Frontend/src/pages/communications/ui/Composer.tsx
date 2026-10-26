import { faPaperPlane } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useModules, usePlans } from '@/entities/catalog'
import { useCommunicationPreview, useSendCommunication, type Channel, type CommunicationSegment } from '@/entities/communication'
import { ApiError } from '@/shared/api'
import { useDebouncedValue } from '@/shared/lib/debounce'
import { ErrorAlert } from '@/shared/ui/error-alert'

const statuses = ['Active', 'Trial', 'Onboarding', 'Suspended', 'Cancelling']

const toggle = <T,>(list: T[], value: T) => (list.includes(value) ? list.filter((x) => x !== value) : [...list, value])

// UC27: subject, text, channels and recipients; the count of recipients updates while choosing.
export function Composer({ selection, onSent, onClose }: { selection: string[]; onSent: (id: string) => void; onClose: () => void }) {
  const { t } = useTranslation()
  const { data: plans = [] } = usePlans()
  const { data: modules = [] } = useModules()
  const send = useSendCommunication()
  const [subject, setSubject] = useState('')
  const [body, setBody] = useState('')
  const [channels, setChannels] = useState<Channel[]>(['InApp', 'Email'])
  const [useSelection, setUseSelection] = useState(selection.length > 0)
  const [confirming, setConfirming] = useState(false)
  const [filters, setFilters] = useState<Required<Pick<CommunicationSegment, 'statuses' | 'planIds'>> & { moduleId?: string }>({ statuses: ['Active', 'Trial'], planIds: [] })
  const segment: CommunicationSegment = useSelection ? { tenantIds: selection } : filters
  // Debounced as JSON: the segment object is new on every render.
  const segmentKey = useDebouncedValue(JSON.stringify(segment), 300)
  const { data: preview } = useCommunicationPreview(useMemo(() => JSON.parse(segmentKey) as CommunicationSegment, [segmentKey]))
  const errors = send.error instanceof ApiError ? send.error.fieldErrors : {}
  const error = (field: string) => errors[field] && <p className="text-[12px] text-error">{t(`validation.${errors[field][0]}`)}</p>

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!confirming) return setConfirming(true)
    try {
      onSent((await send.mutateAsync({ subject, body, channels, segment, visibleUntil: null })).id)
    } catch {
      // Shown from send.error.
      setConfirming(false)
    }
  }

  return (
    <form onSubmit={submit} className="panel flex flex-col gap-3 px-5 py-4">
      <h2 className="panel-title">{t('communications.new')}</h2>
      {send.error && !(send.error instanceof ApiError && send.error.status === 400) && <ErrorAlert error={send.error} action="save" />}
      <label className="fieldset py-0">
        <span className="fieldset-legend py-1 text-[12px]">{t('communications.subject')}</span>
        <input className={`input w-full ${errors.subject ? 'input-error' : ''}`} maxLength={150} value={subject} onChange={(e) => setSubject(e.target.value)} />
        {error('subject')}
      </label>
      <label className="fieldset py-0">
        <span className="fieldset-legend flex justify-between py-1 text-[12px]">
          {t('communications.body')}
          <span className="data text-base-content/50">{body.length}/5000</span>
        </span>
        <textarea className={`textarea h-36 w-full ${errors.body ? 'textarea-error' : ''}`} maxLength={5000} value={body} onChange={(e) => setBody(e.target.value)} />
        {error('body')}
      </label>
      <fieldset className="flex flex-wrap items-center gap-4">
        <legend className="mb-1 text-[12px] font-medium">{t('communications.channels')}</legend>
        {(['InApp', 'Email'] as const).map((channel) => (
          <label key={channel} className="flex items-center gap-2">
            <input type="checkbox" className="checkbox checkbox-sm" checked={channels.includes(channel)} onChange={() => setChannels(toggle(channels, channel))} />
            {t(`communications.channel.${channel}`)}
          </label>
        ))}
        {error('channels')}
      </fieldset>

      <fieldset className="flex flex-col gap-2 rounded-box border border-base-300 p-3">
        <legend className="px-1 text-[12px] font-medium">{t('communications.recipients')}</legend>
        {selection.length > 0 && (
          <div className="flex gap-4">
            <label className="flex items-center gap-2">
              <input type="radio" className="radio radio-sm" checked={useSelection} onChange={() => setUseSelection(true)} />
              {t('communications.selection', { count: selection.length })}
            </label>
            <label className="flex items-center gap-2">
              <input type="radio" className="radio radio-sm" checked={!useSelection} onChange={() => setUseSelection(false)} />
              {t('communications.byFilters')}
            </label>
          </div>
        )}
        {!useSelection && (
          <div className="grid gap-3 md:grid-cols-3">
            <div>
              <div className="mb-1 text-[12px] text-base-content/60">{t('communications.filterStatus')}</div>
              {statuses.map((s) => (
                <label key={s} className="flex items-center gap-2">
                  <input type="checkbox" className="checkbox checkbox-xs" checked={filters.statuses.includes(s)} onChange={() => setFilters({ ...filters, statuses: toggle(filters.statuses, s) })} />
                  {t(`tenantStatus.${s}`)}
                </label>
              ))}
            </div>
            <div>
              <div className="mb-1 text-[12px] text-base-content/60">{t('communications.filterPlan')}</div>
              {plans.map((plan) => (
                <label key={plan.id} className="flex items-center gap-2">
                  <input type="checkbox" className="checkbox checkbox-xs" checked={filters.planIds.includes(plan.id)} onChange={() => setFilters({ ...filters, planIds: toggle(filters.planIds, plan.id) })} />
                  {plan.name}
                </label>
              ))}
            </div>
            <label className="flex flex-col">
              <span className="mb-1 text-[12px] text-base-content/60">{t('communications.filterModule')}</span>
              <select className="select select-sm" value={filters.moduleId ?? ''} onChange={(e) => setFilters({ ...filters, moduleId: e.target.value || undefined })}>
                <option value="">{t('communications.anyModule')}</option>
                {modules.map((m) => (
                  <option key={m.id} value={m.id}>
                    {m.name}
                  </option>
                ))}
              </select>
            </label>
          </div>
        )}
        <p className={`text-[13px] ${preview?.count ? 'text-base-content/80' : 'text-error'}`} aria-live="polite">
          {preview?.count
            ? t('communications.reach', { count: preview.count, sample: preview.sample.join(', ') + (preview.count > preview.sample.length ? '…' : '') })
            : t('communications.reachNone')}
        </p>
        {error('segment')}
      </fieldset>

      {confirming ? (
        // Second step before sending: emails cannot be taken back.
        <div role="alertdialog" aria-live="assertive" className="flex flex-wrap items-center justify-end gap-3 rounded-box bg-neutral px-4 py-2.5 text-neutral-content">
          <span className="mr-auto font-semibold text-white">{t('communications.confirm', { count: preview?.count ?? 0 })}</span>
          <button type="button" className="btn btn-ghost btn-sm text-neutral-content" onClick={() => setConfirming(false)}>
            {t('unsaved.discard')}
          </button>
          <button type="submit" autoFocus className="btn btn-primary btn-sm" disabled={send.isPending}>
            {send.isPending ? <span className="loading loading-spinner loading-xs" /> : <FontAwesomeIcon icon={faPaperPlane} />}
            {t('communications.confirmSend')}
          </button>
        </div>
      ) : (
        <div className="flex justify-end gap-2">
          <button type="button" className="btn btn-ghost btn-sm" onClick={onClose}>
            {t('unsaved.discard')}
          </button>
          <button type="submit" className="btn btn-primary btn-sm" disabled={!preview?.count}>
            <FontAwesomeIcon icon={faPaperPlane} />
            {t('communications.send')}
          </button>
        </div>
      )}
    </form>
  )
}
