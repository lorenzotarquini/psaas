import { faCircleInfo } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { zodResolver } from '@hookform/resolvers/zod'
import { FormProvider, useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { useCreateUser } from '@/entities/user'
import { submitWithServerErrors } from '@/shared/lib/form'
import { routes, usersSearchPath, type UserCreatedState } from '@/shared/routes'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { TextField } from '@/shared/ui/form-field'
import { PageHeader } from '@/shared/ui/page-header'
import { emptyUser, userSchema } from '../model/user'
import { ProfilePicker } from './ProfilePicker'

// UC6: the user is created in Keycloak, which emails them to set the password (R6: it never passes here).
export function UserCreatePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const createUser = useCreateUser()
  const form = useForm({ resolver: zodResolver(userSchema), defaultValues: emptyUser })

  const onSubmit = form.handleSubmit(async (values) => {
    const user = await submitWithServerErrors(() => createUser.mutateAsync(values), form.setError)
    if (user) {
      const state: UserCreatedState = { createdEmail: user.email }
      navigate(usersSearchPath(user.email), { state })
    }
  })

  return (
    <FormProvider {...form}>
      <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        <PageHeader title={t('userForm.title')} back={{ to: routes.users, label: t('userForm.backToUsers') }} />
        {createUser.error && <ErrorAlert error={createUser.error} action="save" />}
        <section className="card bg-base-100 shadow-sm">
          <div className="card-body">
            <div className="grid gap-x-4 sm:grid-cols-2">
              <TextField name="firstName" label={t('userForm.firstName')} autoComplete="off" />
              <TextField name="lastName" label={t('userForm.lastName')} autoComplete="off" />
              <TextField name="email" label={t('users.email')} type="email" autoComplete="off" />
              <TextField name="birthDate" label={t('users.birthDate')} type="date" />
            </div>
            <div role="note" className="alert alert-info alert-soft mt-2">
              <FontAwesomeIcon icon={faCircleInfo} />
              <span>{t('userForm.passwordEmail')}</span>
            </div>
          </div>
        </section>
        <section className="card bg-base-100 shadow-sm">
          <div className="card-body">
            <h2 className="card-title text-base">
              {t('tenantDetail.profiles')}
              <span className="badge badge-ghost">{t('tenantForm.optional')}</span>
            </h2>
            <ProfilePicker />
          </div>
        </section>
        <div className="flex justify-end">
          <button type="submit" className="btn btn-primary" disabled={form.formState.isSubmitting}>
            {form.formState.isSubmitting && <span className="loading loading-spinner loading-sm" />}
            {t('userForm.create')}
          </button>
        </div>
      </form>
    </FormProvider>
  )
}
