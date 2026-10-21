import { useTranslation } from 'react-i18next'

export function SettingsPage() {
  const { t } = useTranslation()

  return (
    <div className="card bg-base-100 shadow-sm max-w-xl">
      <div className="card-body">
        <h1 className="card-title">{t('profile.settings')}</h1>
      </div>
    </div>
  )
}
