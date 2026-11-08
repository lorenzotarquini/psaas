import { useTranslation } from 'react-i18next'
import type { Me } from '../api/getMe'

export function MeDetails({ me }: { me: Me }) {
  const { t } = useTranslation()

  return (
    <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1">
      <dt className="font-medium">{t('me.keycloakId')}</dt>
      <dd>{me.keycloakId}</dd>
      <dt className="font-medium">{t('me.name')}</dt>
      <dd>{me.name}</dd>
      <dt className="font-medium">{t('me.email')}</dt>
      <dd>{me.email}</dd>
      <dt className="font-medium">{t('me.roles')}</dt>
      <dd className="flex flex-wrap gap-1">
        {me.roles.map((role) => (
          <span key={role} className="badge badge-primary">
            {role}
          </span>
        ))}
      </dd>
    </dl>
  )
}
