import {
  faBuilding,
  faCheck,
  faKey,
  faPaperPlane,
  faPen,
  faPlus,
  faShieldHalved,
  faUserCheck,
  faUserSlash,
  faXmark,
} from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useProfiles } from '@/entities/profile'
import { UserAvatar, UserStatusBadge, useUpdateUser, useUser, useUserAction, type User, type UserAction } from '@/entities/user'
import { ApiError } from '@/shared/api'
import { formatDate, formatWhen } from '@/shared/lib/format'
import { ErrorAlert } from '@/shared/ui/error-alert'
import { groupProfilesByTenant, toggle } from '../model/profileGroups'

// UC19 (mock 1f): the user at a glance, its actions and its profiles grouped by tenant.
export function UserPanel({ userId, onClose }: { userId: string; onClose: () => void }) {
  const { t } = useTranslation()
  const { data: user, error, isPending } = useUser(userId)

  return (
    <aside className="flex h-full flex-col overflow-y-auto border-l border-base-300 bg-base-100" aria-label={t('users.panel')}>
      <button type="button" className="btn btn-ghost btn-sm btn-square absolute right-3 top-3" onClick={onClose} aria-label={t('users.closePanel')}>
        <FontAwesomeIcon icon={faXmark} />
      </button>
      {error ? (
        <div className="p-5">
          <ErrorAlert error={error} />
        </div>
      ) : isPending ? (
        <span className="loading loading-spinner m-5" aria-label={t('loading')} />
      ) : (
        // key: a saved user resets the draft.
        <PanelBody key={JSON.stringify(user)} user={user} />
      )}
    </aside>
  )
}

