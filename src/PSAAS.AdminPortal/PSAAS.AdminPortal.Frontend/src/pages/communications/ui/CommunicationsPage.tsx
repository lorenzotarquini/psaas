import { faEnvelope, faPlus, faTowerBroadcast, faXmark } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router'
import { useCommunication, useCommunications, type Communication } from '@/entities/communication'
import { formatDate, formatWhen } from '@/shared/lib/format'
import { tenantPath } from '@/shared/routes'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { PageHeader } from '@/shared/ui/page-header'
import { StatusBadge } from '@/shared/ui/status-badge'
import { Composer } from './Composer'

// UC27 (EXP-11, no mock: system 1a): what was sent, to how many, with what outcome; the composer opens above the list.
export function CommunicationsPage() {
  const { t, i18n } = useTranslation()
  const [params, setParams] = useSearchParams()
  const composing = params.has('new')
  const selection = params.getAll('tenantId')
  const selected = params.get('communication')
  const page = Math.max(1, Number(params.get('page')) || 1)
  const { data, error, isPending } = useCommunications(page)
  const when = (iso: string) => formatWhen(iso, i18n.language, t('audit.yesterday'))

  const open = (id: string | null) => setParams(id ? { communication: id } : {})

  return (
    <div className={`grid gap-4 ${selected ? 'xl:-mr-7 xl:grid-cols-[minmax(0,1fr)_420px]' : ''}`}>
      <div className="flex min-w-0 flex-col gap-4">
        <PageHeader
          title={t('nav.communications')}
          subtitle={t('communications.subtitle')}
          actions={
            !composing && (
              <button type="button" className="btn btn-primary btn-sm" onClick={() => setParams({ new: '1' })}>
                <FontAwesomeIcon icon={faPlus} />
                {t('communications.new')}
              </button>
            )
          }
        />
        {composing && <Composer selection={selection} onSent={(id) => open(id)} onClose={() => setParams({})} />}
        <div className="panel overflow-hidden">
          {error ? (
            <div className="p-4">
              <ErrorAlert error={error} />
            </div>
          ) : isPending ? (
            <span className="loading loading-spinner m-4" aria-label={t('loading')} />
          ) : data.items.length === 0 ? (
            <p className="py-10 text-center text-base-content/60">{t('communications.empty')}</p>
          ) : (
            <table className="table table-dense">
              <thead>
                <tr>
                  <th>{t('communications.subject')}</th>
                  <th>{t('communications.channels')}</th>
                  <th className="text-right">{t('communications.recipientsShort')}</th>
                  <th>{t('communications.sentAt')}</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((c) => (
                  <tr key={c.id} className={`cursor-pointer hover:bg-base-200/60 ${c.id === selected ? 'bg-primary/6' : ''}`} onClick={() => open(c.id)}>
                    <td>
                      <button type="button" className="font-semibold" onClick={() => open(c.id)}>
                        {c.subject}
                      </button>
                      <span className="block text-[12px] text-base-content/55">{c.sentBy}</span>
                    </td>
                    <td>
                      <Channels communication={c} />
                    </td>
                    <td className="data text-right">
                      {c.recipientCount}
                      {c.emailsFailed > 0 && <span className="ml-2 text-error">({t('communications.failed', { count: c.emailsFailed })})</span>}
                    </td>
                    <td className="data text-[12.5px]">{when(c.sentAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
      {selected && (
        <div className="relative xl:sticky xl:top-0 xl:-my-6 xl:h-[calc(100vh-56px)]">
          <Detail id={selected} onClose={() => open(null)} />
        </div>
      )}
    </div>
  )
}

function Channels({ communication }: { communication: Communication }) {
  const { t } = useTranslation()
  return (
    <span className="flex gap-2 text-base-content/70">
      {communication.channels.includes('InApp') && <FontAwesomeIcon icon={faTowerBroadcast} title={t('communications.channel.InApp')} />}
      {communication.channels.includes('Email') && <FontAwesomeIcon icon={faEnvelope} title={t('communications.channel.Email')} />}
    </span>
  )
}

const emailTones = { Sent: 'success', Failed: 'error', Skipped: 'neutral' } as const

function Detail({ id, onClose }: { id: string; onClose: () => void }) {
  const { t, i18n } = useTranslation()
  const { data: c, error, isPending } = useCommunication(id)

  return (
    <aside className="relative flex h-full flex-col overflow-y-auto border-l border-base-300 bg-base-100" aria-label={t('communications.detail')}>
      <button type="button" className="btn btn-ghost btn-sm btn-square absolute right-3 top-3" onClick={onClose} aria-label={t('communications.close')}>
        <FontAwesomeIcon icon={faXmark} />
      </button>
      {error ? (
        <div className="p-5">
          <ErrorAlert error={error} />
        </div>
      ) : isPending ? (
        <span className="loading loading-spinner m-5" aria-label={t('loading')} />
      ) : (
        <>
          <header className="border-b border-base-300 px-5 pt-5 pb-4">
            <h2 className="pr-8 text-[17px] font-semibold">{c.subject}</h2>
            <p className="text-[12px] text-base-content/60">
              {c.sentBy} · {formatWhen(c.sentAt, i18n.language, t('audit.yesterday'))}
              {c.visibleUntil && ` · ${t('communications.visibleUntil', { date: formatDate(c.visibleUntil, i18n.language) })}`}
            </p>
          </header>
          <p className="whitespace-pre-wrap border-b border-base-300 px-5 py-4">{c.body}</p>
          <table className="table table-dense">
            <thead>
              <tr>
                <th>{t('communications.tenant')}</th>
                <th>{t('communications.channel.Email')}</th>
              </tr>
            </thead>
            <tbody>
              {c.recipients.map((r) => (
                <tr key={r.tenantId}>
                  <td>
                    <Link to={tenantPath(r.tenantId, 'history')} className="link link-hover">
                      {r.tenantName}
                    </Link>
                    <span className="block text-[12px] text-base-content/55">{r.email}</span>
                  </td>
                  <td>
                    {r.emailStatus && <StatusBadge tone={emailTones[r.emailStatus]}>{t(`communications.emailStatus.${r.emailStatus}`)}</StatusBadge>}
                    {r.error && <span className="block text-[12px] text-error">{r.error}</span>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      )}
    </aside>
  )
}
