import { faBan, faCircleCheck, faXmark } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { formatMoney, InvoiceStatusBadge, useCancelInvoice, useInvoice, useRegisterPayment, type Invoice } from '@/entities/invoice'
import { ApiError } from '@/shared/api'
import { formatDate } from '@/shared/lib/format'
import { tenantPath } from '@/shared/routes'
import { ErrorAlert } from '@/shared/ui/error-alert'

// UC24 "Apri fattura": the invoice as issued, its history, payment registration and cancellation.
export function InvoicePanel({ invoiceId, onClose, onOpen }: { invoiceId: string; onClose: () => void; onOpen: (id: string) => void }) {
  const { t } = useTranslation()
  const { data: invoice, error, isPending } = useInvoice(invoiceId)

  return (
    <aside className="relative flex h-full flex-col overflow-y-auto border-l border-base-300 bg-base-100" aria-label={t('billing.panel')}>
      <button type="button" className="btn btn-ghost btn-sm btn-square absolute right-3 top-3" onClick={onClose} aria-label={t('billing.closePanel')}>
        <FontAwesomeIcon icon={faXmark} />
      </button>
      {error ? (
        <div className="p-5">
          <ErrorAlert error={error} />
        </div>
      ) : isPending ? (
        <span className="loading loading-spinner m-5" aria-label={t('loading')} />
      ) : (
        <Body key={invoice.status} invoice={invoice} onOpen={onOpen} />
      )}
    </aside>
  )
}

function Body({ invoice, onOpen }: { invoice: Invoice; onOpen: (id: string) => void }) {
  const { t, i18n } = useTranslation()
  const money = (amount: number) => formatMoney(amount, i18n.language)
  const date = (iso: string) => formatDate(iso, i18n.language)
  const open = invoice.kind === 'Invoice' && (invoice.status === 'Issued' || invoice.status === 'Overdue')

  return (
    <div className="flex flex-col">
      <header className="border-b border-base-300 px-5 pt-5 pb-4">
        <div className="eyebrow text-base-content/55">{t(`billing.kind.${invoice.kind}`)}</div>
        <h2 className="data text-[20px] font-semibold">{invoice.number}</h2>
        <div className="mt-1.5 flex flex-wrap items-center gap-2">
          <InvoiceStatusBadge status={invoice.status} />
          {invoice.remindersSent > 0 && <span className="badge badge-ghost badge-sm">{t('billing.reminders', { count: invoice.remindersSent })}</span>}
        </div>
      </header>

      <section className="grid grid-cols-[7rem_1fr] gap-x-3 gap-y-2 border-b border-base-300 px-5 py-4">
        <span className="text-base-content/60">{t('billing.customer')}</span>
        <span>
          <Link to={tenantPath(invoice.tenantId, 'invoices')} className="link link-hover font-semibold">
            {invoice.tenantName}
          </Link>
          <span className="data block text-[12px] text-base-content/60">{invoice.tenantVatNumber}</span>
          <span className="block text-[12px] text-base-content/60">{invoice.tenantCertifiedEmail.toLowerCase()}</span>
          {invoice.tenantSdiCode && <span className="data block text-[12px] text-base-content/60">SDI {invoice.tenantSdiCode}</span>}
        </span>
        <span className="text-base-content/60">{t('billing.issuedAt')}</span>
        <span className="data">{date(invoice.issuedAt)}</span>
        <span className="text-base-content/60">{t('billing.dueAt')}</span>
        <span className="data">{date(invoice.dueAt)}</span>
        {invoice.periodFrom && invoice.periodTo && (
          <>
            <span className="text-base-content/60">{t('billing.period')}</span>
            <span className="data">
              {date(invoice.periodFrom)} – {date(invoice.periodTo)}
            </span>
          </>
        )}
        {invoice.relatedInvoiceId && (
          <>
            <span className="text-base-content/60">{t(invoice.kind === 'CreditNote' ? 'billing.cancels' : 'billing.creditNote')}</span>
            <button type="button" className="link link-primary data w-fit" onClick={() => onOpen(invoice.relatedInvoiceId!)}>
              {invoice.relatedNumber}
            </button>
          </>
        )}
        {invoice.cancelReason && (
          <>
            <span className="text-base-content/60">{t('billing.reason')}</span>
            <span>{invoice.cancelReason}</span>
          </>
        )}
        {invoice.paidAt && invoice.kind === 'Invoice' && (
          <>
            <span className="text-base-content/60">{t('billing.paidAt')}</span>
            <span>
              <span className="data">{date(invoice.paidAt)}</span>
              {invoice.paymentReference && <span className="text-base-content/60"> · {invoice.paymentReference}</span>}
            </span>
          </>
        )}
      </section>

      <section className="border-b border-base-300 px-5 py-4">
        <table className="table table-dense">
          <thead>
            <tr>
              <th>{t('billing.description')}</th>
              <th className="text-right">{t('billing.amount')}</th>
            </tr>
          </thead>
          <tbody>
            {invoice.lines.map((line, i) => (
              <tr key={i}>
                <td>{line.description}</td>
                <td className="data text-right">{money(line.amount)}</td>
              </tr>
            ))}
          </tbody>
        </table>
        <dl className="mt-3 grid grid-cols-[1fr_auto] gap-x-4 gap-y-1 text-right">
          <dt className="text-base-content/60">{t('billing.net')}</dt>
          <dd className="data">{money(invoice.net)}</dd>
          <dt className="text-base-content/60">{t('billing.vat', { rate: Math.round(invoice.vatRate * 100) })}</dt>
          <dd className="data">{money(invoice.vat)}</dd>
          <dt className="font-semibold">{t('billing.total')}</dt>
          <dd className="data text-[16px] font-semibold">{money(invoice.total)}</dd>
        </dl>
      </section>

      {open && <Actions invoice={invoice} />}
    </div>
  )
}

