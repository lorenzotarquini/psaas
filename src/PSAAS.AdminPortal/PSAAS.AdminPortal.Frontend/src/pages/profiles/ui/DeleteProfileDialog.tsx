import { useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { useDeleteProfile, type Profile } from '@/entities/profile'
import { ErrorAlert } from '@/shared/ui/error-alert'

// UC9: deleting is always allowed (UC5-R4), so the dialog only says who loses the profile.
export function DeleteProfileDialog({ profile, onClose }: { profile: Profile | null; onClose: () => void }) {
  const { t } = useTranslation()
  const dialog = useRef<HTMLDialogElement>(null)
  const remove = useDeleteProfile()

  useEffect(() => {
    if (profile) dialog.current?.showModal()
    else dialog.current?.close()
  }, [profile])

  const close = () => {
    remove.reset()
    onClose()
  }

  const confirm = async () => {
    if (!profile) return
    const done = await remove.mutateAsync(profile.id).then(() => true, () => false)
    if (done) close()
  }

  return (
    <dialog ref={dialog} className="modal" onClose={close}>
      <div className="modal-box flex flex-col gap-3">
        <h2 className="text-[17px] font-semibold">{t('profiles.delete')}</h2>
        {remove.error && <ErrorAlert error={remove.error} action="save" />}
        <p>{profile && t('profiles.deleteConfirm', { name: profile.name, tenant: profile.tenantCompanyName })}</p>
        <div className="modal-action mt-0">
          <button type="button" className="btn btn-ghost btn-sm" onClick={close}>
            {t('profiles.cancel')}
          </button>
          <button type="button" className="btn btn-error btn-sm" disabled={remove.isPending} onClick={() => void confirm()}>
            {remove.isPending && <span className="loading loading-spinner loading-xs" />}
            {t('profiles.delete')}
          </button>
        </div>
      </div>
    </dialog>
  )
}
