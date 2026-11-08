import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect, useRef, useState } from 'react'
import { FormProvider, useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { useCreateProfile } from '@/entities/profile'
import { useTenants } from '@/entities/tenant'
import { submitWithServerErrors } from '@/shared/lib/form'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { FieldError, TextField } from '@/shared/ui/form-field'

// Messages are the backend validation codes (UC9-S4), as in every form of the portal.
const schema = z.object({
  tenantId: z.string().min(1, 'Required'),
  name: z.string().trim().min(1, 'Required'),
  description: z.string(),
})

const empty = { tenantId: '', name: '', description: '' }

// UC9: a profile of an organization (company or private person); its units are linked from the Structure tab.
export function CreateProfileDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { t } = useTranslation()
  const dialog = useRef<HTMLDialogElement>(null)
  const [search, setSearch] = useState('')
  // shortcut: the first 100 organizations matching the search; a combobox would scale further.
  const { data: tenants } = useTenants({ search, statuses: [], planIds: [], sort: 'companyName', page: 1, pageSize: 100 })
  const create = useCreateProfile()
  const form = useForm({ resolver: zodResolver(schema), defaultValues: empty })

  useEffect(() => {
    if (open) dialog.current?.showModal()
    else dialog.current?.close()
  }, [open])

  const close = () => {
    form.reset(empty)
    setSearch('')
    create.reset()
    onClose()
  }

  const onSubmit = form.handleSubmit(async ({ tenantId, name, description }) => {
    const created = await submitWithServerErrors(
      () => create.mutateAsync({ tenantId, name, description: description.trim() || null }),
      form.setError,
    )
    if (created) close()
  })

  return (
    <dialog ref={dialog} className="modal" onClose={close}>
      <FormProvider {...form}>
        <form className="modal-box flex flex-col gap-2" onSubmit={(event) => void onSubmit(event)} noValidate>
          <h2 className="text-[17px] font-semibold">{t('profiles.create')}</h2>
          {create.error && <ErrorAlert error={create.error} action="save" />}
          <div className="fieldset py-1.5">
            <label htmlFor="profile-tenant" className="fieldset-legend py-1 text-[12px] font-medium text-base-content/80">
              {t('profiles.tenant')}
            </label>
            <input
              type="search"
              className="input input-sm w-full"
              placeholder={t('tenants.searchPlaceholder')}
              aria-label={t('tenants.searchPlaceholder')}
              value={search}
              onChange={(event) => setSearch(event.target.value)}
            />
            <select
              id="profile-tenant"
              className={`select w-full ${form.formState.errors.tenantId ? 'select-error' : ''}`}
              {...form.register('tenantId')}
            >
              <option value="">{t('profiles.tenantPlaceholder')}</option>
              {tenants?.items.map((tenant) => (
                <option key={tenant.id} value={tenant.id}>
                  {tenant.kind === 'Individual' ? `${tenant.companyName} · ${t('tenants.kinds.Individual')}` : tenant.companyName}
                </option>
              ))}
            </select>
            <FieldError name="tenantId" />
          </div>
          <TextField name="name" label={t('profiles.name')} autoComplete="off" />
          <TextField name="description" label={t('profiles.description')} autoComplete="off" hint={t('tenantForm.optional')} />
          <div className="modal-action mt-2">
            <button type="button" className="btn btn-ghost btn-sm" onClick={close}>
              {t('profiles.cancel')}
            </button>
            <button type="submit" className="btn btn-primary btn-sm" disabled={create.isPending}>
              {create.isPending && <span className="loading loading-spinner loading-xs" />}
              {t('profiles.save')}
            </button>
          </div>
        </form>
      </FormProvider>
    </dialog>
  )
}