function Actions({ invoice }: { invoice: Invoice }) {
  const { t } = useTranslation()
  const pay = useRegisterPayment(invoice.id)
  const cancel = useCancelInvoice(invoice.id)
  const [paidAt, setPaidAt] = useState(() => new Date().toISOString().slice(0, 10))
  const [reference, setReference] = useState('')
  const [reason, setReason] = useState('')
  const [cancelling, setCancelling] = useState(false)
  const payErrors = pay.error instanceof ApiError ? pay.error.fieldErrors : {}
  const reasonError = cancel.error instanceof ApiError ? cancel.error.fieldErrors.reason?.[0] : undefined

  return (
    <section className="flex flex-col gap-4 px-5 py-4">
      <form
        className="flex flex-col gap-2"
        onSubmit={(e) => {
          e.preventDefault()
          pay.mutate({ paidAt, reference })
        }}
      >
        <h3 className="panel-title">{t('billing.registerPayment')}</h3>
        <div className="grid grid-cols-2 gap-2">
          <label className="fieldset py-0">
            <span className="fieldset-legend py-1 text-[12px]">{t('billing.paymentDate')}</span>
            <input type="date" className={`input input-sm w-full ${payErrors.paidAt ? 'input-error' : ''}`} value={paidAt} onChange={(e) => setPaidAt(e.target.value)} />
          </label>
          <label className="fieldset py-0">
            <span className="fieldset-legend py-1 text-[12px]">{t('billing.reference')}</span>
            <input className="input input-sm w-full" maxLength={100} placeholder="CRO…" value={reference} onChange={(e) => setReference(e.target.value)} />
          </label>
        </div>
        {payErrors.paidAt && <p className="text-[12px] text-error">{t(`validation.${payErrors.paidAt[0]}`)}</p>}
        {pay.error && !(pay.error instanceof ApiError && pay.error.status === 400) && <ErrorAlert error={pay.error} action="save" />}
        <button type="submit" className="btn btn-primary btn-sm w-fit" disabled={pay.isPending}>
          <FontAwesomeIcon icon={faCircleCheck} />
          {t('billing.markPaid')}
        </button>
      </form>

      {cancelling ? (
        <form
          className="flex flex-col gap-2"
          onSubmit={(e) => {
            e.preventDefault()
            cancel.mutate(reason)
          }}
        >
          <label className="fieldset py-0">
            <span className="fieldset-legend py-1 text-[12px]">{t('billing.cancelReason')}</span>
            <input autoFocus className={`input input-sm w-full ${reasonError ? 'input-error' : ''}`} value={reason} onChange={(e) => setReason(e.target.value)} />
          </label>
          {reasonError && <p className="text-[12px] text-error">{t(`validation.${reasonError}`)}</p>}
          <p className="text-[12px] text-base-content/60">{t('billing.cancelHint')}</p>
          <div className="flex gap-2">
            <button type="button" className="btn btn-ghost btn-sm" onClick={() => setCancelling(false)}>
              {t('unsaved.discard')}
            </button>
            <button type="submit" className="btn btn-soft btn-error btn-sm" disabled={cancel.isPending}>
              {t('billing.confirmCancel')}
            </button>
          </div>
        </form>
      ) : (
        <button type="button" className="btn btn-ghost btn-sm w-fit text-error" onClick={() => setCancelling(true)}>
          <FontAwesomeIcon icon={faBan} />
          {t('billing.cancel')}
        </button>
      )}
    </section>
  )
}
