import { faDownload, faFileCsv } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useImportUsers, type UserImportResult, type UserImportRow } from '@/entities/user'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { csvTemplate, readUserRows } from '../model/csv'

// UC20 (mock 1f "Importa CSV"): choose the file, check the preview, create the valid rows.
export function UserImportDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { t } = useTranslation()
  const dialog = useRef<HTMLDialogElement>(null)
  const importUsers = useImportUsers()
  const [rows, setRows] = useState<UserImportRow[] | null>(null)
  const [badFile, setBadFile] = useState(false)
  const [results, setResults] = useState<UserImportResult[] | null>(null)
  const confirmed = results?.some((r) => r.created) ?? false
  const valid = results?.filter((r) => Object.keys(r.errors).length === 0).length ?? 0

  useEffect(() => {
    if (open) dialog.current?.showModal()
    else dialog.current?.close()
  }, [open])

  const reset = () => {
    setRows(null)
    setResults(null)
    setBadFile(false)
    importUsers.reset()
  }

  const close = () => {
    reset()
    onClose()
  }

  const onFile = async (file: File | undefined) => {
    reset()
    if (!file) return
    const read = readUserRows(await file.text())
    if (!read || read.length === 0) return setBadFile(true)
    setRows(read)
    setResults(await importUsers.mutateAsync({ rows: read, confirm: false }).catch(() => null))
  }

  const confirm = async () => {
    if (!rows) return
    setResults(await importUsers.mutateAsync({ rows, confirm: true }).catch(() => results))
  }

  return (
    <dialog ref={dialog} className="modal" onClose={close}>
      <div className="modal-box flex max-w-3xl flex-col gap-4">
        <div>
          <h2 className="text-[17px] font-semibold">{t('users.import.title')}</h2>
          <p className="text-base-content/60">{t('users.import.hint')}</p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <label className="btn btn-sm">
            <FontAwesomeIcon icon={faFileCsv} />
            {t('users.import.choose')}
            <input type="file" accept=".csv,text/csv" className="hidden" onChange={(e) => void onFile(e.target.files?.[0])} />
          </label>
          <a className="link link-primary text-[12px]" href={`data:text/csv;charset=utf-8,${encodeURIComponent(csvTemplate)}`} download="utenti.csv">
            <FontAwesomeIcon icon={faDownload} /> {t('users.import.template')}
          </a>
        </div>
        {badFile && <div className="alert alert-error alert-soft">{t('users.import.badFile')}</div>}
        {importUsers.error && <ErrorAlert error={importUsers.error} action="save" />}
        {results && rows && (
          <>
            <p className="text-base-content/70">
              {confirmed
                ? t('users.import.created', { count: results.filter((r) => r.created).length, total: results.length })
                : t('users.import.preview', { count: valid, total: results.length })}
            </p>
            <div className="max-h-80 overflow-auto rounded-box border border-base-300">
              <table className="table table-dense">
                <thead>
                  <tr>
                    <th>#</th>
                    <th>{t('users.name')}</th>
                    <th>{t('users.email')}</th>
                    <th>{t('tenantDetail.profiles')}</th>
                    <th>{t('users.import.outcome')}</th>
                  </tr>
                </thead>
                <tbody>
                  {results.map((r) => {
                    const row = rows[r.row - 1]
                    const errors = Object.entries(r.errors)
                    return (
                      <tr key={r.row} className={errors.length ? 'bg-error/5' : ''}>
                        <td className="data">{r.row}</td>
                        <td>
                          {row.firstName} {row.lastName}
                        </td>
                        <td className="break-all">{row.email}</td>
                        <td className="text-[12px]">{row.profiles || '–'}</td>
                        <td className="text-[12px]">
                          {errors.length ? (
                            <span className="text-error">
                              {errors.map(([field, code]) => `${t(`users.import.fields.${field}`, { defaultValue: field })}: ${t(`validation.${code}`)}`).join(' · ')}
                            </span>
                          ) : (
                            <span className="text-success">{t(r.created ? 'users.import.done' : 'users.import.ok')}</span>
                          )}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          </>
        )}
        <div className="modal-action mt-0">
          <button type="button" className="btn btn-ghost btn-sm" onClick={close}>
            {t(confirmed ? 'users.import.close' : 'unsaved.discard')}
          </button>
          {!confirmed && (
            <button type="button" className="btn btn-primary btn-sm" disabled={valid === 0 || importUsers.isPending} onClick={() => void confirm()}>
              {importUsers.isPending && <span className="loading loading-spinner loading-xs" />}
              {t('users.import.confirm', { count: valid })}
            </button>
          )}
        </div>
      </div>
    </dialog>
  )
}