function PanelBody({ user }: { user: User }) {
  const { t, i18n } = useTranslation()
  const { data: allProfiles = [] } = useProfiles()
  const update = useUpdateUser(user.id)
  const action = useUserAction(user.id)
  const original = {
    firstName: user.firstName,
    lastName: user.lastName,
    birthDate: user.birthDate ?? '',
    mfaRequired: user.mfaRequired,
    profileIds: user.profiles.map((p) => p.id),
  }
  const [editing, setEditing] = useState(false)
  const [draft, setDraft] = useState(original)
  const [extraTenants, setExtraTenants] = useState<string[]>([])
  const groups = groupProfilesByTenant(allProfiles, draft.profileIds, extraTenants)
  const otherTenants = [...new Map(allProfiles.map((p) => [p.tenantId, p.tenantCompanyName]))].filter(
    ([id]) => !groups.some((g) => g.tenantId === id),
  )
  const dirty =
    editing ||
    draft.mfaRequired !== user.mfaRequired ||
    draft.profileIds.length !== user.profiles.length ||
    draft.profileIds.some((id) => !user.profiles.some((p) => p.id === id))
  const errors = update.error instanceof ApiError ? update.error.fieldErrors : {}
  const when = (iso: string) => formatWhen(iso, i18n.language, t('audit.yesterday'))

  const save = () => update.mutate(draft)
  const discard = () => {
    setDraft(original)
    setEditing(false)
    setExtraTenants([])
    update.reset()
  }
  const run = (a: UserAction) => action.mutate(a)

  return (
    <div className="flex flex-col">
      <header className="flex gap-3 border-b border-base-300 px-5 pt-5 pb-4">
        <UserAvatar firstName={user.firstName} lastName={user.lastName} email={user.email} large />
        <div className="min-w-0">
          <h2 className="text-[17px] font-semibold">
            {user.firstName} {user.lastName}
          </h2>
          <p className="break-all text-base-content/60">{user.email}</p>
          <div className="mt-1.5 flex flex-wrap gap-1.5">
            <UserStatusBadge status={user.status} />
            {user.mfaRequired && (
              <span className="badge badge-ghost badge-sm gap-1">
                <FontAwesomeIcon icon={faShieldHalved} />
                {t('users.mfaRequired')}
              </span>
            )}
          </div>
        </div>
      </header>

      <section className="grid grid-cols-2 gap-2 border-b border-base-300 px-5 py-4">
        {/* Mock 1f: always the same four actions; those the status does not allow (UC19 4a) are disabled. */}
        <button
          type="button"
          className={`btn btn-sm ${user.status === 'Invited' ? 'btn-primary' : ''}`}
          disabled={action.isPending || user.status !== 'Invited'}
          onClick={() => run('invite')}
        >
          <FontAwesomeIcon icon={faPaperPlane} />
          {t('users.actions.invite')}
        </button>
        <button type="button" className="btn btn-sm" onClick={() => setEditing(true)} disabled={editing}>
          <FontAwesomeIcon icon={faPen} />
          {t('users.actions.edit')}
        </button>
        <button
          type="button"
          className="btn btn-sm"
          disabled={action.isPending || (user.status !== 'Active' && user.status !== 'Suspended')}
          onClick={() => run('reset-password')}
        >
          <FontAwesomeIcon icon={faKey} />
          {t('users.actions.resetPassword')}
        </button>
        {user.status === 'Disabled' ? (
          <button type="button" className="btn btn-sm" disabled={action.isPending} onClick={() => run('enable')}>
            <FontAwesomeIcon icon={faUserCheck} />
            {t('users.actions.enable')}
          </button>
        ) : (
          <button type="button" className="btn btn-soft btn-error btn-sm" disabled={action.isPending} onClick={() => run('disable')}>
            <FontAwesomeIcon icon={faUserSlash} />
            {t('users.actions.disable')}
          </button>
        )}
        {action.isSuccess && (
          <p role="status" className="col-span-2 text-[12px] text-success">
            {t(`users.done.${action.variables}`)}
          </p>
        )}
        {action.error && (
          <div className="col-span-2">
            <ErrorAlert error={action.error} action="save" />
          </div>
        )}
      </section>

      {editing && (
        <section className="grid grid-cols-2 gap-x-3 gap-y-1 border-b border-base-300 px-5 py-4">
          <Field label={t('userForm.firstName')} error={errors.firstName?.[0]}>
            <input className="input input-sm w-full" value={draft.firstName} onChange={(e) => setDraft({ ...draft, firstName: e.target.value })} />
          </Field>
          <Field label={t('userForm.lastName')} error={errors.lastName?.[0]}>
            <input className="input input-sm w-full" value={draft.lastName} onChange={(e) => setDraft({ ...draft, lastName: e.target.value })} />
          </Field>
          <Field label={t('users.birthDate')} error={errors.birthDate?.[0]}>
            <input type="date" className="input input-sm w-full" value={draft.birthDate} onChange={(e) => setDraft({ ...draft, birthDate: e.target.value })} />
          </Field>
          <label className="flex items-center gap-2 pt-6">
            <input type="checkbox" className="checkbox checkbox-sm" checked={draft.mfaRequired} onChange={(e) => setDraft({ ...draft, mfaRequired: e.target.checked })} />
            {t('users.requireMfa')}
          </label>
        </section>
      )}

      <section className="flex flex-col gap-3 border-b border-base-300 px-5 py-4">
        <div className="flex items-baseline justify-between">
          <h3 className="panel-title">{t('tenantDetail.profiles')}</h3>
          <span className="text-[12px] text-base-content/55">{t('users.groupedByTenant')}</span>
        </div>
        {groups.length === 0 && <p className="text-base-content/60">{t('users.noProfiles')}</p>}
        {groups.map((group) => (
          <div key={group.tenantId}>
            <div className="mb-1.5 flex items-center gap-2 font-semibold">
              <FontAwesomeIcon icon={faBuilding} className="text-[11px] text-base-content/50" />
              {group.tenantCompanyName}
            </div>
            <div className="flex flex-wrap gap-1.5">
              {group.profiles.map((p) => (
                <button
                  key={p.id}
                  type="button"
                  aria-pressed={p.selected}
                  className={`btn btn-sm font-normal ${p.selected ? 'btn-primary btn-soft border-primary' : 'btn-outline border-base-300'}`}
                  onClick={() => setDraft({ ...draft, profileIds: toggle(draft.profileIds, p.id) })}
                >
                  <FontAwesomeIcon icon={p.selected ? faCheck : faPlus} className="text-[10px]" />
                  {p.name}
                </button>
              ))}
            </div>
          </div>
        ))}
        {Object.entries(errors)
          .filter(([key]) => key.startsWith('profileIds'))
          .map(([key, codes]) => (
            <p key={key} className="text-[12px] text-error">
              {t(`validation.${codes[0]}`)}
            </p>
          ))}
        {otherTenants.length > 0 && (
          <select
            className="select select-ghost select-sm w-fit text-primary"
            value=""
            aria-label={t('users.addTenantProfile')}
            onChange={(e) => e.target.value && setExtraTenants([...extraTenants, e.target.value])}
          >
            <option value="">+ {t('users.addTenantProfile')}</option>
            {otherTenants.map(([id, name]) => (
              <option key={id} value={id}>
                {name}
              </option>
            ))}
          </select>
        )}
      </section>

      {dirty && (
        <div className="flex items-center justify-end gap-2 border-b border-base-300 bg-base-200/60 px-5 py-3">
          {update.error && !(update.error instanceof ApiError && update.error.status === 400) && <ErrorAlert error={update.error} action="save" />}
          <button type="button" className="btn btn-ghost btn-sm" onClick={discard}>
            {t('unsaved.discard')}
          </button>
          <button type="button" className="btn btn-primary btn-sm" disabled={update.isPending} onClick={save}>
            {update.isPending && <span className="loading loading-spinner loading-xs" />}
            {t('users.save')}
          </button>
        </div>
      )}

      <section className="px-5 py-4">
        <h3 className="panel-title mb-2.5">{t('users.details')}</h3>
        <dl className="grid grid-cols-[7.5rem_1fr] gap-x-3 gap-y-2">
          <dt className="text-base-content/60">{t('me.keycloakId')}</dt>
          <dd className="data truncate" title={user.id}>
            {user.id}
          </dd>
          <dt className="text-base-content/60">{t('users.username')}</dt>
          <dd className="break-all">{user.email}</dd>
          <dt className="text-base-content/60">{t('users.birthDate')}</dt>
          <dd className="data">{user.birthDate ? formatDate(user.birthDate, i18n.language) : '–'}</dd>
          <dt className="text-base-content/60">{t('users.createdAt')}</dt>
          <dd>
            {user.createdAt ? formatDate(user.createdAt, i18n.language) : '–'}
            {user.createdBy && ` ${t('users.by', { name: user.createdBy })}`}
          </dd>
          <dt className="text-base-content/60">{t('users.invite')}</dt>
          <dd>
            {user.invitesSent > 0
              ? t('users.invitesSent', { count: user.invitesSent, last: user.lastInviteAt ? formatDate(user.lastInviteAt, i18n.language) : '–' })
              : '–'}
          </dd>
          <dt className="text-base-content/60">{t('users.lastLogin')}</dt>
          <dd className="data">{user.lastLoginAt ? when(user.lastLoginAt) : t('users.never')}</dd>
        </dl>
      </section>
    </div>
  )
}

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  const { t } = useTranslation()
  return (
    <label className="fieldset py-1">
      <span className="fieldset-legend py-1 text-[12px] font-medium text-base-content/80">{label}</span>
      {children}
      {error && <span className="text-[12px] text-error">{t(`validation.${error}`)}</span>}
    </label>
  )
}
