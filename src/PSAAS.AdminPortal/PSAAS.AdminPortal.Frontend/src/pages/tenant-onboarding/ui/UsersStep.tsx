import { faPlus, faTrash } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { FormProvider, useFieldArray, useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import type { TenantDetail } from '@/entities/tenant'
import { emptyUser, useCreateUser, userSchema } from '@/entities/user'
import { ApiError } from '@/shared/api'
import { applyServerErrors } from '@/shared/lib/form'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { FieldError } from '@/shared/ui/form-field'
import { StepFooter } from './StepFooter'
import { StepLayout } from './SimpleSteps'

const schema = z.object({ users: z.array(userSchema) })
type Values = z.input<typeof schema>

interface UsersStepProps {
  draft: TenantDetail
  onNext: (tenant: TenantDetail) => void
  onCancel: () => void
  nextLabel: string
}

// UC14 step 4: the first users (UC6) with profiles of this tenant; Keycloak emails each one the password link.
export function UsersStep({ draft, onNext, onCancel, nextLabel }: UsersStepProps) {
  const { t } = useTranslation()
  const form = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { users: [] } })
  const rows = useFieldArray({ control: form.control, name: 'users' })
  const create = useCreateUser()
  const [created, setCreated] = useState<string[]>([])

  // Users are created one by one; a rejected row keeps its errors and stops the step.
  const onSubmit = form.handleSubmit(async ({ users }) => {
    for (let i = 0; i < users.length; i++) {
      try {
        const user = await create.mutateAsync(users[i])
        setCreated((current) => [...current, user.email])
        rows.remove(0)
      } catch (error) {
        if (error instanceof ApiError && error.status === 400) {
          applyServerErrors(
            Object.fromEntries(Object.entries(error.fieldErrors).map(([field, codes]) => [`users.0.${field}`, codes])),
            form.setError,
          )
        }
        return
      }
    }
    onNext(draft)
  })

  const field = (index: number, name: 'firstName' | 'lastName' | 'email' | 'birthDate', type = 'text') => (
    <label className="flex flex-col gap-0.5">
      <span className="text-[11.5px] text-base-content/60">{t(`onboarding.userFields.${name}`)}</span>
      <input type={type} className="input input-sm w-full" {...form.register(`users.${index}.${name}`)} />
      <FieldError name={`users.${index}.${name}`} />
    </label>
  )

  return (
    <StepLayout title={t('onboarding.steps.users.title')} hint={t('onboarding.usersHint')}>
      <FormProvider {...form}>
        <form id="onboarding-users" onSubmit={onSubmit} noValidate className="contents">
          {create.error && !(create.error instanceof ApiError && create.error.status === 400) && (
            <ErrorAlert error={create.error} action="save" />
          )}
          {created.length > 0 && (
            <div role="status" className="alert alert-success alert-soft">
              {t('onboarding.usersCreated', { count: created.length, emails: created.join(', ') })}
            </div>
          )}
          <section className="panel flex flex-col divide-y divide-base-300">
            {rows.fields.length === 0 && <p className="px-4 py-6 text-center text-base-content/60">{t('onboarding.noUsers')}</p>}
            {rows.fields.map((row, i) => (
              <div key={row.id} className="grid gap-2 px-4 py-3 md:grid-cols-[1fr_1fr_1.4fr_9rem_auto]">
                {field(i, 'firstName')}
                {field(i, 'lastName')}
                {field(i, 'email', 'email')}
                {field(i, 'birthDate', 'date')}
                <button type="button" className="btn btn-ghost btn-sm self-end" aria-label={t('onboarding.removeUser')} onClick={() => rows.remove(i)}>
                  <FontAwesomeIcon icon={faTrash} />
                </button>
                <div className="flex flex-wrap gap-1.5 md:col-span-5">
                  {draft.profiles.length === 0 ? (
                    <span className="text-[12px] text-base-content/50">{t('onboarding.noProfilesYet')}</span>
                  ) : (
                    draft.profiles.map((profile) => {
                      // Toggled by hand: a single registered checkbox would yield a boolean, not an array.
                      const chosen = (form.watch(`users.${i}.profileIds`) ?? []).includes(profile.id)
                      return (
                        <button
                          key={profile.id}
                          type="button"
                          aria-pressed={chosen}
                          className={`btn btn-xs ${chosen ? 'btn-primary btn-soft' : ''}`}
                          onClick={() => {
                            const current = form.getValues(`users.${i}.profileIds`) ?? []
                            form.setValue(`users.${i}.profileIds`, chosen ? current.filter((id) => id !== profile.id) : [...current, profile.id])
                          }}
                        >
                          {profile.name}
                        </button>
                      )
                    })
                  )}
                </div>
              </div>
            ))}
            <div className="px-4 py-2">
              <button type="button" className="btn btn-ghost btn-sm text-primary" onClick={() => rows.append({ ...emptyUser })}>
                <FontAwesomeIcon icon={faPlus} />
                {t('onboarding.addUser')}
              </button>
            </div>
          </section>
          <StepFooter
            formId="onboarding-users"
            onCancel={onCancel}
            cancelLabel={t('onboarding.cancel')}
            busy={form.formState.isSubmitting}
            primaryLabel={nextLabel}
          />
        </form>
      </FormProvider>
    </StepLayout>
  )
}
